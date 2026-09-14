namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.4a: Вычисление полинома наивным методом (прямое вычисление).
/// P(x) = Σ(k=1..n) vk * x^(k-1), x = 1.5
/// Теоретическая сложность: O(n²) — из-за вычисления степени x^k на каждом шаге
/// </summary>
public class PolynomialNaive : VectorAlgorithm
{
    private const double X = 1.5;
    private double _result;

    public override string Name => "Полином (наивный метод), x=1.5";
    public override string TheoreticalComplexityLabel => "O(n²)";

    public override double TheoreticalComplexity(int n) => (double)n * n;

    public override void Execute()
    {
        double result = 0;

        for (int k = 0; k < _workingData.Length; k++)
        {
            // Считаем x^k вложенным циклом
            double power = 1.0;
            for (int j = 0; j < k; j++)
            {
                power *= X;
            }

            // Прибавляем v[k] · x^k
            result += _workingData[k] * power;
        }

        _result = result;
    }
}
