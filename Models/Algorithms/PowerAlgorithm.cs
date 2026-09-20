namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Базовый класс для алгоритмов возведения в степень x^n (Задание I.8 и Часть IV).
/// Не требует больших массивов — работает с числом n и основанием x.
///
/// Основной измеряемый параметр по Части IV — количество шагов (умножений или рекурсивных вызовов).
/// </summary>
public abstract class PowerAlgorithm : AbstractAlgorithm
{
    /// <summary>Основание степени x (по умолчанию 1.5)</summary>
    protected const double BaseX = 1.5;

    /// <summary>Текущий показатель степени n</summary>
    protected int _n;

    /// <summary>Результат вычисления</summary>
    protected double _result;

    /// <summary>Количество шагов (умножений / вызовов) последнего запуска</summary>
    public long LastStepCount { get; protected set; }

    public override string Group => "Возведение в степень";

    public override void GenerateMasterData(int maxN, Random random)
    {
        // Для возведения в степень массивы не нужны
    }

    public override void PrepareData(int n)
    {
        _n = n;
        LastStepCount = 0;
    }
}