//should not timeout (O(n*log n))
class HeavyCorrectExample
{
    class Item
    {
        public int Value;
        public int Weight;
        public double Ratio => (double)Value / Weight;
    }

    static void Main()
    {
        int[] weights = new int[100];
        int[] values = new int[100];

        for (int i = 0; i < 100; i++)
        {
            weights[i] = 1 + (i % 5);
            values[i] = 10 + (i % 10);
        }

        Item[] items = new Item[100];

        for (int i = 0; i < 100; i++)
        {
            items[i] = new Item() { Value = values[i], Weight = weights[i] };
        }

        int capacity = 50;
        double totalValue = FractionalKnapsack(items, capacity);
        Console.WriteLine("Максимальная ценность (жадный): " + totalValue);
    }

    static double FractionalKnapsack(Item[] items, int capacity)
    {
        Array.Sort(items, (a, b) => b.Ratio.CompareTo(a.Ratio));

        double totalValue = 0.0;

        foreach (var item in items)
        {
            if (capacity >= item.Weight)
            {
                capacity -= item.Weight;
                totalValue += item.Value;
            }
            else
            {
                totalValue += item.Ratio * capacity;
                break;
            }
        }

        return totalValue;
    }
}


HeavyCorrectExample.Main();
