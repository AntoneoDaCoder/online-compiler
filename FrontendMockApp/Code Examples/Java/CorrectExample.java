class CorrectExample {
    public static class Solution {
        public double fractionalKnapsack(Item[] items, int capacity) {
            Arrays.sort(items, Comparator.comparingDouble((Item i) -> i.ratio).reversed());

            double totalValue = 0.0;

            for (Item item : items) {
                if (capacity >= item.weight) {
                    capacity -= item.weight;
                    totalValue += item.value;
                } else {
                    totalValue += item.ratio * capacity;
                    break;
                }
            }

            return totalValue;
        }
    }
