using Microsoft.EntityFrameworkCore;

namespace MediaFilesManagementSystem.Data;

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; init; }
    public DbSet<VideoFile> VideoFiles { get; init; }

    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        => Database.EnsureCreated();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<User>().HasData(new User[]
        {
            new()
            {
                Id = 1,
                Name = "Admin",
                Password = "Admin12345",
                Role = Role.Administrator
            },
            new()
            {
                Id = 2,
                Name = "User",
                Password = "User123456",
                Role = Role.User
            }
        });

        string videoFilesTableName = nameof(VideoFile) + "s";
        string statePropertyName = nameof(VideoFile.State);
        string beingReplacedByFKName = nameof(VideoFile.BeingReplacedById);
        string beingReplacedOnFKName = nameof(VideoFile.BeingReplacedOnId);
        string deletingByFKName = nameof(VideoFile.DeletingById);
        string enumValues = string.Join(", ", Enum.GetValues<VideoFileState>().Cast<byte>());
        byte beingReplacedValue = (byte)VideoFileState.BeingReplaced;
        byte deletingValue = (byte)VideoFileState.Deleting;

        modelBuilder.Entity<VideoFile>()
            .HasCheckConstraint($"CK_{ videoFilesTableName }_{ statePropertyName }",
                $"[{ statePropertyName }] IN ({ enumValues })")
            .HasCheckConstraint($"CK_{ videoFilesTableName }_{ beingReplacedByFKName }",
                $"[{ beingReplacedByFKName }] IS NULL AND [{ statePropertyName }] != { beingReplacedValue } OR [{ beingReplacedByFKName }] IS NOT NULL AND [{ statePropertyName }] = { beingReplacedValue }")
            .HasCheckConstraint($"CK_{ videoFilesTableName }_{ beingReplacedOnFKName }",
                $"[{ beingReplacedOnFKName }] IS NULL AND [{ statePropertyName }] != { beingReplacedValue } OR [{ beingReplacedOnFKName }] IS NOT NULL AND [{ statePropertyName }] = { beingReplacedValue }")
            .HasCheckConstraint($"CK_{ videoFilesTableName }_{ deletingByFKName }",
                $"[{ deletingByFKName }] IS NULL AND [{ statePropertyName }] != { deletingValue } OR [{ deletingByFKName }] IS NOT NULL AND [{ statePropertyName }] = { deletingValue }");

        base.OnModelCreating(modelBuilder);
    }
}
