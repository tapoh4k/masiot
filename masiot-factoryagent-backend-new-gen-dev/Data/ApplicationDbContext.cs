using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TreeNode> TreeNodes { get; set; }
    public DbSet<AgentHost> Hosts { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<AgentParameter> AgentParameters { get; set; }
    public DbSet<ParameterDictionary> ParameterDictionary { get; set; }
    public DbSet<FileUpload> FileUploads { get; set; }
    public DbSet<Statistics> Statistics { get; set; }
    public DbSet<WorkflowTemplate> WorkflowTemplates { get; set; }
    public DbSet<WorkflowFunction> WorkflowFunctions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<WFList> WFLists { get; set; }
    public DbSet<Timetable> Timetables { get; set; }

    public DbSet<TechProcess> TechProcesses { get; set; }
    public DbSet<TechProcessOperation> TechProcessOperations { get; set; }
    public DbSet<PendingMessage> PendingMessages { get; set; }
    public DbSet<BlankOkpd2Mapping> BlankOkpd2Mappings { get; set; }
    public DbSet<Okpd2Dictionary> Okpd2Dictionary { get; set; }
    public DbSet<DrawingAnalysis> DrawingAnalyses { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TreeNode configuration
        modelBuilder.Entity<TreeNode>(entity =>
        {
            entity.HasOne(t => t.Parent)
                .WithMany(t => t.Children)
                .HasForeignKey(t => t.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.ParentId);
        });

        modelBuilder.Entity<AgentHost>(entity =>
        {
            entity.HasIndex(h => h.Type);
            entity.HasIndex(h => h.Name);
        });

        // Agent configuration
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasOne(a => a.TreeNode)
                .WithMany(t => t.Agents)
                .HasForeignKey(a => a.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Host)
                .WithMany(h => h.Agents)
                .HasForeignKey(a => a.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.NodeId);
            entity.HasIndex(a => a.HostId);

            // One agent -> one tech process (unique index in SQL schema) + FK handled by annotations
        });

        // TechProcess configuration
        modelBuilder.Entity<TechProcess>(entity =>
        {
            entity.HasIndex(tp => tp.Code);
        });

        modelBuilder.Entity<TechProcessOperation>(entity =>
        {
            entity.HasOne(o => o.TechProcess)
                .WithMany(tp => tp.Operations)
                .HasForeignKey(o => o.TechProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(o => o.TechProcessId);
        });

        modelBuilder.Entity<PendingMessage>(entity =>
        {
            entity.HasOne(pm => pm.Agent)
                .WithMany(a => a.PendingMessages)
                .HasForeignKey(pm => pm.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pm => pm.AgentId);
            entity.HasIndex(pm => pm.Status);
        });

        // AgentParameter configuration
        modelBuilder.Entity<AgentParameter>(entity =>
        {
            entity.HasOne(ap => ap.Agent)
                .WithMany(a => a.Parameters)
                .HasForeignKey(ap => ap.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ap => ap.AgentId);
        });

        // ParameterDictionary configuration
        modelBuilder.Entity<ParameterDictionary>(entity =>
        {
            entity.HasIndex(p => p.Name);
        });

        // BlankOkpd2Mapping configuration
        modelBuilder.Entity<BlankOkpd2Mapping>(entity =>
        {
            entity.HasIndex(m => m.Okpd2Code);
            entity.HasIndex(m => m.Keywords);
        });

        // FileUpload configuration
        modelBuilder.Entity<FileUpload>(entity =>
        {
            entity.HasIndex(f => f.UploadedAt);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is TreeNode treeNode)
            {
                if (entry.State == EntityState.Added)
                    treeNode.CreatedAt = DateTime.UtcNow;
                treeNode.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is AgentHost host)
            {
                if (entry.State == EntityState.Added)
                    host.CreatedAt = DateTime.UtcNow;
                host.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Agent agent)
            {
                if (entry.State == EntityState.Added)
                    agent.CreatedAt = DateTime.UtcNow;
                agent.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is AgentParameter agentParam)
            {
                if (entry.State == EntityState.Added)
                    agentParam.CreatedAt = DateTime.UtcNow;
                agentParam.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is ParameterDictionary param)
            {
                if (entry.State == EntityState.Added)
                    param.CreatedAt = DateTime.UtcNow;
                param.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is FileUpload fileUpload)
            {
                if (entry.State == EntityState.Added)
                    fileUpload.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Statistics stats)
            {
                stats.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is WorkflowTemplate workflow)
            {
                if (entry.State == EntityState.Added)
                    workflow.CreatedAt = DateTime.UtcNow;
                workflow.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is WorkflowFunction workflowFunction)
            {
                if (entry.State == EntityState.Added)
                    workflowFunction.CreatedAt = DateTime.UtcNow;
                workflowFunction.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is User user)
            {
                if (entry.State == EntityState.Added)
                    user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is TechProcess techProcess)
            {
                if (entry.State == EntityState.Added)
                    techProcess.CreatedAt = DateTime.UtcNow;
                techProcess.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is TechProcessOperation op)
            {
                if (entry.State == EntityState.Added)
                    op.CreatedAt = DateTime.UtcNow;
                op.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is PendingMessage pendingMessage)
            {
                if (entry.State == EntityState.Added)
                    pendingMessage.CreatedAt = DateTime.UtcNow;
                pendingMessage.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is BlankOkpd2Mapping blankMapping)
            {
                if (entry.State == EntityState.Added)
                    blankMapping.CreatedAt = DateTime.UtcNow;
                blankMapping.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Okpd2Dictionary okpd2Dictionary)
            {
                if (entry.State == EntityState.Added)
                    okpd2Dictionary.CreatedAt = DateTime.UtcNow;
                okpd2Dictionary.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
