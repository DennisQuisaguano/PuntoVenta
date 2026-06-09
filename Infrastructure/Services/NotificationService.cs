using Application.Features.Notifications;
using Application.Features.Settings;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly BusinessSettings _settings;

    public NotificationService(AppDbContext context, IOptions<BusinessSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int? userId, string role, string? userName = null)
    {
        var notifications = new List<NotificationDto>();

        // 1. Notificaciones de Bajo Stock (SOLO PARA ADMINISTRADORES)
        if (role == "Administrador")
        {
            var lowStockProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.Stock <= _settings.LowStockThreshold)
                .Take(5)
                .ToListAsync();

            foreach (var p in lowStockProducts)
            {
                bool isOut = p.Stock <= 0;
                notifications.Add(new NotificationDto
                {
                    Message = isOut ? $"¡AGOTADO! '{p.Name}' no tiene unidades disponibles." : $"Stock bajo: '{p.Name}' tiene solo {p.Stock} unidades.",
                    Type = isOut ? "Danger" : "Warning",
                    Icon = isOut ? "bi-x-octagon-fill" : "bi-exclamation-triangle-fill",
                    CreatedAt = p.UpdatedAt ?? p.CreatedAt,
                    TargetUrl = "/productos"
                });
            }
        }

        // 2. Notificaciones de Ventas y Devoluciones
        if (role == "Administrador")
        {
            // --- NUEVAS VENTAS (VISTA GLOBAL PARA ADMIN) ---
            var recentSales = await _context.Sales
                .Include(s => s.Customer)
                .AsNoTracking()
                .Where(s => s.Status == SaleStatus.Confirmed)
                .OrderByDescending(s => s.IssueDate)
                .Take(5)
                .ToListAsync();

            foreach (var s in recentSales)
            {
                notifications.Add(new NotificationDto
                {
                    Message = $"Nueva Venta: {s.InvoiceNumber} por ${s.Total:N2}",
                    Type = "Success",
                    Icon = "bi-cart-check-fill",
                    CreatedAt = s.IssueDate,
                    TargetUrl = $"/facturas?numeroFactura={s.InvoiceNumber}"
                });
            }

            // --- DEVOLUCIONES / ANULACIONES (ADMIN) ---
            var recentCancellations = await _context.Sales
                .AsNoTracking()
                .Where(s => s.Status == SaleStatus.Cancelled)
                .OrderByDescending(s => s.UpdatedAt ?? s.IssueDate)
                .Take(5)
                .ToListAsync();

            foreach (var c in recentCancellations)
            {
                notifications.Add(new NotificationDto
                {
                    Message = $"Venta Anulada: {c.InvoiceNumber} (Total devuelto: ${c.Total:N2})",
                    Type = "Danger",
                    Icon = "bi-arrow-counterclockwise",
                    CreatedAt = c.UpdatedAt ?? c.IssueDate,
                    TargetUrl = $"/facturas?numeroFactura={c.InvoiceNumber}"
                });
            }
        }
        else if (role == "Vendedor")
        {
            // --- MIS VENTAS (VISTA PERSONAL PARA VENDEDOR) ---
            var myRecentSales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.Status == SaleStatus.Confirmed && s.CreatedBy == userName)
                .OrderByDescending(s => s.IssueDate)
                .Take(10)
                .ToListAsync();

            foreach (var s in myRecentSales)
            {
                notifications.Add(new NotificationDto
                {
                    Message = $"Venta realizada: {s.InvoiceNumber} (${s.Total:N2})",
                    Type = "Success",
                    Icon = "bi-bag-check",
                    CreatedAt = s.IssueDate,
                    TargetUrl = $"/facturas?numeroFactura={s.InvoiceNumber}"
                });
            }
        }

        return notifications.OrderByDescending(n => n.CreatedAt).Take(15);
    }
}
