namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.4b: Вычисление полинома методом Горнера.
/// P(x) = v1 + x*(v2 + x*(v3 + ... + x*vn)...), x = 1.5
/// Теоретическая сложность: O(n)
/// </summary>
public class PolynomialHorner : VectorAlgorithm
{
    private const double X = 1.5;

    public override string Name => "Полином (метод Горнера), x=1.5";
    public override string TheoreticalComplexityLabel => "O(n)";

    public override double TheoreticalComplexity(int n) => n;

    public override void Execute()
    {
        double x = 1.5;
        double result = 0;

        // Идём справа налево
        for (int i = _workingData.Length - 1; i >= 0; i--)
        {
            result = _workingData[i] + x * result;
        }
    }
}
