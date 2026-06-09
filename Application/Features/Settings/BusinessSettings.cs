namespace Application.Features.Settings;

public class BusinessSettings
{
    public const string SectionName = "BusinessSettings";

    public decimal VatRate { get; set; } = 15.00m;
    public int LowStockThreshold { get; set; } = 5;
    public int DefaultPageSize { get; set; } = 20;
    public int IdleTimeoutInMinutes { get; set; } = 10;
}
