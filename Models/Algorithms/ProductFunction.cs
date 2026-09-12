namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.3: f(v) = Πvk (произведение элементов).
/// Теоретическая сложность: O(n)
/// </summary>
public class ProductFunction : VectorAlgorithm
{
    public override string Name => "f(v) = Πvk (произведение элементов)";
    public override string TheoreticalComplexityLabel => "O(n)";

    public override double TheoreticalComplexity(int n) => n;

    public override void Execute()
    {
        // TODO: Реализовать алгоритм
        // Вычислить произведение всех элементов _workingData
        throw new NotImplementedException("Реализуйте алгоритм произведения элементов");
    }
}
