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
        // TODO: Реализовать алгоритм
        // P(x) = v1 + x*(v2 + x*(v3 + ...))
        // Схема Горнера: начинаем с vn, идём справа налево
        throw new NotImplementedException("Реализуйте вычисление полинома методом Горнера");
    }
}
