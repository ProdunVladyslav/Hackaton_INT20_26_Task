using Domain.Model.AdminProfile;
using Domain.Model.Auth;
using Domain.Model.Survey;
using Domain.Model.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Application;

/// <summary>
/// The single EF Core database context for the entire application.
///
/// Naming convention used throughout:
///   • ToTable()               — explicit table names (snake_case-free, PascalCase)
///   • HasMaxLength()          — every text column has a cap (avoids TEXT by default)
///   • HasConversion&lt;string&gt;() — enums stored as readable strings, not magic ints
///   • PropertyAccessMode.Field — navigation collections backed by private List&lt;T&gt; fields
///
/// Relationship summary:
///   ApplicationUser  1──1  UserProfile
///   Flow             1──N  Node         (cascade delete)
///   Flow             1──N  Edge         (cascade delete)
///   Flow             1──N  UserSession  (restrict — keep analytics when flow is deleted)
///   Node             1──N  Option       (cascade delete)
///   Node             1──N  NodeOffer    (cascade delete)
///   Offer            1──N  NodeOffer    (restrict — offer must be unlinked before deletion)
///   Offer            1──N  SessionOffer (restrict)
///   UserSession      1──N  UserAnswer   (cascade delete)
///   UserSession      1──N  SessionOffer (cascade delete)
///   Edge         M──1  Node (source)    (restrict — edges must be removed before node)
///   Edge         M──1  Node (target)    (restrict)
///   UserAnswer   M──1  Node             (restrict)
///   UserSession  M──1  Node (current)   (restrict)
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Identity / Admin ──────────────────────────────────────────────────────
    public DbSet<UserProfile>   UserProfiles  => Set<UserProfile>();

    // ── Survey / Flow ─────────────────────────────────────────────────────────
    public DbSet<Flow>          Flows         => Set<Flow>();
    public DbSet<Node>          Nodes         => Set<Node>();
    public DbSet<Edge>          Edges         => Set<Edge>();
    public DbSet<Option>        Options       => Set<Option>();
    public DbSet<Offer>         Offers        => Set<Offer>();
    public DbSet<NodeOffer>     NodeOffers    => Set<NodeOffer>();

    // ── User activity ─────────────────────────────────────────────────────────
    public DbSet<UserSession>   UserSessions  => Set<UserSession>();
    public DbSet<UserAnswer>    UserAnswers   => Set<UserAnswer>();
    public DbSet<SessionOffer>  SessionOffers => Set<SessionOffer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // ── IMPORTANT: always call base first ────────────────────────────────
        // Registers all ASP.NET Core Identity entity configurations (Users, Roles,
        // Claims, Tokens, UserLogins, UserRoles, RoleClaims). Skipping this call
        // breaks logins, role checks, and token-based email confirmation.
        base.OnModelCreating(builder);

        // =====================================================================
        //  IDENTITY
        // =====================================================================

        // ── ApplicationUser ───────────────────────────────────────────────────
        // Rename the default "AspNetUsers" table to something cleaner.
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
        });

        // ── UserProfile ───────────────────────────────────────────────────────
        // 1:1 with ApplicationUser. FK lives on UserProfile side.
        // EF Core automatically adds a UNIQUE index on ApplicationUserId, which
        // is what actually enforces 1-to-1 at the database level.
        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");

            entity.HasKey(p => p.Id);

            // Use PostgreSQL's native UUID generation rather than C#-side Guid.NewGuid()
            // so the DB always provides a fresh UUID even if the C# code forgets to.
            entity.Property(p => p.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(p => p.ApplicationUser)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(p => p.ApplicationUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================================
        //  SURVEY — FLOW AGGREGATE
        // =====================================================================

        // ── Flow ──────────────────────────────────────────────────────────────
        // Aggregate root. Owns Nodes and Edges. The two navigation collections
        // (_nodes, _edges) are private backing fields — EF must be told to
        // read/write through the field, not the read-only property.
        builder.Entity<Flow>(entity =>
        {
            entity.ToTable("Flows");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(f => f.Description)
                .HasMaxLength(2000);

            entity.Property(f => f.IsPublished)
                .HasDefaultValue(false);

            // EntryNodeId is a soft pointer: nullable, no FK constraint at DB
            // level to avoid circular dependency (Flow → Node → Flow).
            // Application logic validates it on SetEntryNode() and Publish().
            entity.Property(f => f.EntryNodeId);

            // Flow → Nodes (1:N)
            // Deleting a flow deletes all its nodes (and by cascade their options).
            entity.HasMany(f => f.Nodes)
                .WithOne()
                .HasForeignKey(n => n.FlowId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Flow → Edges (1:N)
            // Edges are deleted when the flow is deleted.
            entity.HasMany(f => f.Edges)
                .WithOne()
                .HasForeignKey(e => e.FlowId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Tell EF Core to materialise rows into the private backing fields.
            entity.Navigation(f => f.Nodes).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(f => f.Edges).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // ── Node ──────────────────────────────────────────────────────────────
        // Belongs to one Flow. Owns a collection of Options.
        // _options is a private backing field.
        builder.Entity<Node>(entity =>
        {
            entity.ToTable("Nodes");
            entity.HasKey(n => n.Id);

            // Store enum as human-readable string ("Question", "InfoPage", "Offer")
            // so the DB is self-documenting and migrations never break on reorder.
            entity.Property(n => n.Type)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(500);

            // AttributeKey is required only for Question nodes (enforced in domain);
            // keep nullable at DB level so InfoPage/Offer nodes can omit it.
            entity.Property(n => n.AttributeKey)
                .HasMaxLength(200);

            entity.Property(n => n.Description)
                .HasMaxLength(2000);

            entity.Property(n => n.MediaUrl)
                .HasMaxLength(1000);

            // AnswerType stored as human-readable string; nullable (non-Question nodes leave it null).
            entity.Property(n => n.AnswerType)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(n => n.SliderMin)
                .HasColumnType("numeric(18,4)");

            entity.Property(n => n.SliderMax)
                .HasColumnType("numeric(18,4)");

            // Node → Options (1:N)
            // Deleting a node deletes all its answer options.
            entity.HasMany(n => n.Options)
                .WithOne()
                .HasForeignKey(o => o.NodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(n => n.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // ── Edge ──────────────────────────────────────────────────────────────
        // Directed connection between two nodes inside a flow.
        // SourceNodeId / TargetNodeId use Restrict so you can't silently orphan
        // an edge by deleting one of its endpoint nodes directly — the caller
        // must remove affected edges before removing the node.
        builder.Entity<Edge>(entity =>
        {
            entity.ToTable("Edges");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Priority)
                .HasDefaultValue(0);

            // Store conditions as free-form JSON text.
            entity.Property(e => e.ConditionsJson)
                .HasColumnType("text");

            // Edge → Source Node (M:1, restrict)
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(e => e.SourceNodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Edge → Target Node (M:1, restrict)
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(e => e.TargetNodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Composite index speeds up "find all edges leaving node X".
            entity.HasIndex(e => new { e.FlowId, e.SourceNodeId });
        });

        // ── Option ────────────────────────────────────────────────────────────
        // An answer choice on a Question node. Owned by one Node.
        builder.Entity<Option>(entity =>
        {
            entity.ToTable("Options");
            entity.HasKey(o => o.Id);

            entity.Property(o => o.Label)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(o => o.Value)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(o => o.MediaUrl)
                .HasMaxLength(1000);

            entity.Property(o => o.DisplayOrder)
                .HasDefaultValue(0);

            // Index for ordered rendering.
            entity.HasIndex(o => new { o.NodeId, o.DisplayOrder });
        });

        // =====================================================================
        //  SURVEY — OFFER
        // =====================================================================

        // ── Offer ─────────────────────────────────────────────────────────────
        // Standalone aggregate — products/services presented at offer nodes.
        // Not owned by Flow; reusable across multiple nodes and sessions.
        builder.Entity<Offer>(entity =>
        {
            entity.ToTable("Offers");
            entity.HasKey(o => o.Id);

            // Slug is the public identifier (URL-safe, human-readable).
            entity.Property(o => o.Slug)
                .IsRequired()
                .HasMaxLength(200);

            // Enforce slug uniqueness at the DB level.
            entity.HasIndex(o => o.Slug)
                .IsUnique();

            entity.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(o => o.Description)
                .HasMaxLength(4000);

            entity.Property(o => o.Duration)
                .HasMaxLength(200);

            entity.Property(o => o.DigitalContent)
                .HasMaxLength(2000);

            entity.Property(o => o.PhysicalWellnessKitName)
                .HasMaxLength(300);

            entity.Property(o => o.PhysicalWellnessKitItems)
                .HasMaxLength(4000);

            // Price: 18 significant digits, 2 decimal places. Nullable = free tier.
            entity.Property(o => o.Price)
                .HasColumnType("numeric(18,2)");

            entity.Property(o => o.ImageUrl)
                .HasMaxLength(1000);

            entity.Property(o => o.CtaText)
                .HasMaxLength(300);

            entity.Property(o => o.CtaUrl)
                .HasMaxLength(1000);
        });

        // ── NodeOffer ─────────────────────────────────────────────────────────
        // Explicit many-to-many join between Node and Offer.
        // IsPrimary flags the "recommended" offer when multiple are shown.
        builder.Entity<NodeOffer>(entity =>
        {
            entity.ToTable("NodeOffers");
            entity.HasKey(no => no.Id);

            entity.Property(no => no.IsPrimary)
                .HasDefaultValue(false);

            // NodeOffer → Node (M:1)
            // Cascade: removing a node removes all its offer links.
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(no => no.NodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // NodeOffer → Offer (M:1)
            // Restrict: cannot delete an Offer while it is still linked to a node.
            // Callers must unlink first (remove all NodeOffers for the offer).
            entity.HasOne<Offer>()
                .WithMany()
                .HasForeignKey(no => no.OfferId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // One offer can only appear once per node.
            entity.HasIndex(no => new { no.NodeId, no.OfferId })
                .IsUnique();
        });

        // =====================================================================
        //  USER ACTIVITY
        // =====================================================================

        // ── UserSession ───────────────────────────────────────────────────────
        // Tracks a single end-user's journey through a published flow.
        builder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(s => s.UtmSource)
                .HasMaxLength(200);

            entity.Property(s => s.UtmCampaign)
                .HasMaxLength(200);

            entity.Property(s => s.StartedAt)
                .IsRequired();

            entity.Property(s => s.CompletedAt);

            // UserSession → Flow (M:1, restrict)
            // We never want to silently drop analytics data when a flow is deleted.
            // The admin must archive or explicitly handle sessions first.
            entity.HasOne<Flow>()
                .WithMany()
                .HasForeignKey(s => s.FlowId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // UserSession.CurrentNodeId → Node (M:1, restrict)
            // Prevent deleting a node while a live session is positioned on it.
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(s => s.CurrentNodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Index for retrieving all sessions for a given flow (analytics queries).
            entity.HasIndex(s => s.FlowId);

            // Index for finding active sessions quickly.
            entity.HasIndex(s => s.Status);
        });

        // ── UserAnswer ────────────────────────────────────────────────────────
        // One recorded answer per node per session.
        builder.Entity<UserAnswer>(entity =>
        {
            entity.ToTable("UserAnswers");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.AttributeKey)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(a => a.Value)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(a => a.AnsweredAt)
                .IsRequired();

            // UserAnswer → UserSession (M:1, cascade)
            // Deleting a session erases all its recorded answers.
            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(a => a.SessionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // UserAnswer → Node (M:1, restrict)
            // Prevents deleting a node while answer records still reference it.
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(a => a.NodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Index for quickly loading all answers of a session (answer summary page).
            entity.HasIndex(a => a.SessionId);
        });

        // ── SessionOffer ──────────────────────────────────────────────────────
        // Records which offers were presented during a session and whether the
        // user converted (clicked CTA / purchased).
        builder.Entity<SessionOffer>(entity =>
        {
            entity.ToTable("SessionOffers");
            entity.HasKey(so => so.Id);

            entity.Property(so => so.IsPrimary)
                .HasDefaultValue(false);

            entity.Property(so => so.Converted)
                .HasDefaultValue(false);

            entity.Property(so => so.PresentedAt)
                .IsRequired();

            // SessionOffer → UserSession (M:1, cascade)
            // Deleting a session erases all offer presentation records for it.
            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(so => so.SessionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // SessionOffer → Offer (M:1, restrict)
            // Cannot delete an Offer while conversion history still references it.
            entity.HasOne<Offer>()
                .WithMany()
                .HasForeignKey(so => so.OfferId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Index for offer performance analytics: "how many times was offer X shown?"
            entity.HasIndex(so => so.OfferId);

            // Index for retrieving all offers shown in a session.
            entity.HasIndex(so => so.SessionId);
        });
    }
}
