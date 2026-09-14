namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.3: f(v) = Πvk (произведение элементов).
/// Теоретическая сложность: O(n)
/// </summary>
public class ProductFunction : VectorAlgorithm
{
    private double _product;

    public override string Name => "f(v) = Πvk (произведение элементов)";
    public override string TheoreticalComplexityLabel => "O(n)";

    public override double TheoreticalComplexity(int n) => n;

    public override void Execute()
    {
        double product = 1.0;
        for (int i = 0; i < _workingData.Length; i++)
        {
            product *= _workingData[i];
        }
        _product = product;
    }
}
