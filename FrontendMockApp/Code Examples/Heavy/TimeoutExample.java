class TimeOut{
	public static class Solution {
        public double fractionalKnapsack(Item[] items, int capacity) {
            Arrays.sort(items, Comparator.comparingDouble(Item::getRatio).reversed());
            return fractionalKnapsackRecursive(items, capacity, 0);
        }

        private double fractionalKnapsackRecursive(Item[] items, int capacity, int index) {
            if (capacity == 0 || index == items.length)
                return 0.0;

            double takeFull = 0;
            if (items[index].weight <= capacity) {
                takeFull = items[index].value + fractionalKnapsackRecursive(items, capacity - items[index].weight, index + 1);
            }

            double takePartial = items[index].getRatio() * Math.min(capacity, items[index].weight);

            double skip = fractionalKnapsackRecursive(items, capacity, index + 1);

            return Math.max(Math.max(takeFull, takePartial), skip);
        }
	}
}


