using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    
    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options) 
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        // Lógica de IAuditable (CreatedAt, CreatedBy, etc)
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
            }
        }

        // --- INICIO AUDITORÍA GENERAL ---
        var auditEntries = OnBeforeSaveChanges(currentUser);
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries);
        return result;
        // --- FIN AUDITORÍA GENERAL ---
    }

    private List<AuditEntry> OnBeforeSaveChanges(string userId)
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry);
            auditEntry.TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            auditEntry.UserId = userId;
            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue!;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.AuditType = "Create";
                        auditEntry.NewValues[propertyName] = property.CurrentValue!;
                        break;

                    case EntityState.Deleted:
                        auditEntry.AuditType = "Delete";
                        auditEntry.OldValues[propertyName] = property.OriginalValue!;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.ChangedColumns.Add(propertyName);
                            auditEntry.AuditType = "Update";
                            auditEntry.OldValues[propertyName] = property.OriginalValue!;
                            auditEntry.NewValues[propertyName] = property.CurrentValue!;
                        }
                        break;
                }
            }
        }

        foreach (var auditEntry in auditEntries.Where(_ => !_.HasTemporaryProperties))
        {
            AuditLogs.Add(auditEntry.ToAudit());
        }

        return auditEntries.Where(_ => _.HasTemporaryProperties).ToList();
    }

    private Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
            }

            AuditLogs.Add(auditEntry.ToAudit());
        }

        return base.SaveChangesAsync();
    }

    public string? GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        bool isSqlServer = Database.IsSqlServer();
        bool isOracle = Database.ProviderName == "Oracle.EntityFrameworkCore";

        // --- AUDIT LOGS ---
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(ValidationConstants.AuditUserMax);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.Property(e => e.Type).HasMaxLength(20);
            entity.HasIndex(e => e.DateTime);
        });

        // --- FILTROS GLOBALES ---
        modelBuilder.Entity<Product>().HasQueryFilter(p => p.IsActive);
        modelBuilder.Entity<Customer>().HasQueryFilter(c => c.IsActive);
        modelBuilder.Entity<Sale>().HasQueryFilter(s => s.IsActive);
        modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => u.IsActive);
        modelBuilder.Entity<InventoryMovement>().HasQueryFilter(im => im.IsActive);

        // --- ERROR LOGS ---
        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.StackTrace).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.CreatedAt);
        });

        // --- MOVIMIENTOS DE INVENTARIO ---
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(im => im.Id);
            entity.Property(im => im.UnitCost).HasPrecision(18, 2);
            entity.Property(im => im.Reference).HasMaxLength(250);
            
            entity.HasOne(im => im.Product)
                  .WithMany()
                  .HasForeignKey(im => im.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Auditoría
            entity.Property(im => im.CreatedBy).HasMaxLength(ValidationConstants.AuditUserMax);
            entity.Property(im => im.UpdatedBy).HasMaxLength(ValidationConstants.AuditUserMax);
        });

        // --- PRODUCTOS ---
        modelBuilder.Entity<Product>(entity =>
        {
            // Solo SQL Server requiere el metadato de HasTrigger para no ignorar triggers en el comando de guardado
            if (isSqlServer)
            {
                entity.ToTable(tb => tb.HasTrigger("TR_PreventNegativeStockAndPrice"));
            }
            
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(ValidationConstants.ProductNameMax).IsRequired();
            entity.HasIndex(p => p.Name);

            entity.OwnsOne(p => p.UnitPrice, price =>
            {
                price.Property(p => p.Worth).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired();
            });

            // Auditoría
            entity.Property(p => p.CreatedBy).HasMaxLength(ValidationConstants.AuditUserMax);
            entity.Property(p => p.UpdatedBy).HasMaxLength(ValidationConstants.AuditUserMax);
        });

        // --- CLIENTES ---
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IDCard).IsUnique(); 
            entity.HasIndex(e => e.LastName); 
            
            entity.Property(e => e.IDCard)
                .HasMaxLength(ValidationConstants.IDCardLength)
                .IsFixedLength() // Mapea a CHAR(10)
                .IsRequired();

            entity.Property(e => e.Name).HasMaxLength(ValidationConstants.NameMax).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(ValidationConstants.LastNameMax).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(ValidationConstants.PhoneLength).IsFixedLength();
            entity.Property(e => e.Email).HasMaxLength(ValidationConstants.EmailMax);
            entity.Property(e => e.Address).HasMaxLength(ValidationConstants.AddressMax);

            // Auditoría
            entity.Property(e => e.CreatedBy).HasMaxLength(ValidationConstants.AuditUserMax);
            entity.Property(p => p.UpdatedBy).HasMaxLength(ValidationConstants.AuditUserMax);
        });

        // --- VENTAS Y SECUENCIAS ---
        modelBuilder.HasSequence<int>("InvoiceSequence").StartsAt(1).IncrementsBy(1);

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(v => v.SubTotal).HasPrecision(18, 2);
            entity.Property(v => v.VatPercentage).HasPrecision(5, 2);
            entity.Property(v => v.VatAmount).HasPrecision(18, 2);
            entity.Property(v => v.Total).HasPrecision(18, 2);

            // Las restricciones de verificación se mapean igual en ambos motores, 
            // pero SQL Server es más indulgente con la sintaxis T-SQL.
            entity.ToTable(t => t.HasCheckConstraint("CK_Sale_Total_Consistency", "Total = SubTotal + VatAmount"));

            entity.HasKey(s => s.Id);
            entity.Property(s => s.Status).HasConversion<int>().IsRequired();
            entity.HasIndex(s => s.InvoiceNumber);
            entity.HasIndex(s => s.IssueDate);

            entity.HasOne(s => s.Customer)
                  .WithMany()
                  .HasForeignKey(s => s.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(v => v.Details)
                  .WithOne()
                  .HasForeignKey("SaleId")
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade);

            // Auditoría
            entity.Property(s => s.CreatedBy).HasMaxLength(ValidationConstants.AuditUserMax);
            entity.Property(s => s.UpdatedBy).HasMaxLength(ValidationConstants.AuditUserMax);
        });

        // --- USUARIOS ---
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(u => u.LastName);
            entity.Property(u => u.UserName).HasMaxLength(ValidationConstants.UserNameMax);
            entity.Property(u => u.FirstName).HasMaxLength(ValidationConstants.NameMax);
            entity.Property(u => u.LastName).HasMaxLength(ValidationConstants.LastNameMax);
        });

        // --- DETALLES ---
        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.HasKey(vd => vd.Id);

            entity.OwnsOne(vd => vd.UnitPrice, price =>
            {
                price.Property(p => p.Worth).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired();
            });

            entity.HasOne(sd => sd.Product)
                  .WithMany()
                  .HasForeignKey(sd => sd.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

// Clase Auxiliar para construir los registros de auditoría
public class AuditEntry
{
    public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        Entry = entry;
    }

    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
    public string UserId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string AuditType { get; set; } = string.Empty;
    public Dictionary<string, object> KeyValues { get; } = new();
    public Dictionary<string, object> OldValues { get; } = new();
    public Dictionary<string, object> NewValues { get; } = new();
    public List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> TemporaryProperties { get; } = new();
    public List<string> ChangedColumns { get; } = new();

    public bool HasTemporaryProperties => TemporaryProperties.Any();

    public AuditLog ToAudit()
    {
        var audit = new AuditLog();
        audit.UserId = UserId;
        audit.Type = AuditType;
        audit.TableName = TableName;
        audit.DateTime = DateTime.UtcNow;
        audit.PrimaryKey = JsonSerializer.Serialize(KeyValues);
        audit.OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues);
        audit.NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues);
        audit.AffectedColumns = ChangedColumns.Count == 0 ? null : JsonSerializer.Serialize(ChangedColumns);
        return audit;
    }
}
