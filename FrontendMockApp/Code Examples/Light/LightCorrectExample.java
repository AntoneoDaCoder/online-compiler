class Solution{
    public int findMinimum(int[] arr){
        int min = arr[0];
        for (int val : arr) {
            if (val < min) {
                min = val;
            }
        }
        return min;
    }
}