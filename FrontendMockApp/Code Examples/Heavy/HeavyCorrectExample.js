class Solution {
    fractionalKnapsack(items, capacity) {
        items.sort((a, b) => b.ratio - a.ratio);

        let totalValue = 0.0;

        for (const item of items) {
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