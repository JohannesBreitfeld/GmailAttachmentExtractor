using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SQLite;

public class SQLiteContext : DbContext
{

    public DbSet<ImageAttachment> ImageAttachments { get; set; }
    public DbSet<ExtractionDate> ExtractionDates { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var relativePath = Path.Combine("..", "..", "..", "..", "ImageData.db");
        var absolutePath = Path.GetFullPath(relativePath);

        var connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = absolutePath,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        optionsBuilder.UseSqlite(connectionString);
    }
}
