namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.4a: Вычисление полинома наивным методом (прямое вычисление).
/// P(x) = Σ(k=1..n) vk * x^(k-1), x = 1.5
/// Теоретическая сложность: O(n²) — из-за вычисления x^k на каждом шаге
/// </summary>
public class PolynomialNaive : VectorAlgorithm
{
    private const double X = 1.5;

    public override string Name => "Полином (наивный метод), x=1.5";
    public override string TheoreticalComplexityLabel => "O(n²)";

    public override double TheoreticalComplexity(int n) => (double)n * n;

    public override void Execute()
    {
        // TODO: Реализовать алгоритм
        // P(x) = v1*x^0 + v2*x^1 + v3*x^2 + ... + vn*x^(n-1)
        // Наивно: для каждого члена вычислять x^k заново (без Math.Pow!)
        throw new NotImplementedException("Реализуйте наивное вычисление полинома");
    }
}
