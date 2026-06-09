using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Features.Customers;
using Application.Features.Products;
using Application.Features.Sales;
using Application.Features.Inventory;
using Application.Features.Users;
using Application.Features.Notifications;
using Application.Features.AuditLogs;
using Application.Features.Settings;
using Domain.Entities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Oracle;
using Infrastructure.Services.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;


namespace API.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // --- FLUENT VALIDATION AUTO-VALIDATION ---
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();

        // --- BUSINESS SETTINGS (Options Pattern) ---
        services.Configure<BusinessSettings>(configuration.GetSection(BusinessSettings.SectionName));

        // --- DYNAMIC DATABASE CONFIGURATION ---
        var dbProvider = configuration["DbProvider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            // --- DBCONTEXT POOLING (SENIOR OPTIMIZATION) ---
            // Reutiliza instancias de DbContext para evitar sobrecarga de memoria
            services.AddDbContextPool<AppDbContext>(options => options.UseSqlServer(connectionString), poolSize: 128);
            services.AddScoped<IDbProviderService, SqlServerProviderService>();
        }
        else if (dbProvider.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContextPool<AppDbContext>(options => options.UseOracle(connectionString), poolSize: 128);
            services.AddScoped<IDbProviderService, OracleProviderService>();
        }

        // --- IDENTITY CONFIGURATION ---
        services.AddIdentity<ApplicationUser, ApplicationRole>(options => 
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            // Un bloqueo permanente de unos 100 años (o puedes usar un valor fijo grande)
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(36500);
            options.Lockout.MaxFailedAccessAttempts = 3;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // --- VALIDATORS (FLUENT VALIDATION) ---
        services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerValidator>();
        services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerValidator>();
        services.AddScoped<IValidator<CreateProductRequest>, CreateProductValidator>();
        services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductValidator>();
        services.AddScoped<IValidator<RegisterUserRequest>, RegisterValidator>();
        services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserValidator>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ISaleQueryService, SaleQueryService>();
        services.AddScoped<ISaleCommandService, SaleCommandService>();

        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Política global contra DoS: 100 peticiones por minuto por cada IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100, 
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    message = "Seguridad: Demasiadas peticiones detectadas desde su dirección IP. Bloqueo temporal activado (DoS Protection).",
                    retryAfter = "60 seconds"
                }, cancellationToken: token);
            };
        });

        return services;
    }

    public static IServiceCollection AddMicrosoftAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuramos la autenticación de Microsoft (Azure AD / Entra ID)
        // Usamos un esquema diferente ("Microsoft") para no colisionar con nuestro JWT predeterminado
        services.AddAuthentication()
            .AddMicrosoftIdentityWebApi(configuration, "AzureAd", "Microsoft");

        return services;
    }
}
