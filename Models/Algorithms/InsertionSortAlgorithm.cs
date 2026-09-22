namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание III: Сортировка вставками (Insertion Sort).
/// Теоретическая сложность: O(n²)
/// </summary>
public class InsertionSortAlgorithm : VectorAlgorithm
{
    public override string Name => "Сортировка вставками (Insertion Sort)";
    public override string TheoreticalComplexityLabel => "O(n²)";
    public override string Group => "Сортировки";

    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => (double)n * n;

    public override void Execute()
    {
        int n = _workingData.Length;

        for (int i = 1; i < n; i++)
        {
            if ((i & 0xFF) == 0) ThrowIfCancellationRequested();
            int key = _workingData[i];
            int j = i - 1;

            while (j >= 0 && _workingData[j] > key)
            {
                _workingData[j + 1] = _workingData[j];
                j--;
            }

            _workingData[j + 1] = key;
        }
    }
}
