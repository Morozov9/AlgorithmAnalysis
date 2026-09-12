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
        int sum = 0;
        for (int i = 0; i < _workingData.Length; i++)
        {
            sum += _workingData[i];
        }
    }
}
