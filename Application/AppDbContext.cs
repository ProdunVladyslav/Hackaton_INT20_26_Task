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
    public DbSet<NodeLeadCapture> NodeLeadCaptures => Set<NodeLeadCapture>();
    public DbSet<NodeLeadCaptureField> NodeLeadCaptureFields => Set<NodeLeadCaptureField>();
    public DbSet<NodeRedirect> NodeRedirects => Set<NodeRedirect>();
    public DbSet<NodeRedirectLink> NodeRedirectLinks => Set<NodeRedirectLink>();

    // ── Leads ─────────────────────────────────────────────────────────────────
    public DbSet<Lead> Leads => Set<Lead>();

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

            entity.HasMany(p => p.Flows)
              .WithOne(f => f.Owner)
              .HasForeignKey(f => f.OwnerId)
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

            entity.Property(f => f.OwnerId)
                .IsRequired();

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

        // ── NodeLeadCapture ───────────────────────────────────────────────────────
        // 1:1 with Node. Only exists when Node.Type == LeadCapture.
        // Cascade: removing the node removes its lead capture config.
        builder.Entity<NodeLeadCapture>(entity =>
        {
            entity.ToTable("NodeLeadCaptures");
            entity.HasKey(nlc => nlc.Id);

            entity.Property(nlc => nlc.IsRequired)
                .HasDefaultValue(true);

            // 1:1 with Node
            entity.HasOne<Node>()
                .WithOne(n => n.LeadCapture)
                .HasForeignKey<NodeLeadCapture>(nlc => nlc.NodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // NodeLeadCapture → Fields (1:N)
            entity.HasMany(nlc => nlc.Fields)
                .WithOne()
                .HasForeignKey(f => f.NodeLeadCaptureId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(nlc => nlc.Fields)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // ── NodeLeadCaptureField ──────────────────────────────────────────────────
        // Each field the builder toggled on for a LeadCapture node.
        builder.Entity<NodeLeadCaptureField>(entity =>
        {
            entity.ToTable("NodeLeadCaptureFields");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.FieldType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(f => f.IsRequired)
                .HasDefaultValue(false);

            entity.Property(f => f.DisplayOrder)
                .HasDefaultValue(0);

            entity.Property(f => f.Placeholder)
                .HasMaxLength(200);

            // AttributeKey is computed — not mapped to a column.
            entity.Ignore(f => f.AttributeKey);

            // One field type can only appear once per NodeLeadCapture.
            entity.HasIndex(f => new { f.NodeLeadCaptureId, f.FieldType })
                .IsUnique();

            entity.HasIndex(f => new { f.NodeLeadCaptureId, f.DisplayOrder });
        });

        // ── NodeRedirect ──────────────────────────────────────────────────────────
        builder.Entity<NodeRedirect>(entity =>
        {
            entity.ToTable("NodeRedirects");
            entity.HasKey(nr => nr.Id);

            entity.Property(nr => nr.RedirectUrl)
                .HasMaxLength(1000);

            entity.Property(nr => nr.AutoRedirectAfterSeconds);

            entity.Property(nr => nr.DisqualificationReason)
                .IsRequired()
                .HasMaxLength(200);

            // 1:1 with Node — inverse navigation wired explicitly
            entity.HasOne<Node>()
                .WithOne(n => n.Redirect)
                .HasForeignKey<NodeRedirect>(nr => nr.NodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // NodeRedirect → Links (1:N)
            entity.HasMany(nr => nr.Links)
                .WithOne()
                .HasForeignKey(l => l.NodeRedirectId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(nr => nr.Links)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // ── NodeRedirectLink ──────────────────────────────────────────────────────
        builder.Entity<NodeRedirectLink>(entity =>
        {
            entity.ToTable("NodeRedirectLinks");
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Label)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(l => l.Url)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(l => l.DisplayOrder)
                .HasDefaultValue(0);

            entity.HasIndex(l => new { l.NodeRedirectId, l.DisplayOrder });
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
        // Standalone aggregate — booking/CTA screen shown to qualified leads.
        // Not owned by Flow; reusable across multiple nodes.
        builder.Entity<Offer>(entity =>
        {
            entity.ToTable("Offers");
            entity.HasKey(o => o.Id);

            // Slug is the public URL-safe identifier.
            entity.Property(o => o.Slug)
                .IsRequired()
                .HasMaxLength(200);
            entity.HasIndex(o => o.Slug).IsUnique();

            // Name is internal — shown in the builder, not to the lead.
            entity.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(300);

            // Lead-facing content. Headline supports {{token}} substitution.
            entity.Property(o => o.Headline)
                .HasMaxLength(500);

            entity.Property(o => o.Body)
                .HasMaxLength(4000);

            entity.Property(o => o.ImageUrl)
                .HasMaxLength(1000);

            // Call to action
            entity.Property(o => o.CtaText)
                .HasMaxLength(300);

            entity.Property(o => o.CtaUrl)
                .HasMaxLength(1000);

            // Calendar booking — raw link, provider enum lives on NodeOffer.
            entity.Property(o => o.CalendarUrl)
                .HasMaxLength(1000);

            entity.Property(o => o.OwnerId).IsRequired();

            entity.HasOne<UserProfile>()
                .WithMany(p => p.Offers)
                .HasForeignKey(o => o.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── NodeOffer ─────────────────────────────────────────────────────────
        // Explicit many-to-many join between Node and Offer.
        // Carries qualification context for this specific node→offer link:
        // tier, calendar provider, and assigned sales rep.
        builder.Entity<NodeOffer>(entity =>
        {
            entity.ToTable("NodeOffers");
            entity.HasKey(no => no.Id);

            entity.Property(no => no.IsPrimary)
                .HasDefaultValue(false);

            // Qualification tier for this path — defaults to Hot since
            // Offer nodes are always qualified paths.
            entity.Property(no => no.Tier)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue(QualificationTier.Hot);

            // Which calendar provider to use when rendering CtaUrl.
            // Null = plain URL redirect, no embed.
            entity.Property(no => no.CalendarProvider)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Which sales rep gets notified when this offer is reached.
            // Null = notify flow owner.
            entity.Property(no => no.AssignedOwnerId);

            // NodeOffer → Node (M:1, cascade)
            // Removing a node removes all its offer links.
            entity.HasOne(no => no.Node)
                .WithMany()
                .HasForeignKey(no => no.NodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // NodeOffer → Offer (M:1, cascade)
            entity.HasOne(no => no.Offer)
                .WithMany()
                .HasForeignKey(no => no.OfferId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // NodeOffer → AssignedOwner UserProfile (M:1, optional)
            // SetNull: if the user profile is deleted, clear the assignment
            // rather than blocking deletion or orphaning the record.
            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(no => no.AssignedOwnerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

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
                .WithMany(s => s.Answers)
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

            entity.Property(so => so.ConvertedAt);
        });

        // ── Lead ──────────────────────────────────────────────────────────────────
        // Created on session complete when a LeadCapture node was traversed.
        // One lead per session maximum — enforced by unique index on SessionId.
        // Restrict on Session, Flow, and TerminalNode — leads are business records,
        // never silently dropped.
        builder.Entity<Lead>(entity =>
        {
            entity.ToTable("Leads");
            entity.HasKey(l => l.Id);

            // Identity
            entity.Property(l => l.Email)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(l => l.FullName)
                .HasMaxLength(300);

            entity.Property(l => l.Phone)
                .HasMaxLength(50);

            entity.Property(l => l.CompanyName)
                .HasMaxLength(300);

            entity.Property(l => l.JobTitle)
                .HasMaxLength(200);

            entity.Property(l => l.CompanySize)
                .HasMaxLength(100);

            entity.Property(l => l.Website)
                .HasMaxLength(500);

            // Qualification
            entity.Property(l => l.Score)
                .HasDefaultValue(0);

            entity.Property(l => l.Tier)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(l => l.DisqualificationReason)
                .HasMaxLength(500);

            entity.Property(l => l.TerminalNodeType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            // Sales rep workflow
            entity.Property(l => l.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue(LeadStatus.New);

            entity.Property(l => l.Notes)
                .HasMaxLength(4000);

            entity.Property(l => l.TimeToCompleteSeconds)
                .HasDefaultValue(0);

            entity.Property(l => l.CreatedAt)
                .IsRequired();

            // Lead → UserSession (1:1, restrict)
            // One lead per completed session. Session records must outlive leads.
            entity.HasOne<UserSession>()
                .WithOne()
                .HasForeignKey<Lead>(l => l.SessionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Lead → Flow (M:1, restrict)
            // Keep leads when flow is soft-deleted or archived.
            entity.HasOne<Flow>()
                .WithMany()
                .HasForeignKey(l => l.FlowId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Lead → TerminalNode (M:1, restrict)
            // Prevent deleting a node while lead records still reference it.
            entity.HasOne<Node>()
                .WithMany()
                .HasForeignKey(l => l.TerminalNodeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Lead → AssignedTo UserProfile (M:1, optional, set null)
            // If the assigned rep's profile is deleted, clear the assignment
            // rather than blocking deletion or orphaning the lead.
            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(l => l.AssignedToId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Queries: all leads for a flow (leads table view)
            entity.HasIndex(l => l.FlowId);

            // Queries: filter by tier and status (most common dashboard filters)
            entity.HasIndex(l => new { l.FlowId, l.Tier });
            entity.HasIndex(l => new { l.FlowId, l.Status });

            // Queries: dedup check — find existing lead by email within a flow
            entity.HasIndex(l => new { l.FlowId, l.Email });

            // One lead per session — enforced at DB level
            entity.HasIndex(l => l.SessionId)
                .IsUnique();
        });
    }
}
