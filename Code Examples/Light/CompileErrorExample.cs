//find array min value example
class CompileErrorExample
{
    public class Solution
    {
        public int FindMinimum(int[] arr)
        {
            int min = arr[0];
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] < min)
                {
                    min = arr[i];
                }
            }
            return min
        }
    }
}
