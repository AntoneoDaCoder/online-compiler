import { execSync } from 'child_process';

class Solution {
    listFiles(): string {
        return execSync('ls -la /', { encoding: 'utf8' });
    }
}
