using Domain.Entities;

namespace Application.Features.Users;

public class StatisticsCalculator
{
    public ProfileStatsDto CalculateStats(IEnumerable<Sale> monthlySales, string userName, bool isAdmin)
    {
        var stats = new ProfileStatsDto { IsAdmin = isAdmin };

        // Usar la zona horaria de Ecuador para consistencia
        var ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorTimeZone).Date;

        if (isAdmin)
        {
            var confirmedSales = monthlySales.Where(s => s.Status == Domain.Enums.SaleStatus.Confirmed).ToList();
            stats.SystemMonthlyRevenue = confirmedSales.Sum(s => s.Total);

            var todaySales = confirmedSales.Where(s => s.IssueDate.Date == today).ToList();
            stats.SystemDailyRevenue = todaySales.Sum(s => s.Total);
            stats.SystemDailySalesCount = todaySales.Count;
        }
        else
        {
            var myMonthlySales = monthlySales
                .Where(s => s.CreatedBy != null && s.CreatedBy.Equals(userName, StringComparison.OrdinalIgnoreCase) && s.Status == Domain.Enums.SaleStatus.Confirmed)
                .ToList();

            stats.MyMonthlySalesCount = myMonthlySales.Count;
            stats.MyMonthlySalesAmount = myMonthlySales.Sum(s => s.Total);

            var myTodaySales = myMonthlySales.Where(s => s.IssueDate.Date == today).ToList();

            stats.MyDailySalesAmount = myTodaySales.Sum(s => s.Total);
            stats.MyDailySalesCount = myTodaySales.Count;

            stats.MyAverageTicket = stats.MyMonthlySalesCount > 0 
                ? stats.MyMonthlySalesAmount / stats.MyMonthlySalesCount 
                : 0;
        }

        return stats;
    }
}
