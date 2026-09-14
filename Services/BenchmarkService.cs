using System.Diagnostics;
using AlgorithmAnalysis.Models;

namespace AlgorithmAnalysis.Services;

/// <summary>
/// Движок бенчмарков. Запускает алгоритм на различных размерах данных,
/// замеряет время через Stopwatch, вычисляет среднее.
/// </summary>
public class BenchmarkService
{
    /// <summary>Количество прогонов для каждого размера (для усреднения)</summary>
    public int RunsPerSize { get; set; } = 5;

    /// <summary>Seed для генератора случайных чисел (для воспроизводимости)</summary>
    public int RandomSeed { get; set; } = 42;

    /// <summary>
    /// Запускает серию экспериментов для алгоритма.
    /// Выполняется асинхронно в фоновом потоке, не блокируя интерфейс.
    /// </summary>
    /// <param name="algorithm">Алгоритм для тестирования</param>
    /// <param name="sizes">Массив размеров данных [n1, n2, ..., nk]</param>
    /// <param name="progress">Callback для отчёта о прогрессе (0.0 .. 1.0)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Полный результат бенчмарка</returns>
    public async Task<BenchmarkResult> RunBenchmarkAsync(
        AbstractAlgorithm algorithm,
        int[] sizes,
        Action<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BenchmarkResult
        {
            AlgorithmName = algorithm.Name,
            ComplexityLabel = algorithm.TheoreticalComplexityLabel
        };

        if (sizes.Length == 0) return result;

        int maxN = sizes.Max();
        var random = new Random(RandomSeed);

        return await Task.Run(() =>
        {
            // 1. Генерируем мастер-данные максимального размера (один раз)
            algorithm.GenerateMasterData(maxN, random);
            cancellationToken.ThrowIfCancellationRequested();

            // 2. Прогрев JIT (Warmup), чтобы первый замер не включал JIT-компиляцию
            int warmupN = Math.Min(10, maxN);
            algorithm.PrepareData(warmupN);
            algorithm.Execute();
            algorithm.Execute();
            algorithm.Execute();

            var stopwatch = new Stopwatch();
            int totalExperiments = sizes.Length;

            // 3. Серия замеров для каждого размера n
            for (int i = 0; i < sizes.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int n = sizes[i];
                var runTimes = new double[RunsPerSize];

                for (int run = 0; run < RunsPerSize; run++)
                {
                    // Подготовка данных (копирование среза) — строго до запуска секундомера
                    algorithm.PrepareData(n);

                    // Точный замер чистого выполнения алгоритма без Task.Run оверхеда
                    stopwatch.Restart();
                    algorithm.Execute();
                    stopwatch.Stop();

                    runTimes[run] = stopwatch.Elapsed.TotalMilliseconds;
                }

                // Среднее время по 5 запускам (согласно заданию лабы)
                double avgTime = runTimes.Average();

                result.Results.Add(new ExperimentResult
                {
                    N = n,
                    AverageTimeMs = avgTime,
                    AllRunsMs = runTimes
                });

                // Отчёт о прогрессе
                progress?.Invoke((double)(i + 1) / totalExperiments);
            }

            // 4. Подбираем коэффициент c для теоретической кривой T = c·f(n)
            // Метод наименьших квадратов: c = Σ(Ti · f(ni)) / Σ(f(ni)²)
            FitTheoreticalCurve(algorithm, result);

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// Подбирает коэффициент c для теоретической кривой T = c·f(n)
    /// методом наименьших квадратов.
    /// </summary>
    private static void FitTheoreticalCurve(AbstractAlgorithm algorithm, BenchmarkResult result)
    {
        double numerator = 0;
        double denominator = 0;

        foreach (var r in result.Results)
        {
            double fn = algorithm.TheoreticalComplexity(r.N);
            numerator += r.AverageTimeMs * fn;
            denominator += fn * fn;
        }

        double c = denominator > 0 ? numerator / denominator : 0;
        result.FittedCoefficient = c;

        // Заполняем теоретическое время
        foreach (var r in result.Results)
        {
            r.TheoreticalTimeMs = c * algorithm.TheoreticalComplexity(r.N);
        }
    }
}
