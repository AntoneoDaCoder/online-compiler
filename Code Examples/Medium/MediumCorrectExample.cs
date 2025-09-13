//quick sort example
class MediumCorrectExample
{
    static void Main()
    {
        int[] data = { 5, 2, 9, 1, 5, 6 };
        QuickSort(data, 0, data.Length - 1);
        Console.WriteLine("Sorted:");
        foreach (int val in data)
            Console.WriteLine(val);
    }

    static void QuickSort(int[] arr, int left, int right)
    {
        if (left >= right)
            return;

        int pivot = arr[(left + right) / 2];
        int index = Partition(arr, left, right, pivot);
        QuickSort(arr, left, index - 1);
        QuickSort(arr, index, right);
    }

    static int Partition(int[] arr, int left, int right, int pivot)
    {
        while (left <= right)
        {
            while (arr[left] < pivot) left++;
            while (arr[right] > pivot) right--;

            if (left <= right)
            {
                int tmp = arr[left];
                arr[left] = arr[right];
                arr[right] = tmp;
                left++;
                right--;
            }
        }
        return left;
    }
}
