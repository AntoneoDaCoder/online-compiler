class Solution {
  fractionalKnapsack(items, capacity) {
    // сортируем по убыванию ratio
    const sortedItems = [...items].sort((a, b) => b.ratio - a.ratio);
    return this.fractionalKnapsackRecursive(sortedItems, capacity, 0);
  }

  // Неоптимальная экспоненциальная версия
  fractionalKnapsackRecursive(items, capacity, index) {
    if (capacity === 0 || index === items.length) {
      return 0.0;
    }

    let takeFull = 0;
    if (items[index].weight <= capacity) {
      takeFull =
        items[index].value +
        this.fractionalKnapsackRecursive(
          items,
          capacity - items[index].weight,
          index + 1
        );
    }

    const takePartial =
      items[index].ratio * Math.min(capacity, items[index].weight);

    const skip = this.fractionalKnapsackRecursive(items, capacity, index + 1);

    return Math.max(takeFull, takePartial, skip);
  }
}