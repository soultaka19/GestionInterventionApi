using Microsoft.EntityFrameworkCore;
using GestionInterventionApi.Models;
using GestionInterventionApi.Services;

namespace GestionInterventionApi.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantService _tenantService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    // DbSets
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TechnicianLocation> TechnicianLocations => Set<TechnicianLocation>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Intervention> Interventions => Set<Intervention>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration Organization
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SubscriptionPlan).HasConversion<string>();
        });

        // Configuration User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Role).HasConversion<string>();

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuration TechnicianLocation
        modelBuilder.Entity<TechnicianLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TechnicianId).IsUnique();
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();

            entity.HasOne(e => e.Technician)
                .WithOne()
                .HasForeignKey<TechnicianLocation>(e => e.TechnicianId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuration Client
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(256);

            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuration Equipment
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.SerialNumber).HasMaxLength(50);

            entity.HasOne(e => e.Client)
                .WithMany(c => c.Equipments)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuration Intervention
        modelBuilder.Entity<Intervention>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Equipment)
                .WithMany()
                .HasForeignKey(e => e.EquipmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Technician)
                .WithMany()
                .HasForeignKey(e => e.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index pour les requêtes fréquentes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledDate);
            entity.HasIndex(e => e.TechnicianId);
        });

        // Configuration globale pour les entités multi-tenant
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(ApplyTenantQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void ApplyTenantQueryFilter<T>(ModelBuilder modelBuilder) where T : class, ITenantEntity
    {
        // FAIL-CLOSED. Le filtre disait auparavant :
        //     _tenantService.OrganizationId == null || e.OrganizationId == ...
        // c'est-a-dire : « si je ne sais pas a quelle organisation appartient
        // l'appelant, montre-lui TOUT ». Or le TenantMiddleware n'agit que sur le
        // pipeline HTTP : une invocation de methode SignalR s'execute dans un
        // scope DI neuf ou OrganizationId vaut null. Un utilisateur authentifie
        // connaissant un GUID pouvait ainsi lire la position et le nom d'un
        // technicien de n'importe quelle organisation.
        //
        // Desormais, un tenant inconnu ne voit RIEN : la comparaison avec un
        // Guid? nul ne satisfait aucune ligne cote SQL.
        //
        // Les deux chemins qui doivent legitimement voir hors tenant (login et
        // register, qui cherchent un utilisateur avant de connaitre son
        // organisation) passent deja par IgnoreQueryFilters() : ils ne
        // dependaient pas de cette ouverture.
        modelBuilder.Entity<T>().HasQueryFilter(e =>
            e.OrganizationId == _tenantService.OrganizationId);
    }

    public override int SaveChanges()
    {
        SetTenantIdOnEntities();
        SetTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantIdOnEntities();
        SetTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantIdOnEntities()
    {
        var entries = ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (_tenantService.OrganizationId.HasValue && entry.Entity.OrganizationId == Guid.Empty)
            {
                entry.Entity.OrganizationId = _tenantService.OrganizationId.Value;
            }
        }
    }

    private void SetTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
