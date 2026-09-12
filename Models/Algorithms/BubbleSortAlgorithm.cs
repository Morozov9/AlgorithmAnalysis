namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.5: Сортировка пузырьком (Bubble Sort).
/// Теоретическая сложность: O(n²)
/// </summary>
public class BubbleSortAlgorithm : VectorAlgorithm
{
    public override string Name => "Сортировка пузырьком (Bubble Sort)";
    public override string TheoreticalComplexityLabel => "O(n²)";
    public override string Group => "Сортировки";

    /// <summary>Сортировка — деструктивный алгоритм, нужна копия данных</summary>
    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => (double)n * n;

    public override void Execute()
    {
        int n = _workingData.Length;

        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - 1 - i; j++)
            {
                if (_workingData[j] > _workingData[j + 1])
                {
                    int temp = _workingData[j];
                    _workingData[j] = _workingData[j + 1];
                    _workingData[j + 1] = temp;
                }
            }
        }
    }
}
