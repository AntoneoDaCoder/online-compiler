class Solution {
    public double fractionalKnapsack(Item[] items, int capacity) {
    java.util.Arrays.sort(items, (a, b) -> Double.compare(b.getRatio(), a.getRatio()));

    double totalValue = 0.0;

    for (Item item : items) {
        if (capacity >= item.weight) {
            capacity -= item.weight;
            totalValue += item.value;
        } else {
            totalValue += item.getRatio() * capacity;
            break;
        }
    }

    return totalValue;
}

}