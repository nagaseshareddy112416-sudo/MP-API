using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GfcEngine.Application.Common.Interfaces;
using GfcEngine.Domain.Entities;

namespace GfcEngine.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<CadLayerMapping> CadLayerMappings => Set<CadLayerMapping>();
    public DbSet<CategoryConfig> CategoryConfigs => Set<CategoryConfig>();
    public DbSet<CategoryFieldConfig> CategoryFieldConfigs => Set<CategoryFieldConfig>();
    public DbSet<UploadedDrawing> UploadedDrawings => Set<UploadedDrawing>();
    public DbSet<DrawingRevision> DrawingRevisions => Set<DrawingRevision>();
    public DbSet<ExtractedFeature> ExtractedFeatures => Set<ExtractedFeature>();
    public DbSet<Clash> Clashes => Set<Clash>();
    public DbSet<ReferenceLayer> ReferenceLayers => Set<ReferenceLayer>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkspaceUserRole> WorkspaceUserRoles => Set<WorkspaceUserRole>();
    public DbSet<DrawingStage> DrawingStages => Set<DrawingStage>();
    public DbSet<ToleranceRule> ToleranceRules => Set<ToleranceRule>();
    public DbSet<FeatureMatch> FeatureMatches => Set<FeatureMatch>();
    public DbSet<ComplianceResult> ComplianceResults => Set<ComplianceResult>();
    public DbSet<Deviation> Deviations => Set<Deviation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Workspace
        builder.Entity<Workspace>(b =>
        {
            b.HasKey(w => w.Id);
            b.Property(w => w.Name).IsRequired().HasMaxLength(200);
            b.Property(w => w.Code).IsRequired().HasMaxLength(50);
            b.HasIndex(w => w.Code).IsUnique();
            b.Property(w => w.BoundaryGeometry).HasColumnType("geometry");
        });

        // CadLayerMapping
        builder.Entity<CadLayerMapping>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.CadLayer).IsRequired().HasMaxLength(100);
            b.Property(m => m.Category).IsRequired().HasMaxLength(100);
            b.Property(m => m.SubType).IsRequired().HasMaxLength(100);
            b.Property(m => m.GeometryType).IsRequired().HasMaxLength(50);
            b.Property(m => m.Color).HasMaxLength(20);
            b.HasOne(m => m.Workspace)
                .WithMany(w => w.CadLayerMappings)
                .HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(m => new { m.WorkspaceId, m.CadLayer });
        });

        // CategoryConfig
        builder.Entity<CategoryConfig>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);
            b.Property(c => c.SubTypeName).IsRequired().HasMaxLength(100);
            b.Property(c => c.ColorHex).HasMaxLength(20);
            b.Property(c => c.IconName).HasMaxLength(50);
            b.HasOne(c => c.Workspace)
                .WithMany(w => w.CategoryConfigs)
                .HasForeignKey(c => c.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CategoryFieldConfig
        builder.Entity<CategoryFieldConfig>(b =>
        {
            b.HasKey(f => f.Id);
            b.Property(f => f.CategoryName).IsRequired().HasMaxLength(100);
            b.Property(f => f.FieldName).IsRequired().HasMaxLength(100);
            b.Property(f => f.FieldLabel).IsRequired().HasMaxLength(200);
            b.Property(f => f.FieldType).IsRequired().HasMaxLength(50);
            b.HasOne(f => f.Workspace)
                .WithMany(w => w.CategoryFieldConfigs)
                .HasForeignKey(f => f.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UploadedDrawing
        builder.Entity<UploadedDrawing>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.ProjectName).IsRequired().HasMaxLength(200);
            b.Property(d => d.Category).IsRequired().HasMaxLength(100);
            b.Property(d => d.SubType).IsRequired().HasMaxLength(100);
            b.Property(d => d.CurrentRevision).HasMaxLength(20);
            b.Property(d => d.PinLocation).HasColumnType("geometry");
            b.Property(d => d.DisplayPinLocation).HasColumnType("geometry");
            b.HasOne(d => d.Workspace)
                .WithMany(w => w.Drawings)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(d => d.GfcDrawing)
                .WithMany()
                .HasForeignKey(d => d.GfcDrawingId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(d => d.Stage)
                .WithMany()
                .HasForeignKey(d => d.StageId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasIndex(d => new { d.WorkspaceId, d.Status });
        });

        // DrawingRevision
        builder.Entity<DrawingRevision>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.RevisionNumber).IsRequired().HasMaxLength(20);
            b.HasOne(r => r.Drawing)
                .WithMany(d => d.Revisions)
                .HasForeignKey(r => r.DrawingId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(r => r.Workspace)
                .WithMany()
                .HasForeignKey(r => r.WorkspaceId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasIndex(r => new { r.DrawingId, r.RevisionNumber });
        });

        // ExtractedFeature
        builder.Entity<ExtractedFeature>(b =>
        {
            b.HasKey(f => f.Id);
            b.Property(f => f.CadLayer).IsRequired().HasMaxLength(100);
            b.Property(f => f.Category).IsRequired().HasMaxLength(100);
            b.Property(f => f.SubType).IsRequired().HasMaxLength(100);
            b.Property(f => f.GeometryType).IsRequired().HasMaxLength(50);
            b.Property(f => f.NativeGeometry).IsRequired().HasColumnType("geometry");
            b.Property(f => f.DisplayGeometry).IsRequired().HasColumnType("geometry");
            b.HasOne(f => f.Drawing)
                .WithMany(d => d.Features)
                .HasForeignKey(f => f.DrawingId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(f => f.Revision)
                .WithMany(r => r.Features)
                .HasForeignKey(f => f.RevisionId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(f => f.Workspace)
                .WithMany()
                .HasForeignKey(f => f.WorkspaceId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasIndex(f => new { f.WorkspaceId, f.IsActive });
            b.HasIndex(f => new { f.RevisionId, f.IsActive });
        });

        // Clash
        builder.Entity<Clash>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Location).IsRequired().HasColumnType("geometry");
            b.Property(c => c.DisplayLocation).IsRequired().HasColumnType("geometry");
            b.HasOne(c => c.FeatureA)
                .WithMany()
                .HasForeignKey(c => c.FeatureAId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.FeatureB)
                .WithMany()
                .HasForeignKey(c => c.FeatureBId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.DrawingA)
                .WithMany()
                .HasForeignKey(c => c.DrawingAId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.DrawingB)
                .WithMany()
                .HasForeignKey(c => c.DrawingBId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.Workspace)
                .WithMany(w => w.Clashes)
                .HasForeignKey(c => c.WorkspaceId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasIndex(c => new { c.WorkspaceId, c.Status });
        });

        // ReferenceLayer
        builder.Entity<ReferenceLayer>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Name).IsRequired().HasMaxLength(200);
            b.Property(r => r.Format).IsRequired().HasMaxLength(50);
            b.Property(r => r.DisplayGeometry).HasColumnType("geometry");
            b.HasOne(r => r.Workspace)
                .WithMany(w => w.ReferenceLayers)
                .HasForeignKey(r => r.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog
        builder.Entity<AuditLog>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Action).IsRequired().HasMaxLength(50);
            b.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            b.Property(a => a.UserId).IsRequired().HasMaxLength(100);
            b.HasOne(a => a.Workspace)
                .WithMany(w => w.AuditLogs)
                .HasForeignKey(a => a.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(a => new { a.WorkspaceId, a.Timestamp });
        });

        // WorkspaceUserRole
        builder.Entity<WorkspaceUserRole>(b =>
        {
            b.HasKey(ur => ur.Id);
            b.Property(ur => ur.Role).IsRequired().HasMaxLength(50);
            b.HasOne(ur => ur.User)
                .WithMany(u => u.WorkspaceRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(ur => ur.Workspace)
                .WithMany(w => w.UserRoles)
                .HasForeignKey(ur => ur.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(ur => new { ur.WorkspaceId, ur.UserId }).IsUnique();
        });

        // DrawingStage
        builder.Entity<DrawingStage>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).IsRequired().HasMaxLength(100);
            b.HasOne(s => s.Workspace)
                .WithMany()
                .HasForeignKey(s => s.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(s => new { s.WorkspaceId, s.StageOrder });
        });

        // ToleranceRule
        builder.Entity<ToleranceRule>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Category).IsRequired().HasMaxLength(100);
            b.Property(t => t.SubType).IsRequired().HasMaxLength(100);
            b.HasOne(t => t.Workspace)
                .WithMany()
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(t => new { t.WorkspaceId, t.Category, t.SubType });
        });

        // FeatureMatch
        builder.Entity<FeatureMatch>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasOne(m => m.AsBuiltDrawing)
                .WithMany()
                .HasForeignKey(m => m.AsBuiltDrawingId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(m => m.GfcDrawing)
                .WithMany()
                .HasForeignKey(m => m.GfcDrawingId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(m => m.AsBuiltFeature)
                .WithMany()
                .HasForeignKey(m => m.AsBuiltFeatureId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(m => m.GfcFeature)
                .WithMany()
                .HasForeignKey(m => m.GfcFeatureId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(m => m.Workspace)
                .WithMany()
                .HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ComplianceResult
        builder.Entity<ComplianceResult>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Category).IsRequired().HasMaxLength(100);
            b.Property(c => c.SubType).IsRequired().HasMaxLength(100);
            b.HasOne(c => c.AsBuiltDrawing)
                .WithMany()
                .HasForeignKey(c => c.AsBuiltDrawingId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.GfcDrawing)
                .WithMany()
                .HasForeignKey(c => c.GfcDrawingId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.GfcRevision)
                .WithMany()
                .HasForeignKey(c => c.GfcRevisionId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.FeatureMatch)
                .WithMany()
                .HasForeignKey(c => c.FeatureMatchId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.AsBuiltFeature)
                .WithMany()
                .HasForeignKey(c => c.AsBuiltFeatureId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.GfcFeature)
                .WithMany()
                .HasForeignKey(c => c.GfcFeatureId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(c => c.Workspace)
                .WithMany()
                .HasForeignKey(c => c.WorkspaceId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasIndex(c => new { c.WorkspaceId, c.AsBuiltDrawingId, c.Status });
        });

        // Deviation
        builder.Entity<Deviation>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Reason).HasMaxLength(1000);
            b.Property(d => d.DecidedBy).HasMaxLength(100);
            b.HasOne(d => d.ComplianceResult)
                .WithMany(c => c.Deviations)
                .HasForeignKey(d => d.ComplianceResultId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(d => d.Workspace)
                .WithMany()
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
