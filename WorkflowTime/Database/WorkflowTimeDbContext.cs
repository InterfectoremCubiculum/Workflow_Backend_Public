using Microsoft.EntityFrameworkCore;
using WorkflowTime.Features.DayOffs.Models;
using WorkflowTime.Features.Notifications.Models;
using WorkflowTime.Features.ProjectManagement.Models;
using WorkflowTime.Features.TeamManagement.Models;
using WorkflowTime.Features.UserManagment.Models;
using WorkflowTime.Features.WorkLog.Models;

namespace WorkflowTime.Database
{
    public class WorkflowTimeDbContext : DbContext
    {
        public DbSet<DayOffRequest> DayOffRequests { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<SettingModel> Settings { get; set; }
        public DbSet<TimeSegment> TimeSegments { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public WorkflowTimeDbContext(DbContextOptions<WorkflowTimeDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SettingModel>()
                .Property(e => e.Type)
                .HasConversion<string>();

            modelBuilder.Entity<DayOffRequest>()
                .Property(e => e.RequestStatus)
                .HasConversion<string>();

            modelBuilder.Entity<TimeSegment>()
                .Property(e => e.TimeSegmentType)
                .HasConversion<string>();

            modelBuilder.Entity<UserModel>()
                .HasOne(u => u.Team)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TimeSegment>()
                .HasOne(wds => wds.User)
                .WithMany()
                .HasForeignKey(wds => wds.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TimeSegment>(
                entity =>
                {
                    entity.Property(e => e.DurationInSeconds)
                        .HasComputedColumnSql("CASE WHEN EndTime IS NULL THEN NULL ELSE DATEDIFF(second, StartTime, EndTime) END", stored: true);
                });

            modelBuilder.Entity<DayOffRequest>()
                .HasOne(dor => dor.User)
                .WithMany()
                .HasForeignKey(dor => dor.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserModel>()
            .HasMany(u => u.DayOffRequests)
            .WithOne(d => d.User)
            .HasForeignKey(d => d.UserId);

            modelBuilder.Entity<UserModel>()
                .HasMany(u => u.TimeSegments)
                .WithOne(w => w.User)
                .HasForeignKey(w => w.UserId);
        }
    }
}
