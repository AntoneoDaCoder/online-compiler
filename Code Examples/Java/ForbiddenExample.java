class ForbiddenExample {
    public static class Solution {
        public static void openSite() throws IOException{
            try {
            ProcessBuilder pb = new ProcessBuilder("wget", "https://example.com");
            pb.redirectErrorStream(true); 
            Process process = pb.start();

            BufferedReader reader = new BufferedReader(
                new InputStreamReader(process.getInputStream()));

            String line;
            while ((line = reader.readLine()) != null) {
                System.out.println(line);
            }

            int exitCode = process.waitFor();
            System.out.println("Process finished with exit code " + exitCode);
        } catch (Exception e) {
            e.printStackTrace();
        }
        }
    }
