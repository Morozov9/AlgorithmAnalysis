namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.2: f(v) = Σvk (сумма элементов).
/// Теоретическая сложность: O(n)
/// </summary>
public class SumFunction : VectorAlgorithm
{
    public override string Name => "f(v) = Σvk (сумма элементов)";
    public override string TheoreticalComplexityLabel => "O(n)";

    public override double TheoreticalComplexity(int n) => n;

    public override void Execute()
    {
        // TODO: Реализовать алгоритм
        // Вычислить сумму всех элементов _workingData
        throw new NotImplementedException("Реализуйте алгоритм суммы элементов");
    }
}
