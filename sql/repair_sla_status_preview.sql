/*
    Khôi phục SlaStatus cho ticket đã resolve/closed.

    Lý do: worker cũ có thể đã đổi MetSLA/MissedSLA thành NearBreach/Breached
    sau khi kỹ thuật viên resolve. Kết quả hoàn tất phải được tính tại ResolvedAt,
    không phải tại thời điểm worker chạy hoặc người dùng bấm Close.

    An toàn: @ApplyChanges mặc định 0, nên lần chạy đầu chỉ preview. Script so sánh
    lại các giá trị gốc trước UPDATE để tránh ghi đè nếu ticket vừa được sửa đồng thời.
    Giữ nguyên các cột *NotifiedAt vì chúng là dấu vết audit của worker cũ.

    Không tái dựng DueAt tự động. Priority hiện tại có thể khác priority lúc tạo;
    thời gian pause vendor và các lần đổi priority không có lịch sử đầy đủ để xác định
    chắc hạn cũ. Truy vấn cuối chỉ liệt kê các dòng đáng xem xét thủ công.
*/
SET NOCOUNT ON;

DECLARE @ApplyChanges bit = 0; -- Chỉ đổi thành 1 sau khi đã backup và duyệt preview.

IF OBJECT_ID('tempdb..#SlaRepairCandidates') IS NOT NULL
    DROP TABLE #SlaRepairCandidates;

SELECT
    ticket.Id,
    ticket.Status AS OriginalStatus,
    ticket.ResolvedAt AS OriginalResolvedAt,
    ticket.DueAt AS OriginalDueAt,
    ticket.SlaStatus AS OriginalSlaStatus,
    CASE
        WHEN ticket.ResolvedAt <= ticket.DueAt THEN 'MetSLA'
        ELSE 'MissedSLA'
    END AS ExpectedSlaStatus
INTO #SlaRepairCandidates
FROM dbo.MaintenanceTickets AS ticket
WHERE ticket.Status IN ('Resolved', 'Closed')
  AND ticket.ResolvedAt IS NOT NULL
  AND ticket.DueAt IS NOT NULL
  AND ticket.SlaStatus <> CASE
        WHEN ticket.ResolvedAt <= ticket.DueAt THEN 'MetSLA'
        ELSE 'MissedSLA'
      END;

SELECT
    ExpectedSlaStatus,
    COUNT(*) AS TicketCount
FROM #SlaRepairCandidates
GROUP BY ExpectedSlaStatus
ORDER BY ExpectedSlaStatus;

SELECT
    candidate.Id,
    ticket.TicketCode,
    candidate.OriginalStatus,
    candidate.OriginalResolvedAt,
    candidate.OriginalDueAt,
    candidate.OriginalSlaStatus,
    candidate.ExpectedSlaStatus,
    ticket.NearBreachNotifiedAt,
    ticket.BreachedNotifiedAt,
    ticket.EscalatedNotifiedAt
FROM #SlaRepairCandidates AS candidate
JOIN dbo.MaintenanceTickets AS ticket ON ticket.Id = candidate.Id
ORDER BY ticket.ResolvedAt, ticket.TicketCode;

IF @ApplyChanges = 1
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE ticket
        SET SlaStatus = candidate.ExpectedSlaStatus
        OUTPUT
            inserted.Id,
            inserted.TicketCode,
            deleted.SlaStatus AS OldSlaStatus,
            inserted.SlaStatus AS NewSlaStatus
        FROM dbo.MaintenanceTickets AS ticket
        JOIN #SlaRepairCandidates AS candidate ON candidate.Id = ticket.Id
        WHERE ticket.Status = candidate.OriginalStatus
          AND ticket.ResolvedAt = candidate.OriginalResolvedAt
          AND ticket.DueAt = candidate.OriginalDueAt
          AND ticket.SlaStatus = candidate.OriginalSlaStatus;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

-- Chỉ là danh sách rà soát: giá trị ExpectedDueAt không được UPDATE tự động.
-- Sai khác có thể do priority đã đổi sau khi ticket được tạo.
SELECT
    ticket.Id,
    ticket.TicketCode,
    ticket.Priority AS CurrentPriority,
    ticket.CreatedAt,
    ticket.TotalSlaPausedMinutes,
    ticket.DueAt AS StoredDueAt,
    DATEADD(
        MINUTE,
        ticket.TotalSlaPausedMinutes,
        DATEADD(HOUR,
            CASE ticket.Priority
                WHEN 'Critical' THEN 2
                WHEN 'High' THEN 8
                WHEN 'Medium' THEN 24
                WHEN 'Low' THEN 48
                ELSE 24
            END,
            ticket.CreatedAt)) AS DueAtFromCurrentPriority
FROM dbo.MaintenanceTickets AS ticket
WHERE ticket.Status IN ('Resolved', 'Closed')
  AND ticket.ResolvedAt IS NOT NULL
  AND ticket.DueAt IS NOT NULL
  AND ABS(DATEDIFF(MINUTE, ticket.DueAt,
        DATEADD(MINUTE, ticket.TotalSlaPausedMinutes,
            DATEADD(HOUR,
                CASE ticket.Priority
                    WHEN 'Critical' THEN 2
                    WHEN 'High' THEN 8
                    WHEN 'Medium' THEN 24
                    WHEN 'Low' THEN 48
                    ELSE 24
                END,
                ticket.CreatedAt)))) > 1
ORDER BY ticket.TicketCode;

DROP TABLE #SlaRepairCandidates;
