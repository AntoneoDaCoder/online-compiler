using FrontendMockApp.Services;
using System.Text;
using System.Text.Json;

class Program
{
    const string _callbackUrl = "http://host.minikube.internal:23456/callback/";
    const string _urlPrefix = "http://+:23456/callback/";


    //do netsh for consumer to work
    //netsh http add urlacl url=http://+:23456/callback/ user=username
    //after you're done, do this
    //netsh http delete urlacl url=http://+:23456/callback/

    static Dictionary<string, string> LoadCodeExamples()
    {
        var examples = new Dictionary<string, string>();

        string baseDir = AppContext.BaseDirectory;
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        string examplesDir = Path.Combine(projectRoot, "Code Examples");

        if (!Directory.Exists(examplesDir))
        {
            throw new DirectoryNotFoundException($"Folder not found: {examplesDir}");
        }

        string[] filePaths = Directory.GetFiles(examplesDir, "*.cs", SearchOption.AllDirectories);
        foreach (var filePath in filePaths)
        {
            var fileName = Path.GetFileName(filePath);
            string code = File.ReadAllText(filePath);

            string extracted = ExtractOuterWrapper(code);
            examples[fileName] = extracted;
        }

        return examples;
    }

    static string ExtractOuterWrapper(string code)
    {
        var lines = code.Split('\n');
        var sb = new StringBuilder();
        int braceLevel = 0;
        bool copying = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (!copying)
            {
                if (line.Contains("class Solution"))
                {
                    copying = true;
                }
            }

            if (copying)
            {
                sb.AppendLine(rawLine);


                braceLevel += rawLine.Count(c => c == '{');
                braceLevel -= rawLine.Count(c => c == '}');

                if (braceLevel == 0 && line.Contains("}"))
                    break;
            }
        }

        return sb.ToString().Trim();
    }

    static async Task<int> Main()
    {
        var examples = LoadCodeExamples();

        using var tester = new APITester(_callbackUrl, _urlPrefix, examples);
        {
            tester.StartSession("Statistics of long-running pods approach");

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {

            }

            var sessionResults = await tester.EndSession();

            Console.WriteLine(JsonSerializer.Serialize(sessionResults, new JsonSerializerOptions { WriteIndented = true }));
        }

        return 0;
    }
}
