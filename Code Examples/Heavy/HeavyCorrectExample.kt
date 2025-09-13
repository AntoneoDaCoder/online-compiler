class Solution {
    fun fractionalKnapsack(items: Array<Item>, capacity: Int): Double {
        val sortedItems = items.sortedByDescending { it.ratio }
        var remainingCapacity = capacity
        var totalValue = 0.0

        for (item in sortedItems) {
            if (remainingCapacity >= item.weight) {
                remainingCapacity -= item.weight
                totalValue += item.value
            } else {
                totalValue += item.ratio * remainingCapacity
                break
            }
        }

        return totalValue
    }
}
