using Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
namespace Common.Data
{
    
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<UserModel> Users => Set<UserModel>();
        public DbSet<TaskModel> Tasks => Set<TaskModel>();
        public DbSet<TimeEntryModel> TimeEntries => Set<TimeEntryModel>();
        public DbSet<CommentModel> Comments => Set<CommentModel>();

        public DbSet<ThreadSessionModel> ThreadSessions => Set<ThreadSessionModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // (Many-to-Many)
            // TaskModel to UserModel.
            modelBuilder.Entity<TaskModel>()
                .HasMany(t => t.Users)
                .WithMany(u => u.Tasks)
                .UsingEntity(j => j.ToTable("TaskUser"));

            modelBuilder.Entity<UserModel>()
                .HasKey(u => u.UserId);

            modelBuilder.Entity<TaskModel>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TaskModel>()
            .ToTable(tb => tb.HasCheckConstraint(
                "CHK_DueDate_After_StartDate",
                "\"DueDate\" IS NULL OR \"DueDate\" > \"StartDate\""
            ));

            modelBuilder.Entity<TimeEntryModel>()
                .HasKey(te => te.Id);
            modelBuilder.Entity<TimeEntryModel>()
                .HasOne(te => te.Task)
                .WithMany(t => t.TimeEntries)
                .HasForeignKey(te => te.LocalTaskId);
            modelBuilder.Entity<TimeEntryModel>()
                .HasOne(te => te.User)
                .WithMany(u => u.TimeEntries)
                .HasForeignKey(te => te.UserId);
            modelBuilder.Entity<TimeEntryModel>()
                .ToTable(tb => tb.HasCheckConstraint(
                    "CHK_EndTime_After_StartTime",
                    "\"EndTime\" IS NULL OR \"EndTime\" >= \"StartTime\""
                ));

            modelBuilder.Entity<CommentModel>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<CommentModel>()
                .HasOne(c => c.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.LocalTaskId);
            modelBuilder.Entity<CommentModel>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId);


        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<TaskModel>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    entry.Entity.NormalizedTitle = NormalizeAsci(entry.Entity.Title);
                }
            }
            foreach (var entry in ChangeTracker.Entries<UserModel>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    entry.Entity.NormalizedDisplayName = NormalizeAsci(entry.Entity.DisplayName);
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        private static string NormalizeAsci(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var normalized = name.ToLowerInvariant()
                                  .Replace(" ", "")
                                  .Replace("ą", "a")
                                  .Replace("ć", "c")
                                  .Replace("ę", "e")
                                  .Replace("ł", "l")
                                  .Replace("ń", "n")
                                  .Replace("ó", "o")
                                  .Replace("ś", "s")
                                  .Replace("ż", "z")
                                  .Replace("ź", "z");

            return normalized;
        }
    }
}