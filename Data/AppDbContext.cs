using Microsoft.EntityFrameworkCore;
using YourApp.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Search> Searches { get; set; }
    public DbSet<Database> Databases { get; set; }
    public DbSet<Record> Records { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Search
        modelBuilder.Entity<Search>(entity =>
        {
            entity.ToTable("searches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Query).HasColumnName("query").IsRequired();
            entity.Property(e => e.QueryType).HasColumnName("query_type").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").IsRequired(false);
            entity.Property(e => e.Limit).HasColumnName("limit");
            entity.Property(e => e.Language).HasColumnName("language");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        // Configure Database
        modelBuilder.Entity<Database>(entity =>
        {
            entity.ToTable("databases");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SearchId).HasColumnName("search_id").IsRequired();
            entity.Property(e => e.DatabaseName).HasColumnName("database_name").IsRequired();
            entity.Property(e => e.InfoLeak).HasColumnName("info_leak").IsRequired(false);
            entity.Property(e => e.RecordCount).HasColumnName("record_count").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasOne(e => e.Search)
                  .WithMany(s => s.Databases)
                  .HasForeignKey(e => e.SearchId)
                  .HasConstraintName("fk_search")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Record
        modelBuilder.Entity<Record>(entity =>
        {
            entity.ToTable("records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DatabaseId).HasColumnName("database_id").IsRequired();
            entity.Property(e => e.RawData).HasColumnName("raw_data").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasOne(e => e.Database)
                  .WithMany(d => d.Records)
                  .HasForeignKey(e => e.DatabaseId)
                  .HasConstraintName("fk_database")
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}