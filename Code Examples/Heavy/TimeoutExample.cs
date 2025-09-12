//recursive solution of the knapsack problem (should timeout because of O(n^2))
class TimeoutExample
{
    public class Solution
    {
        public double FractionalKnapsack(Item[] items, int capacity)
        {            
            var sortedItems = items.OrderByDescending(i => i.Ratio).ToArray();

            return FractionalKnapsackRecursive(sortedItems, capacity, 0);
        }

        // Artificially exponential version (не оптимальный, просто ради нагрузки)
        private double FractionalKnapsackRecursive(Item[] items, int capacity, int index)
        {
            if (capacity == 0 || index == items.Length)
                return 0.0;

            double takeFull = 0;
            if (items[index].Weight <= capacity)
            {
                takeFull = items[index].Value + FractionalKnapsackRecursive(items, capacity - items[index].Weight, index + 1);
            }

            double takePartial = items[index].Ratio * Math.Min(capacity, items[index].Weight);

            double skip = FractionalKnapsackRecursive(items, capacity, index + 1);

            return Math.Max(Math.Max(takeFull, takePartial), skip);
        }

    }
}


