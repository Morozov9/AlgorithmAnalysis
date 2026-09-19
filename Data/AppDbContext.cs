using AlgorithmAnalysis.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlgorithmAnalysis.Data;

/// <summary>
/// EF Core контекст приложения.
/// Управляет соединением с SQLite и описывает схему таблиц.
/// Файл БД: algorithmanalysis.db рядом с исполняемым файлом.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Таблица всех запусков алгоритмов</summary>
    public DbSet<BenchmarkRunEntity> BenchmarkRuns => Set<BenchmarkRunEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Путь к файлу БД — рядом с .exe приложения
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "algorithmanalysis.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BenchmarkRunEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AlgorithmName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.ElapsedMs)
                  .IsRequired();

            entity.Property(e => e.ExperimentDate)
                  .IsRequired();

            // Индекс для быстрого поиска по алгоритму и размеру n
            // (используется при проверке кэша в шаге 2)
            entity.HasIndex(e => new { e.AlgorithmName, e.N })
                  .HasDatabaseName("IX_BenchmarkRuns_AlgoName_N");
        });
    }
}
