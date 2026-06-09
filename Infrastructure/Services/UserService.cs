using Application.Features.Users;
using Application.Interfaces.Repositories;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ISaleRepository _saleRepository;
    private readonly AppDbContext _context;

    public UserService(
        UserManager<ApplicationUser> userManager, 
        RoleManager<ApplicationRole> roleManager,
        ISaleRepository saleRepository,
        AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _saleRepository = saleRepository;
        _context = context;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.IgnoreQueryFilters().ToListAsync();
        var userDtos = new List<UserResponseDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(await MapToDtoAsync(user, roles.FirstOrDefault() ?? "Sin Rol"));
        }

        return userDtos;
    }

    public async Task<(IEnumerable<UserResponseDto> Items, int TotalCount)> GetPagedUsersAsync(
        int pageNumber, 
        int pageSize, 
        string? term = null, 
        string searchBy = "name", 
        string status = "active")
    {
        var query = _userManager.Users.IgnoreQueryFilters().AsQueryable();
        var now = DateTimeOffset.UtcNow;

        if (status == "active") query = query.Where(u => u.IsActive && (u.LockoutEnd == null || u.LockoutEnd <= now));
        else if (status == "inactive") query = query.Where(u => !u.IsActive);
        else if (status == "bloqueados") query = query.Where(u => u.IsActive && u.LockoutEnd > now);

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            if (searchBy == "id")
            {
                query = query.Where(u => u.Id.ToString().StartsWith(term));
            }
            else if (searchBy == "name") 
            {
                query = query.Where(u => u.LastName.ToLower().StartsWith(term));
            }
            else if (searchBy == "role")
            {
                query = from u in query
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        join r in _context.Roles on ur.RoleId equals r.Id
                        where r.Name.ToLower().StartsWith(term)
                        select u;
            }
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.LastName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<UserResponseDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(await MapToDtoAsync(user, roles.FirstOrDefault() ?? "Sin Rol"));
        }

        return (userDtos, totalCount);
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int id)
    {
        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return await MapToDtoAsync(user, roles.FirstOrDefault() ?? "Sin Rol");
    }

    public async Task<UserResponseDto?> GetUserByUserNameAsync(string userName)
    {
        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return await MapToDtoAsync(user, roles.FirstOrDefault() ?? "Sin Rol");
    }

    public async Task<Domain.Entities.ApplicationUser?> GetUserEntityByUserNameAsync(string userName)
    {
        return await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task<ProfileStatsDto> GetProfileStatsAsync(string userName, string role)
    {
        var isAdmin = (role == "Administrador");
        
        // Obtenemos las fechas directamente desde .NET
        var ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorTimeZone);
        var firstDayOfMonth = new DateTime(nowLocal.Year, nowLocal.Month, 1);

        // 1. Obtener datos crudos desde la infraestructura
        var monthlySales = await _saleRepository.GetMonthlySalesAsync(firstDayOfMonth);

        // 2. Usar la lógica de Aplicación para calcular (Desacoplado)
        var calculator = new StatisticsCalculator();
        var stats = calculator.CalculateStats(monthlySales, userName, isAdmin);

        // 3. Rellenar contadores directos que no dependen de lógica compleja de ventas
        if (isAdmin)
        {
            stats.SystemLowStockCount = await _context.Products
                .CountAsync(p => p.IsActive && p.Stock <= 5);

            stats.SystemNewCustomersCount = await _context.Customers
                .CountAsync(c => c.CreatedAt >= firstDayOfMonth);

            stats.SystemActiveUsersCount = await _userManager.Users
                .CountAsync(u => u.IsActive);

            stats.SystemTotalProductsCount = await _context.Products.CountAsync(p => p.IsActive);
            stats.SystemTotalCustomersCount = await _context.Customers.CountAsync(c => c.IsActive);
        }
        else
        {
            stats.SystemTotalProductsCount = await _context.Products.CountAsync(p => p.IsActive);
        }

        return stats;
    }

    private async Task<UserResponseDto> MapToDtoAsync(ApplicationUser user, string role)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IDCard = user.IDCard,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? "",
            Address = user.Address,
            Role = role,
            IsActive = user.IsActive,
            LastLogin = user.LastLogin,
            MustChangePassword = user.MustChangePassword,
            IsLockedOut = await _userManager.IsLockedOutAsync(user),
            LockoutEnd = user.LockoutEnd
        };
    }

    public async Task<(bool IsSuccess, string[] Errors)> CreateUserAsync(RegisterUserRequest request)
    {
        var existingUser = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserName == request.UserName);
        if (existingUser != null && !existingUser.IsActive)
        {
            return (false, new[] { $"INACTIVE_USER_EXISTS|{existingUser.Id}" });
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IDCard = request.IDCard,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            MustChangePassword = true 
        };

        user.Activate();

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(request.Role))
            {
                if (!await _roleManager.RoleExistsAsync(request.Role))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(request.Role));
                }
                await _userManager.AddToRoleAsync(user, request.Role);
            }
            return (true, Array.Empty<string>());
        }

        return (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool IsSuccess, string[] Errors)> UpdateUserAsync(UpdateUserRequest request)
    {
        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == request.Id);
        if (user == null) return (false, new[] { "Usuario no encontrado." });

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IDCard = request.IDCard;
        user.Address = request.Address;
        user.Email = request.Email;
        user.UserName = request.UserName;
        user.PhoneNumber = request.PhoneNumber;
        
        if (request.IsActive) user.Activate();
        else user.Deactivate("Admin");

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(request.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!await _roleManager.RoleExistsAsync(request.Role))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(request.Role));
                }
                await _userManager.AddToRoleAsync(user, request.Role);
            }

            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (!passResult.Succeeded)
                {
                    return (false, passResult.Errors.Select(e => e.Description).ToArray());
                }
                user.MustChangePassword = true;
                await _userManager.UpdateAsync(user);
            }

            return (true, Array.Empty<string>());
        }

        return (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool IsSuccess, string[] Errors)> ReactivateUserAsync(int userId, UpdateUserRequest request)
    {
        request.Id = userId;
        request.IsActive = true; 
        return await UpdateUserAsync(request);
    }

    public async Task<bool> UnlockUserAsync(int userId)
    {
        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        await _userManager.ResetAccessFailedCountAsync(user);
        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        
        return result.Succeeded;
    }

    public async Task<(bool IsSuccess, string[] Errors)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return (false, new[] { "Usuario no encontrado." });

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            return (true, Array.Empty<string>());
        }

        return (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;

        user.Deactivate("System");
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<IEnumerable<string>> GetRolesAsync()
    {
        return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
    }
}
