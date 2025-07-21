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

        private double FractionalKnapsackRecursive(Item[] items, int capacity, int index)
        {
            if (capacity == 0 || index == items.Length)
                return 0.0;

            var current = items[index];

            if (current.Weight <= capacity)
            {
                return current.Value + FractionalKnapsackRecursive(items, capacity - current.Weight, index + 1);
            }
            else
            {
                return current.Ratio * capacity;
            }
        }
    }
}


