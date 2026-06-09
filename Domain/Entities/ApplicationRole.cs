using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
