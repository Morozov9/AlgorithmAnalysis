namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.6: Быстрая сортировка (Quick Sort).
/// Теоретическая сложность: O(n log n) — средний случай
/// </summary>
public class QuickSortAlgorithm : VectorAlgorithm
{
    public override string Name => "Быстрая сортировка (Quick Sort)";
    public override string TheoreticalComplexityLabel => "O(n log n)";
    public override string Group => "Сортировки";

    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => n * Math.Log2(n > 0 ? n : 1);

    public override void Execute()
    {
        // TODO: Реализовать алгоритм быстрой сортировки
        // Работать с массивом _workingData
        throw new NotImplementedException("Реализуйте быструю сортировку");
    }
}
