namespace AlgorithmAnalysis.Models;

/// <summary>
/// Результат одного эксперимента для конкретного размера данных.
/// </summary>
public class ExperimentResult
{
    /// <summary>Размер данных (n)</summary>
    public int N { get; set; }

    /// <summary>Среднее время выполнения в миллисекундах (среднее из нескольких запусков)</summary>
    public double AverageTimeMs { get; set; }

    /// <summary>Теоретическое время (аппроксимация), мс</summary>
    public double TheoreticalTimeMs { get; set; }

    /// <summary>Все замеры для данного n (мс), для анализа разброса</summary>
    public double[] AllRunsMs { get; set; } = [];
}
