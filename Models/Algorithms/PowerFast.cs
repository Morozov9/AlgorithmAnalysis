namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.8: Быстрое возведение в степень (бинарное).
/// Теоретическая сложность: O(log n)
/// (n — показатель степени)
/// </summary>
public class PowerFast : VectorAlgorithm
{
    public override string Name => "Возведение в степень (быстрое)";
    public override string TheoreticalComplexityLabel => "O(log n)";
    public override string Group => "Возведение в степень";

    public override double TheoreticalComplexity(int n) => Math.Log2(n > 0 ? n : 1);

    public override void Execute()
    {
        // TODO: Реализовать быстрое возведение в степень
        // base^n через двоичное разложение показателя (бинарное возведение)
        throw new NotImplementedException("Реализуйте быстрое возведение в степень");
    }
}
