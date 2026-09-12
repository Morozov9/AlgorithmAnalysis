namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Базовый класс для алгоритмов, работающих с вектором (одномерным массивом).
/// Реализует паттерн "большой массив + срез":
///   - GenerateMasterData: создаёт один большой массив
///   - PrepareData: берёт первые n элементов (копируя, если алгоритм деструктивный)
/// </summary>
public abstract class VectorAlgorithm : AbstractAlgorithm
{
    /// <summary>Мастер-массив максимального размера</summary>
    protected int[] _masterData = [];

    /// <summary>Рабочий массив (срез для текущего запуска)</summary>
    protected int[] _workingData = [];

    /// <summary>
    /// Если true, PrepareData создаёт копию среза (для сортировок и др. деструктивных алгоритмов).
    /// Если false, использует Span/копию — зависит от реализации.
    /// </summary>
    protected virtual bool IsDestructive => false;

    public override string Group => "Функции";

    public override void GenerateMasterData(int maxN, Random random)
    {
        _masterData = new int[maxN];
        for (int i = 0; i < maxN; i++)
        {
            _masterData[i] = random.Next(0, 100_000); // Неотрицательные элементы
        }
    }

    public override void PrepareData(int n)
    {
        if (IsDestructive)
        {
            // Копируем срез, чтобы не испортить мастер-данные
            _workingData = new int[n];
            Array.Copy(_masterData, _workingData, n);
        }
        else
        {
            // Для недеструктивных алгоритмов можно просто взять срез
            // (используем копию для единообразия, но без лишних аллокаций
            // можно было бы использовать Span)
            _workingData = new int[n];
            Array.Copy(_masterData, _workingData, n);
        }
    }
}
