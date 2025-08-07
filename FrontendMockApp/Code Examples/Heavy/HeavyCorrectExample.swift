class Solution {
    func fractionalKnapsack(_ items: [Item], _ capacity: Int) -> Double {
        let sortedItems = items.sorted { $0.ratio > $1.ratio }
        var remainingCapacity = capacity
        var totalValue: Double = 0.0

        for item in sortedItems {
            if remainingCapacity >= item.weight {
                remainingCapacity -= item.weight
                totalValue += Double(item.value)
            } else {
                totalValue += item.ratio * Double(remainingCapacity)
                break
            }
        }

        return totalValue
    }
}