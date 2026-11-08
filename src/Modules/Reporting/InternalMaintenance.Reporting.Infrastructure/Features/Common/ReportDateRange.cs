using System.Linq.Expressions;

namespace InternalMaintenance.Reporting.Infrastructure;

/// <summary>
/// Áp dụng khoảng ngày lịch cho cột thời gian mà không gọi Date/Convert trên cột SQL.
/// </summary>
internal static class ReportDateRange
{
    public static IQueryable<TEntity> WhereDateRange<TEntity>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, DateTime>> dateSelector,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var parameter = dateSelector.Parameters[0];
        Expression? predicate = null;
        var dateColumn = dateSelector.Body;

        if (fromDate.HasValue)
        {
            predicate = Expression.GreaterThanOrEqual(
                dateColumn,
                Expression.Constant(fromDate.Value.Date));
        }

        if (toDate.HasValue)
        {
            // ToDate là ngày lịch được chọn; dùng mốc ngày kế tiếp loại trừ để lấy trọn ngày,
            // đồng thời giữ điều kiện sargable để SQL Server có thể dùng index trên cột thời gian.
            var endExclusive = toDate.Value.Date == DateTime.MaxValue.Date
                ? DateTime.MaxValue
                : toDate.Value.Date.AddDays(1);
            var endComparison = toDate.Value.Date == DateTime.MaxValue.Date
                ? Expression.LessThanOrEqual(dateColumn, Expression.Constant(endExclusive))
                : Expression.LessThan(dateColumn, Expression.Constant(endExclusive));

            predicate = predicate is null
                ? endComparison
                : Expression.AndAlso(predicate, endComparison);
        }

        if (predicate is null)
            return query;

        return query.Where(Expression.Lambda<Func<TEntity, bool>>(predicate, parameter));
    }
}
