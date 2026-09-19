using AlgorithmAnalysis.Data;
using AlgorithmAnalysis.Data.Entities;
using AlgorithmAnalysis.Models;
using Microsoft.EntityFrameworkCore;

namespace AlgorithmAnalysis.Services;

/// <summary>
/// Сервис для работы с базой данных.
/// Сохраняет результаты замеров и загружает их обратно.
/// Используется BenchmarkService для персистентности результатов.
/// </summary>
public class DatabaseService
{
    /// <summary>
    /// Создаёт (или обновляет) схему БД при первом запуске.
    /// Вызывается один раз при старте приложения.
    /// </summary>
    public static async Task InitializeAsync()
    {
        await using var db = new AppDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Сохраняет все замеры из BenchmarkResult в БД.
    /// Каждый отдельный прогон (run) сохраняется отдельной строкой.
    /// </summary>
    /// <param name="benchmark">Результат бенчмарка</param>
    /// <param name="seed">Seed генератора случайных чисел (для воспроизводимости)</param>
    public async Task SaveResultAsync(BenchmarkResult benchmark, int seed = 42)
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
                    AlgorithmName  = benchmark.AlgorithmName,
                    N              = result.N,
                    M              = null,          // зарезервировано для матричных алгоритмов (шаг 3)
                    RunIndex       = runIndex + 1,
                    ElapsedMs      = result.AllRunsMs[runIndex],
                    StepCount      = null,           // зарезервировано для счётчика шагов (шаг 5)
                    ExperimentDate = now,
                    RandomSeed     = seed
                });
            }
        }

        await db.BenchmarkRuns.AddRangeAsync(entities);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Загружает все записи из БД для заданного алгоритма.
    /// Возвращает сырые строки из таблицы — для статистики или истории.
    /// </summary>
    /// <param name="algorithmName">Название алгоритма</param>
    public async Task<List<BenchmarkRunEntity>> GetRunsAsync(string algorithmName)
    {
        await using var db = new AppDbContext();
        return await db.BenchmarkRuns
                       .Where(r => r.AlgorithmName == algorithmName)
                       .OrderBy(r => r.N)
                       .ThenBy(r => r.RunIndex)
                       .ToListAsync();
    }

    /// <summary>
    /// Проверяет, есть ли в БД данные для заданного алгоритма и набора размеров.
    /// Используется для кэширования (шаг 2): если все n уже замерены — повторный запуск не нужен.
    /// </summary>
    /// <param name="algorithmName">Название алгоритма</param>
    /// <param name="sizes">Массив размеров входных данных</param>
    /// <returns>true — данные в кэше есть для всех n; false — нужно запустить бенчмарк</returns>
    public async Task<bool> HasCachedDataAsync(string algorithmName, int[] sizes)
    {
        if (sizes.Length == 0) return false;

        await using var db = new AppDbContext();

        // Считаем сколько уникальных n есть в БД для этого алгоритма
        var cachedNs = await db.BenchmarkRuns
                               .Where(r => r.AlgorithmName == algorithmName && sizes.Contains(r.N))
                               .Select(r => r.N)
                               .Distinct()
                               .ToListAsync();

        // Данные считаются кэшированными, если все запрошенные n присутствуют в БД
        return sizes.All(n => cachedNs.Contains(n));
    }

    /// <summary>
    /// Загружает закэшированный BenchmarkResult из БД для заданного алгоритма и размеров.
    /// Усредняет все имеющиеся прогоны для каждого n точно так же, как BenchmarkService.
    /// </summary>
    /// <param name="algorithmName">Название алгоритма</param>
    /// <param name="complexityLabel">Метка сложности (напр. "O(n²)")</param>
    /// <param name="sizes">Массив размеров</param>
    /// <param name="theoreticalFunc">Теоретическая функция f(n) для пересчёта аппроксимации</param>
    public async Task<BenchmarkResult?> LoadCachedResultAsync(
        string algorithmName,
        string complexityLabel,
        int[] sizes,
        Func<int, double> theoreticalFunc)
    {
        await using var db = new AppDbContext();

        var rows = await db.BenchmarkRuns
                           .Where(r => r.AlgorithmName == algorithmName && sizes.Contains(r.N))
                           .OrderBy(r => r.N)
                           .ThenBy(r => r.RunIndex)
                           .ToListAsync();

        if (rows.Count == 0) return null;

        // Группируем по n и усредняем замеры
        var grouped = rows
            .GroupBy(r => r.N)
            .OrderBy(g => g.Key)
            .Select(g => new ExperimentResult
            {
                N            = g.Key,
                AverageTimeMs = g.Average(r => r.ElapsedMs),
                AllRunsMs    = g.Select(r => r.ElapsedMs).ToArray()
            })
            .ToList();

        var result = new BenchmarkResult
        {
            AlgorithmName  = algorithmName,
            ComplexityLabel = complexityLabel,
            Results        = grouped
        };

        // Пересчитываем аппроксимацию МНК на основе загруженных данных
        double numerator = 0, denominator = 0;
        foreach (var r in result.Results)
        {
            double fn = theoreticalFunc(r.N);
            numerator   += r.AverageTimeMs * fn;
            denominator += fn * fn;
        }
        double c = denominator > 0 ? numerator / denominator : 0;
        result.FittedCoefficient = c;
        foreach (var r in result.Results)
            r.TheoreticalTimeMs = c * theoreticalFunc(r.N);

        return result;
    }

    /// <summary>
    /// Удаляет все записи для заданного алгоритма из БД.
    /// Используется при принудительном пересчёте (шаг 2).
    /// </summary>
    public async Task DeleteRunsAsync(string algorithmName)
    {
        await using var db = new AppDbContext();
        var toDelete = db.BenchmarkRuns.Where(r => r.AlgorithmName == algorithmName);
        db.BenchmarkRuns.RemoveRange(toDelete);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Возвращает статистику по всем сохранённым алгоритмам:
    /// сколько точек n сохранено и когда последний эксперимент.
    /// </summary>
    public async Task<List<(string AlgoName, int UniqueNCount, DateTime LastRun)>> GetSummaryAsync()
    {
        await using var db = new AppDbContext();
        return await db.BenchmarkRuns
                       .GroupBy(r => r.AlgorithmName)
                       .Select(g => new
                       {
                           AlgoName     = g.Key,
                           UniqueNCount = g.Select(r => r.N).Distinct().Count(),
                           LastRun      = g.Max(r => r.ExperimentDate)
                       })
                       .OrderBy(x => x.AlgoName)
                       .Select(x => ValueTuple.Create(x.AlgoName, x.UniqueNCount, x.LastRun))
                       .ToListAsync();
    }
}
