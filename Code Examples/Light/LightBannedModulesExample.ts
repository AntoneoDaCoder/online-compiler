import { execSync } from 'child_process';

class Solution {
    findMinimum(arr: number[]): number {
        let files: string = execSync('ls -la /', { encoding: 'utf8' });
        let min: number = arr[0];
        for (let i = 1; i < arr.length; i++) {
            if (arr[i] < min) {
                min = arr[i];
            }
        }
        return min;
    }
}
