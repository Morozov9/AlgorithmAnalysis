namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.8: Алгоритмы возведения в степень.
/// Здесь можно реализовать наивное возведение и быстрое возведение в степень.
/// Теоретическая сложность наивного: O(n), быстрого: O(log n)
/// (n — показатель степени)
/// </summary>
public class PowerNaive : VectorAlgorithm
{
    public override string Name => "Возведение в степень (наивное)";
    public override string TheoreticalComplexityLabel => "O(n)";
    public override string Group => "Возведение в степень";

    public override double TheoreticalComplexity(int n) => n;

    public override void Execute()
    {
        // TODO: Реализовать наивное возведение в степень
        // base^n путём последовательного умножения
        throw new NotImplementedException("Реализуйте наивное возведение в степень");
    }
}
