class Solution {
    func fractionalKnapsack(_ items: [Item], _ capacity: Int) -> Double {
        let sortedItems = items.sorted { $0.ratio > $1.ratio }
        return fractionalKnapsackRecursive(sortedItems, capacity, 0)
    }

    private func fractionalKnapsackRecursive(_ items: [Item], _ capacity: Int, _ index: Int) -> Double {
        if capacity == 0 || index >= items.count {
            return 0.0
        }

        var takeFull = 0.0
        if items[index].weight <= capacity {
            takeFull = Double(items[index].value) + fractionalKnapsackRecursive(items, capacity - items[index].weight, index + 1)
        }

        let takePartial = items[index].ratio * Double(min(capacity, items[index].weight))
        let skip = fractionalKnapsackRecursive(items, capacity, index + 1)

        return max(takeFull, max(takePartial, skip))
    }
}