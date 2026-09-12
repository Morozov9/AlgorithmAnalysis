namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.1: f(v) = 1 (постоянная функция).
/// Теоретическая сложность: O(1)
/// </summary>
public class ConstantFunction : VectorAlgorithm
{
    public override string Name => "f(v) = 1 (постоянная функция)";
    public override string TheoreticalComplexityLabel => "O(1)";

    public override double TheoreticalComplexity(int n) => 1.0;

    public override void Execute()
    {
        // f(v) = 1 — просто возвращаем 1, не обращаясь к элементам вектора
        int result = 1;
    }
}
