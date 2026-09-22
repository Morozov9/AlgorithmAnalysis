namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.7: Гибридная сортировка Timsort.
/// Сочетает сортировку вставками для небольших блоков (run) и слияние (merge).
/// Теоретическая сложность: O(n log n)
/// </summary>
public class TimSortAlgorithm : VectorAlgorithm
{
    private const int MinRun = 32;

    public override string Name => "Гибридная сортировка (Timsort)";
    public override string TheoreticalComplexityLabel => "O(n log n)";
    public override string Group => "Сортировки";

    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => n * Math.Log2(n > 0 ? n : 1);

    public override void Execute()
    {
        int n = _workingData.Length;
        if (n <= 1) return;

        // 1. Сортируем отдельные подмассивы размером MinRun сортировкой вставками
        for (int i = 0; i < n; i += MinRun)
        {
            ThrowIfCancellationRequested();
            InsertionSort(_workingData, i, Math.Min(i + MinRun - 1, n - 1));
        }

        // 2. Итеративно сливаем отсортированные блоки, начиная с MinRun
        for (int size = MinRun; size < n; size *= 2)
        {
            ThrowIfCancellationRequested();
            for (int left = 0; left < n; left += 2 * size)
            {
                int mid = left + size - 1;
                int right = Math.Min(left + 2 * size - 1, n - 1);

                if (mid < right)
                {
                    Merge(_workingData, left, mid, right);
                }
            }
        }
    }

    private static void InsertionSort(int[] arr, int left, int right)
    {
        for (int i = left + 1; i <= right; i++)
        {
            int temp = arr[i];
            int j = i - 1;
            while (j >= left && arr[j] > temp)
            {
                arr[j + 1] = arr[j];
                j--;
            }
            arr[j + 1] = temp;
        }
    }

    private static void Merge(int[] arr, int left, int mid, int right)
    {
        int len1 = mid - left + 1;
        int len2 = right - mid;

        int[] leftArr = new int[len1];
        int[] rightArr = new int[len2];

        Array.Copy(arr, left, leftArr, 0, len1);
        Array.Copy(arr, mid + 1, rightArr, 0, len2);

        int i = 0, j = 0, k = left;

        while (i < len1 && j < len2)
        {
            if (leftArr[i] <= rightArr[j])
            {
                arr[k++] = leftArr[i++];
            }
            else
            {
                arr[k++] = rightArr[j++];
            }
        }

        while (i < len1) arr[k++] = leftArr[i++];
        while (j < len2) arr[k++] = rightArr[j++];
    }
}
