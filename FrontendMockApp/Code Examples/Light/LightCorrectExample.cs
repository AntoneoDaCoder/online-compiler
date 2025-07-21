//find array min value example
class LightCorrectExample
{
    static void Main()
    {
        int[] numbers = { 5, 3, 8, 1, 4 };
        int min = FindMinimum(numbers);
        Console.WriteLine("Array minimum: " + min);
    }

    static int FindMinimum(int[] arr)
    {
        int min = arr[0];
        for (int i = 1; i < arr.Length; i++)
        {
            if (arr[i] < min)
            {
                min = arr[i];
            }
        }
        return min;
    }
}


