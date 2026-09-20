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
    /// Среднеквадратичная ошибка аппроксимации (MSE):
    /// MSE = (1/k) · Σ (T_эксп(n_i) − T_теор(n_i))²
    /// Чем меньше — тем лучше теория описывает эксперимент.
    /// </summary>
    public double MSE { get; set; }

    /// <summary>Показывает, измеряется ли число операций/шагов (true) или машинное время (false)</summary>
    public bool IsStepBased { get; set; }

    /// <summary>Единица измерения (шагов / мс)</summary>
    public string UnitLabel => IsStepBased ? "шагов" : "мс";

    /// <summary>Подпись оси Y на графике</summary>
    public string YAxisTitle => IsStepBased ? "Число операций (шагов)" : "Время выполнения (мс)";

    /// <summary>Подпись оси X на графике</summary>
    public string XAxisTitle => IsStepBased ? "Показатель степени n" : "Размерность входных данных n";
}