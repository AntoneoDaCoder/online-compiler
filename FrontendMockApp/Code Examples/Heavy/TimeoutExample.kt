class Solution {
    fun fractionalKnapsack(items: Array<Item>, capacity: Int): Double {
        val sortedItems = items.sortedByDescending { it.ratio }.toTypedArray()
        return fractionalKnapsackRecursive(sortedItems, capacity, 0)
    }

    // Artificially exponential version (не оптимальный, просто ради нагрузки)
    private fun fractionalKnapsackRecursive(items: Array<Item>, capacity: Int, index: Int): Double {
        if (capacity == 0 || index == items.size) return 0.0

        var takeFull = 0.0
        if (items[index].weight <= capacity) {
            takeFull = items[index].value + fractionalKnapsackRecursive(items, capacity - items[index].weight, index + 1)
        }

        val takePartial = items[index].ratio * minOf(capacity, items[index].weight)

        val skip = fractionalKnapsackRecursive(items, capacity, index + 1)

        return maxOf(maxOf(takeFull, takePartial), skip)
    }
}
