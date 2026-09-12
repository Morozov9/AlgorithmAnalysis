namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.7: Гибридная сортировка Timsort.
/// Теоретическая сложность: O(n log n)
/// </summary>
public class TimSortAlgorithm : VectorAlgorithm
{
    public override string Name => "Гибридная сортировка (Timsort)";
    public override string TheoreticalComplexityLabel => "O(n log n)";
    public override string Group => "Сортировки";

    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => n * Math.Log2(n > 0 ? n : 1);

    public override void Execute()
    {
        // TODO: Реализовать алгоритм Timsort
        // Можно использовать Array.Sort() (который в .NET — Timsort/Introsort)
        // или написать свою реализацию
        // Работать с массивом _workingData
        throw new NotImplementedException("Реализуйте Timsort");
    }
}
