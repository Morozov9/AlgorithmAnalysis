namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.5: Сортировка пузырьком (Bubble Sort).
/// Теоретическая сложность: O(n²)
/// </summary>
public class BubbleSortAlgorithm : VectorAlgorithm
{
    public override string Name => "Сортировка пузырьком (Bubble Sort)";
    public override string TheoreticalComplexityLabel => "O(n²)";
    public override string Group => "Сортировки";

    /// <summary>Сортировка — деструктивный алгоритм, нужна копия данных</summary>
    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => (double)n * n;

    public override void Execute()
    {
        // TODO: Реализовать алгоритм сортировки пузырьком
        // Работать с массивом _workingData
        throw new NotImplementedException("Реализуйте сортировку пузырьком");
    }
}
