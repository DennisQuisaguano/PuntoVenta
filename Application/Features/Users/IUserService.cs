namespace Application.Features.Users;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
    Task<(IEnumerable<UserResponseDto> Items, int TotalCount)> GetPagedUsersAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "name", string status = "active");
    Task<UserResponseDto?> GetUserByIdAsync(int id);
    Task<UserResponseDto?> GetUserByUserNameAsync(string userName);
    Task<Domain.Entities.ApplicationUser?> GetUserEntityByUserNameAsync(string userName);
    Task<ProfileStatsDto> GetProfileStatsAsync(string userName, string role);
    Task<(bool IsSuccess, string[] Errors)> CreateUserAsync(RegisterUserRequest request);
    Task<(bool IsSuccess, string[] Errors)> UpdateUserAsync(UpdateUserRequest request);
    Task<(bool IsSuccess, string[] Errors)> ReactivateUserAsync(int userId, UpdateUserRequest request);
    Task<bool> UnlockUserAsync(int userId);
    Task<(bool IsSuccess, string[] Errors)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> DeleteUserAsync(int id);
    Task<IEnumerable<string>> GetRolesAsync();
}
