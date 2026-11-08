/*
    Khôi phục SlaStatus cho ticket đã resolve/closed (BẢN THỰC THI CHÍNH THỨC).

    Mục đích:
    - Sửa lỗi do worker cũ ghi đè nhãn SLA (MetSLA/MissedSLA -> NearBreach/Breached).
    - Tính đúng nhãn tại thời điểm ResolvedAt (ResolvedAt <= DueAt: MetSLA, ngược lại: MissedSLA).
    - Giữ nguyên các cột Audit (*NotifiedAt).

    Đảm bảo an toàn:
    - Bao bọc trong TRANSACTION.
    - Có điều kiện WHERE kiểm tra concurrency (Status, ResolvedAt, DueAt, SlaStatus khớp chính xác).
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

PRINT '==================================================';
PRINT 'BẮT ĐẦU QUÁ TRÌNH KHÔI PHỤC SLA STATUS CHO TICKETS';
PRINT 'Thời gian thực thi: ' + CONVERT(VARCHAR, GETUTCDATE(), 120) + ' (UTC)';
PRINT '==================================================';

IF OBJECT_ID('tempdb..#SlaRepairCandidates') IS NOT NULL
    DROP TABLE #SlaRepairCandidates;

-- 1. Lọc danh sách các ticket bị sai SlaStatus
SELECT
    ticket.Id,
    ticket.TicketCode,
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

DECLARE @TotalCandidates INT = (SELECT COUNT(*) FROM #SlaRepairCandidates);

PRINT 'Số lượng ticket cần khôi phục nhãn: ' + CAST(@TotalCandidates AS VARCHAR);

IF @TotalCandidates = 0
BEGIN
    PRINT '-> Không có ticket nào bị sai SlaStatus. Quá trình kết thúc an toàn.';
    DROP TABLE #SlaRepairCandidates;
    RETURN;
END

-- 2. Thống kê trước khi UPDATE
PRINT 'Thống kê theo nhãn dự kiến cập nhật:';
SELECT
    ExpectedSlaStatus,
    COUNT(*) AS TicketCount
FROM #SlaRepairCandidates
GROUP BY ExpectedSlaStatus;

-- 3. Thực thi UPDATE trong Transaction
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

    DECLARE @UpdatedRows INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT '-> Cập nhật THÀNH CÔNG cho ' + CAST(@UpdatedRows AS VARCHAR) + ' tickets.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '-> LỖI XẢY RA TRONG TRANSACTION: ' + ERROR_MESSAGE();
    THROW;
END CATCH;

-- 4. Báo cáo các trường hợp vi phạm bất biến DueAt cần review thủ công
PRINT '--------------------------------------------------';
PRINT 'Danh sách tickets cần xem xét thủ công do DueAt lệch công thức:';
SELECT
    ticket.Id,
    ticket.TicketCode,
    ticket.Priority,
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
            ticket.CreatedAt)) AS DueAtCalculated
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
                ticket.CreatedAt)))) > 1;

DROP TABLE #SlaRepairCandidates;
PRINT '==================================================';
PRINT 'HOÀN THÀNH QUÁ TRÌNH.';
PRINT '==================================================';
