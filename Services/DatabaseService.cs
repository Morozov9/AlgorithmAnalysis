using AlgorithmAnalysis.Data;
using AlgorithmAnalysis.Data.Entities;
using AlgorithmAnalysis.Models;
using Microsoft.EntityFrameworkCore;

namespace AlgorithmAnalysis.Services;

/// <summary>
/// Сервис для работы с базой данных.
/// Все методы обёрнуты в try-catch, чтобы ошибки БД не ломали работу приложения.
/// </summary>
public class DatabaseService
{
    /// <summary>
    /// Создаёт (или обновляет) схему БД при первом запуске.
    /// </summary>
    public static async Task InitializeAsync()
    {
        try
        {
            await using var db = new AppDbContext();
            await db.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Ошибка инициализации: {ex.Message}");
        }
    }

    /// <summary>
    /// Сохраняет замеры в БД. Каждый запуск — отдельная строка.
    /// </summary>
    public async Task SaveResultAsync(BenchmarkResult benchmark, int seed = 42)
    {
        try
        {
            await using var db = new AppDbContext();

            var now = DateTime.UtcNow;
            var entities = new List<BenchmarkRunEntity>();

            foreach (var result in benchmark.Results)
            {
                for (int runIndex = 0; runIndex < result.AllRunsMs.Length; runIndex++)
                {
                    entities.Add(new BenchmarkRunEntity
                    {
                        AlgorithmName = benchmark.AlgorithmName,
                        N = result.N,
                        M = null,
                        RunIndex = runIndex + 1,
                        ElapsedMs = result.AllRunsMs[runIndex],
                        StepCount = null,
                        ExperimentDate = now,
                        RandomSeed = seed
                    });
                }
            }

            await db.BenchmarkRuns.AddRangeAsync(entities);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Ошибка сохранения: {ex.Message}");
        }
    }

    /// <summary>
    /// Загружает все записи для заданного алгоритма.
    /// </summary>
    public async Task<List<BenchmarkRunEntity>> GetRunsAsync(string algorithmName)
    {
        try
        {
            await using var db = new AppDbContext();
            return await db.BenchmarkRuns
                           .Where(r => r.AlgorithmName == algorithmName)
                           .OrderBy(r => r.N)
                           .ThenBy(r => r.RunIndex)
                           .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Ошибка чтения: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Проверяет, есть ли в БД данные для заданного алгоритма и набора размеров.
    /// </summary>
    public async Task<bool> HasCachedDataAsync(string algorithmName, int[] sizes)
    {
        if (sizes.Length == 0) return false;

        try
        {
            await using var db = new AppDbContext();
            var sizesList = sizes.ToList();

            var cachedNs = await db.BenchmarkRuns
                                   .Where(r => r.AlgorithmName == algorithmName && sizesList.Contains(r.N))
                                   .Select(r => r.N)
                                   .Distinct()
                                   .ToListAsync();

            return sizes.All(n => cachedNs.Contains(n));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Ошибка проверки кэша: {ex.Message}");
            return false;   // при ошибке — считаем, что кэша нет
        }
    }

    /// <summary>
    /// Загружает закэшированный BenchmarkResult из БД.
    /// </summary>
    public async Task<BenchmarkResult?> LoadCachedResultAsync(
        string algorithmName,
        string complexityLabel,
        int[] sizes,
        Func<int, double> theoreticalFunc)
    {
        try
        {
            await using var db = new AppDbContext();
            var sizesList = sizes.ToList();

            var rows = await db.BenchmarkRuns
                               .Where(r => r.AlgorithmName == algorithmName && sizesList.Contains(r.N))
                               .OrderBy(r => r.N)
                               .ThenBy(r => r.RunIndex)
                               .ToListAsync();

            if (rows.Count == 0) return null;

            var grouped = rows
                .GroupBy(r => r.N)
                .OrderBy(g => g.Key)
                .Select(g => new ExperimentResult
                {
                    N = g.Key,
                    AverageTimeMs = g.Average(r => r.ElapsedMs),
                    AllRunsMs = g.Select(r => r.ElapsedMs).ToArray()
                })
                .ToList();

            var result = new BenchmarkResult
            {
                AlgorithmName = algorithmName,
                ComplexityLabel = complexityLabel,
                Results = grouped
            };

            // Пересчёт МНК и MSE
            double numerator = 0, denominator = 0;
            foreach (var r in result.Results)
            {
                double fn = theoreticalFunc(r.N);
                numerator += r.AverageTimeMs * fn;
                denominator += fn * fn;
            }
            double c = denominator > 0 ? numerator / denominator : 0;
            result.FittedCoefficient = c;

            double sumSquaredErrors = 0;
            foreach (var r in result.Results)
            {
                r.TheoreticalTimeMs = c * theoreticalFunc(r.N);
                double error = r.AverageTimeMs - r.TheoreticalTimeMs;
                sumSquaredErrors += error * error;
            }
            result.MSE = result.Results.Count > 0 ? sumSquaredErrors / result.Results.Count : 0;

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Ошибка загрузки кэша: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Удаляет все записи для заданного алгоритма.
    /// </summary>
    public async Task DeleteRunsAsync(string algorithmName)
    {
        try
        {
            await using var db = new AppDbContext();
            var toDelete = db.BenchmarkRuns.Where(r => r.AlgorithmName == algorithmName);
            db.BenchmarkRuns.RemoveRange(toDelete);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Ошибка удаления: {ex.Message}");
        }
    }

    /// <summary>
    /// Сводка по всем сохранённым алгоритмам.
    /// </summary>
    public async Task<List<(string AlgoName, int UniqueNCount, DateTime LastRun)>> GetSummaryAsync()
    {
        try
        {
            await using var db = new AppDbContext();
            return await db.BenchmarkRuns
                           .GroupBy(r => r.AlgorithmName)
                           .Select(g => new
                           {
                               AlgoName = g.Key,
                               UniqueNCount = g.Select(r => r.N).Distinct().Count(),
                               LastRun = g.Max(r => r.ExperimentDate)
                           })
                           .OrderBy(x => x.AlgoName)
                           .Select(x => ValueTuple.Create(x.AlgoName, x.UniqueNCount, x.LastRun))
                           .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Ошибка сводки: {ex.Message}");
            return [];
        }
    }
}