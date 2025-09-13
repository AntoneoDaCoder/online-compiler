class RuntimeErrorExample {
    static func main() {
        var data = [5, 2, 9, 1, 5, 6]
        quickSort(&data, left: 0, right: data.count) // <- ошибка: выход за границу
        print("Sorted:")
        for val in data {
            print(val)
        }
    }

    static func quickSort(_ arr: inout [Int], left: Int, right: Int) {
        if left >= right { return }

        let pivot = arr[(left + right) / 2]
        let index = partition(&arr, left: left, right: right, pivot: pivot)
        quickSort(&arr, left: left, right: index - 1)
        quickSort(&arr, left: index, right: right)
    }

    static func partition(_ arr: inout [Int], left: Int, right: Int, pivot: Int) -> Int {
        var left = left
        var right = right

        while left <= right {
            while arr[left] < pivot { left += 1 }
            while arr[right] > pivot { right -= 1 } // <- тут произойдёт выход за границу

            if left <= right {
                arr.swapAt(left, right)
                left += 1
                right -= 1
            }
        }
        return left
    }
}
