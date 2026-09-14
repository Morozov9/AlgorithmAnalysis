namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Базовый класс для алгоритмов возведения в степень x^n (Задание I.8).
/// Не требует выделения больших массивов памяти под n элементов,
/// сохраняет показатель степени n и основание x.
/// </summary>
public abstract class PowerAlgorithm : AbstractAlgorithm
{
    /// <summary>Основание степени x (по умолчанию 1.5)</summary>
    protected const double BaseX = 1.5;

    /// <summary>Текущий показатель степени n</summary>
    protected int _n;

    /// <summary>Результат вычисления</summary>
    protected double _result;

    public override string Group => "Возведение в степень";

    public override void GenerateMasterData(int maxN, Random random)
    {
        // Для математических алгоритмов возведения в степень
        // генерация больших массивов не требуется
    }

    public override void PrepareData(int n)
    {
        _n = n;
    }
}
