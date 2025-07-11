//recursive solution of the knapsack problem (should timeout because of O(n^2))
class TimeoutExample
{

    static void Main()
    {
        int[] weights = new int[100];
        int[] values = new int[100];

        for (int i = 0; i < 100; i++)
        {
            weights[i] = 1 + (i % 5);
            values[i] = 10 + (i % 10);
        }

        int capacity = 50;
        int maxValue = Knapsack(weights, values, weights.Length, capacity);
        Console.WriteLine("Максимальная ценность: " + maxValue);
    }

    static int Knapsack(int[] weights, int[] values, int n, int capacity)
    {
        if (n == 0 || capacity == 0)
            return 0;

        if (weights[n - 1] > capacity)
            return Knapsack(weights, values, n - 1, capacity);

        int include = values[n - 1] + Knapsack(weights, values, n - 1, capacity - weights[n - 1]);
        int exclude = Knapsack(weights, values, n - 1, capacity);
        return Math.Max(include, exclude);
    }
}


