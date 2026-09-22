using System.Diagnostics;
using System.Linq;
using AlgorithmAnalysis.Models;
using AlgorithmAnalysis.Models.Algorithms;

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

    public async Task<BenchmarkResult> RunBenchmarkAsync(
        AbstractAlgorithm algorithm,
        int[] sizes,
        Action<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new BenchmarkResult
        {
            AlgorithmName = algorithm.Name,
            ComplexityLabel = algorithm.TheoreticalComplexityLabel,
            IsStepBased = algorithm.MeasureSteps
        };

        if (sizes.Length == 0) return result;

        int maxN = sizes.Max();
        var random = new Random(RandomSeed);

        algorithm.CancellationToken = cancellationToken;
        BenchmarkResult benchmarkResult;
        try
        {
            benchmarkResult = await Task.Run(() =>
            {
                // 1. Генерируем мастер-данные максимального размера (один раз)
                algorithm.GenerateMasterData(maxN, random);
                cancellationToken.ThrowIfCancellationRequested();

                // 2. Прогрев JIT (Warmup), чтобы первый замер не включал JIT-компиляцию
                cancellationToken.ThrowIfCancellationRequested();
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
                    long stepCount = 0;

                    for (int run = 0; run < RunsPerSize; run++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Подготовка данных (копирование среза) — строго до запуска секундомера
                        algorithm.PrepareData(n);

                        // Точный замер чистого выполнения алгоритма без Task.Run оверхеда
                        stopwatch.Restart();
                        algorithm.Execute();
                        stopwatch.Stop();

                        runTimes[run] = stopwatch.Elapsed.TotalMilliseconds;

                        if (algorithm is PowerAlgorithm powerAlgo)
                        {
                            stepCount = powerAlgo.LastStepCount;
                        }
                    }

                    // Среднее время по 5 запускам (согласно заданию лабы)
                    double avgTime = runTimes.Average();

                    result.Results.Add(new ExperimentResult
                    {
                        N = n,
                        AverageTimeMs = avgTime,
                        StepCount = stepCount,
                        AllRunsMs = runTimes
                    });

                    // Отчёт о прогрессе
                    progress?.Invoke((double)(i + 1) / totalExperiments);
                }

                // 4. Аппроксимация МНК + MSE
                FitTheoreticalCurve(algorithm, result);

                return result;
            }, cancellationToken);
        }
        finally
        {
            algorithm.CancellationToken = CancellationToken.None;
        }

        // 5. Сохраняем результаты в БД (fire-and-forget, не блокирует UI)
        _ = Task.Run(async () =>
        {
            try
            {
                var db = new DatabaseService();
                await db.SaveResultAsync(benchmarkResult, RandomSeed);
            }
            catch
            {
                // Ошибка сохранения в БД не должна ломать работу приложения
            }
        }, CancellationToken.None);

        return benchmarkResult;
    }

    /// <summary>
    /// Подбирает коэффициент c для теоретической кривой T = c·f(n)
    /// методом наименьших квадратов и вычисляет MSE.
    /// </summary>
    private static void FitTheoreticalCurve(AbstractAlgorithm algorithm, BenchmarkResult result)
    {
        double numerator = 0;
        double denominator = 0;

        foreach (var r in result.Results)
        {
            double fn = algorithm.TheoreticalComplexity(r.N);
            double actualVal = result.IsStepBased ? r.StepCount : r.AverageTimeMs;
            numerator += actualVal * fn;
            denominator += fn * fn;
        }

        double c = denominator > 0 ? numerator / denominator : 0;
        result.FittedCoefficient = c;

        // Заполняем теоретическое значение и считаем MSE
        double sumSquaredErrors = 0;
        foreach (var r in result.Results)
        {
            r.TheoreticalTimeMs = c * algorithm.TheoreticalComplexity(r.N);

            double actualVal = result.IsStepBased ? r.StepCount : r.AverageTimeMs;
            double error = actualVal - r.TheoreticalTimeMs;
            sumSquaredErrors += error * error;
        }

        result.MSE = result.Results.Count > 0
            ? sumSquaredErrors / result.Results.Count
            : 0;
    }
}