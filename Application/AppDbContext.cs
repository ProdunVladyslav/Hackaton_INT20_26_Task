using Domain.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Application;

/// <summary>
/// The single EF Core database context for the entire application.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Profiles table. Use Set&lt;T&gt;() pattern to avoid nullable DbSet warnings.</summary>
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // MUST call base first — it registers all Identity entity configurations.
        // Skipping this breaks logins, role checks, token storage, etc.
        base.OnModelCreating(builder);

        // ── ApplicationUser ───────────────────────────────────────────────────
        builder.Entity<ApplicationUser>(entity =>
        {
            // Rename the default "AspNetUsers" table to something cleaner.
            entity.ToTable("Users");
        });

        // ── UserProfile ───────────────────────────────────────────────────────
        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");

            entity.HasKey(p => p.Id);

            // PostgreSQL-native UUID generation. Relies on the pgcrypto extension
            // (available by default in modern Postgres). Alternative: use C#-side
            // Guid.NewGuid() by removing this line and setting Id in the constructor.
            entity.Property(p => p.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            // ── 1-to-1 Relationship with ApplicationUser ──────────────────────
            //
            // HasOne  → UserProfile has ONE ApplicationUser
            // WithOne → ApplicationUser has ONE UserProfile (back-reference via Profile)
            // HasForeignKey<UserProfile> → the FK column lives on the UserProfile side
            //
            // EF Core automatically adds a UNIQUE index on ApplicationUserId,
            // which is what actually enforces "1-to-1" at the database level.
            // Without that index it would just be "many profiles per user" (many-to-one).
            //
            // Cascade: deleting the ApplicationUser automatically deletes the profile.
            entity.HasOne(p => p.ApplicationUser)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(p => p.ApplicationUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
