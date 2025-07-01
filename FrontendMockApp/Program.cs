using FrontendMockApp.Services;

class Program
{
    const string _callbackUrl = "http://host.docker.internal:23456/callback/";
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

        string[] filePaths = Directory.GetFiles(examplesDir, "*.cs", searchOption: SearchOption.AllDirectories);
        foreach (var filePath in filePaths)
        {
            var fileName = Path.GetFileName(filePath);
            examples[fileName] = File.ReadAllText(filePath);
        }

        return examples;
    }
    static async Task<int> Main()
    {
        var examples = LoadCodeExamples();

        var requestObserver = new RequestObserver();
        var requestManager = new RequestManager();
        requestObserver.Subscribe(requestManager);


        using (var producer = new CodeRequestProducer(examples, _callbackUrl, requestObserver))
        using (var consumer = new CodeResponseConsumer(_urlPrefix, requestObserver))
        {
            consumer.Start();
            producer.Start();

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
                
            }

            await consumer.StopAsync();
            await producer.StopAsync();
        }

        return 0;
    }
}
