//should not timeout (O(n*log n))
class HeavyCorrectExample
{
    public class Solution
    {
        public double FractionalKnapsack(Item[] items, int capacity)
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

}
