namespace Application.Features.Users;

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? IDCard { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Role { get; set; } = "Vendedor"; // Administrador o Vendedor
    public string? Address { get; set; }
}

public class UpdateUserRequest : RegisterUserRequest
{
    public int Id { get; set; }
    public string? NewPassword { get; set; }
    public bool IsActive { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public string? UserName { get; set; }
    public string? Role { get; set; }
    public string? RefreshToken { get; set; } // Nuevo campo para rotación de tokens
    public DateTime? Expiration { get; set; }
}

public class UserResponseDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? IDCard { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
}

public record TokenRequestDto(string AccessToken, string RefreshToken);

public class ProfileStatsDto
{
    public bool IsAdmin { get; set; }
    
    // Métricas para Vendedores
    public int MyMonthlySalesCount { get; set; }
    public decimal MyMonthlySalesAmount { get; set; }
    public decimal MyDailySalesAmount { get; set; }
    public int MyDailySalesCount { get; set; }
    public decimal MyAverageTicket { get; set; }
    
    // Métricas para Administradores
    public decimal SystemMonthlyRevenue { get; set; }
    public decimal SystemDailyRevenue { get; set; }
    public int SystemDailySalesCount { get; set; }
    public int SystemLowStockCount { get; set; }
    public int SystemNewCustomersCount { get; set; }
    public int SystemActiveUsersCount { get; set; }
    public int SystemTotalProductsCount { get; set; }
    public int SystemTotalCustomersCount { get; set; }
}
