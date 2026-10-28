namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Шейкерная сортировка (Cocktail Shaker Sort).
/// Теоретическая сложность: O(n²), для отсортированного массива — O(n).
/// </summary>
public class CocktailShakerSortAlgorithm : VectorAlgorithm
{
    public override string Name => "Шейкерная сортировка (Cocktail Shaker Sort)";
    public override string TheoreticalComplexityLabel => "O(n²)";
    public override string Group => "Сортировки";

    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => (double)n * n;

    public override void Execute()
    {
        int left = 0;
        int right = _workingData.Length - 1;

        ThrowIfCancellationRequested();

        while (left < right)
        {
            ThrowIfCancellationRequested();
            bool swapped = false;

            // За прямой проход наибольший элемент перемещается вправо.
            for (int i = left; i < right; i++)
            {
                if ((i & 0x3FF) == 0) ThrowIfCancellationRequested();

                if (_workingData[i] > _workingData[i + 1])
                {
                    int temp = _workingData[i];
                    _workingData[i] = _workingData[i + 1];
                    _workingData[i + 1] = temp;
                    swapped = true;
                }
            }

            right--;
            if (!swapped) break;

            swapped = false;

            // За обратный проход наименьший элемент перемещается влево.
            for (int i = right; i > left; i--)
            {
                if ((i & 0x3FF) == 0) ThrowIfCancellationRequested();

                if (_workingData[i - 1] > _workingData[i])
                {
                    int temp = _workingData[i - 1];
                    _workingData[i - 1] = _workingData[i];
                    _workingData[i] = temp;
                    swapped = true;
                }
            }

            left++;
            if (!swapped) break;
        }
    }
}
