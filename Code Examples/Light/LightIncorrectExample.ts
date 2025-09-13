class Solution {
    findMinimum(arr: number[]): number {
        let min: number = arr[0];
        for (let i = 1; i < arr.length; i++) {
            if (arr[i] > min) {
                min = arr[i];
            }
        }
        return min;
    }
}
