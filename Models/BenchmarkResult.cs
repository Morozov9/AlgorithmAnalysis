namespace AlgorithmAnalysis.Models;

/// <summary>
/// Полный результат бенчмарка для одного алгоритма.
/// Содержит все замеры + метаданные.
/// </summary>
public class BenchmarkResult
{
    /// <summary>Название алгоритма</summary>
    public string AlgorithmName { get; set; } = string.Empty;

    /// <summary>Подпись теоретической сложности (напр. "O(n²)")</summary>
    public string ComplexityLabel { get; set; } = string.Empty;

    /// <summary>Результаты экспериментов для каждого размера n</summary>
    public List<ExperimentResult> Results { get; set; } = [];

    /// <summary>Подобранный коэффициент c для теоретической кривой T = c·f(n)</summary>
    public double FittedCoefficient { get; set; }

    /// <summary>
    /// Относительная среднеквадратичная ошибка аппроксимации в процентах:
    /// 100 · √(Σ(T_эксп − T_теор)² / ΣT_эксп²).
    /// </summary>
    public double ErrorPercent
    {
        get
        {
            double squaredErrors = 0;
            double squaredActuals = 0;
            foreach (var result in Results)
            {
                double actual = IsStepBased ? result.StepCount : result.AverageTimeMs;
                double error = actual - result.TheoreticalTimeMs;
                squaredErrors += error * error;
                squaredActuals += actual * actual;
            }

            return squaredActuals > 0
                ? 100 * Math.Sqrt(squaredErrors / squaredActuals)
                : squaredErrors == 0 ? 0 : double.NaN;
        }
    }

    /// <summary>Показывает, измеряется ли число операций/шагов (true) или машинное время (false)</summary>
    public bool IsStepBased { get; set; }

    /// <summary>Единица измерения (шагов / мс)</summary>
    public string UnitLabel => IsStepBased ? "шагов" : "мс";

    /// <summary>Подпись оси Y на графике</summary>
    public string YAxisTitle => IsStepBased ? "Число операций (шагов)" : "Время выполнения (мс)";

    /// <summary>Подпись оси X на графике</summary>
    public string XAxisTitle => IsStepBased ? "Показатель степени n" : "Размерность входных данных n";
}
