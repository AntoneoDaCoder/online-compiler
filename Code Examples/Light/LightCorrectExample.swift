
    class Solution {
        func findMinimum(_ arr: [Int]) -> Int {
            var min = arr[0]
            for i in 1..<arr.count {
                if arr[i] < min {
                    min = arr[i]
                }
            }
            return min
        }
    }

