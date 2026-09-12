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
}
