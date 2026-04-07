using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Core.Services;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.DAL.Extensions;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.ConflictExceptions;
using Shared.DTOs;
using Shared.Helpers;
using System.Text.Json;

class Program
{
    private static bool _seedingFailed = false;
    const string _bucketName = "manifestbucket";
    private static Guid _adminId;

    private static Dictionary<string, string> _languages = new()
        {
            { "csharp","C#"},
            { "nodejs","NodeJS"},
            { "java","Java"},
            { "kotlin","Kotlin"},
            { "typescript","TypeScript"},
        };

    private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    private class SampleProblemData
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Statement { get; set; }
        public string ManifestKey { get; set; }
    }

    private static List<SampleProblemData> ParseProblemsFromFile(string filePath)
    {
        var result = new List<SampleProblemData>();

        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return result;
            }

            string jsonContent = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                Console.WriteLine($"File is empty: {filePath}");
                return result;
            }

            var problems = JsonSerializer.Deserialize<List<SampleProblemData>>(jsonContent, _defaultOptions);
            if (problems != null)
            {
                result.AddRange(problems);
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[Problem-Seeding] JSON parsing error in {filePath}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Problem-Seeding] Error reading file {filePath}: {ex.Message}");
        }

        return result;
    }

    private static Dictionary<string, ManifestDto> ParseAllManifestsFromDirectory(string directoryPath)
    {
        var manifests = new Dictionary<string, ManifestDto>();

        if (!Directory.Exists(directoryPath))
        {
            return manifests;
        }

        var allFiles = Directory.GetFiles(directoryPath);

        foreach (var filePath in allFiles)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    continue;
                }

                var manifest = ManifestParser.Parse(jsonContent, null);

                var fileName = Path.GetFileNameWithoutExtension(filePath);

                manifests.Add(fileName, manifest);
            }
            catch
            {
                // Пропускаем файлы, которые не могут быть распарсены
                continue;
            }
        }

        return manifests;
    }

    private static void MigrateDb(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BaseDbContext>();

        try
        {
            Console.WriteLine("Applying migrations...");
            dbContext.Database.Migrate();
            Console.WriteLine("Migrations applied successfully.");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
        }
    }

    private static async Task<IEnumerable<LanguageEntity>> SeedLanguagesAsync(IServiceProvider sp)
    {
        var languages = new List<LanguageEntity>();

        await using (var scope = sp.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ILanguageRepository>();

            foreach (var lang in _languages)
            {
                if (await repo.GetByCodeAsync(lang.Key) is not null)
                    continue;

                var langEntity = new LanguageEntity()
                {
                    Id = Guid.NewGuid(),
                    Code = lang.Key,
                    DisplayName = lang.Value,
                };
                try
                {
                    languages.Add(await repo.CreateAsync(langEntity));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Languages-Seeding][Warning] Failed to seed {lang.Key} language. Skipping. Reason: " + ex);
                }
            }
        }

        return languages;
    }


    private static async Task SeedProblemsAsync(IServiceProvider sp)
    {
        var problemData = ParseProblemsFromFile(@"SampleDbSeedingData/sample_problem_descriptions.json");

        if (problemData is null)
        {
            throw new ArgumentNullException("[Problem-Seeding][Error] Failed. Couldn't get problem samples. Problem seeding aborted");
        }

        var sampleManifests = ParseAllManifestsFromDirectory(@"SampleDbSeedingData/sample_manifests");

        if (sampleManifests.Count == 0)
        {
            Console.WriteLine("[Problem-Seeding][Warning] No manifests found. All problems will be added without test manifests");
        }

        await using (var scope = sp.CreateAsyncScope())
        {
            var problemRepo = scope.ServiceProvider.GetRequiredService<IProblemRepository>();
            var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
            var problemLanguageRepo = scope.ServiceProvider.GetRequiredService<IProblemVersionLanguageRepository>();
            // Получим DbContext напрямую для вставки версии и апдейта problem,
            // т.к. репозиторий problemRepo может быть реализован так, что он делает SaveChanges внутри.
            var db = scope.ServiceProvider.GetRequiredService<BaseDbContext>();

            var languages = await db.Languages.ToListAsync();

            if (!languages.Any())
            {
                throw new ArgumentNullException("[Problem-Seeding][Error] Failed. Couldn't get languages. Problem seeding aborted");
            }

            foreach (var data in problemData)
            {
                if (await problemRepo.GetLatestVersionBySlugAsync(data.Slug) is not null)
                    continue;

                var problemId = Guid.NewGuid();
                var versionId = Guid.NewGuid();

                string? key = $"problems/{problemId}/versions/{versionId}/template.json";

                if (!sampleManifests.TryGetValue(data.ManifestKey, out var manifest))
                {
                    Console.WriteLine($"[Problem-Seeding][Warning] No manifest found. Problem {data.Slug} will be added without manifest key.");
                    key = null;
                }
                else
                {
                    try
                    {
                        var manifestString = JsonSerializer.Serialize(manifest, _defaultOptions);

                        if (!await storage.UploadStringAsync(_bucketName, key, manifestString))
                        {
                            Console.WriteLine($"[Problem-Seeding][Warning] Minio failed to accept manifest. Problem {data.Slug} will be added without manifest key.");
                            key = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Problem-Seeding][Warning] Failed to publish manifest to Minio due to an exception. Problem {data.Slug} will be added without manifest key. Reason: " + ex);
                        key = null;
                    }
                }

                // 1) создаём версию-объект (но НЕ привязываем навигационно к problem)
                var version = new ProblemVersionEntity()
                {
                    Id = versionId,
                    ProblemId = problemId, // указываем ProblemId, но соответствующая problem ещё не в БД — это ок, мы сохраним problem первым (ниже)
                    Statement = data.Statement,
                    Version = 0,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = _adminId,
                    IsPublished = true,
                    IsDraft = false,
                    PublishedBy = _adminId,
                    TestTemplateKey = key,
                    TotalTests = (manifest is not null) ? manifest.SampleTests.Count + manifest.AdvancedTests.Count : 0,
                };

                // 2) создаём problem без установки LastPublishedVersion / LastPublishedVersionId (чтобы избежать цикла)
                var problemEntity = new ProblemEntity()
                {
                    Id = problemId,
                    Slug = data.Slug,
                    Title = data.Title,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = _adminId
                    // LastPublishedVersion и LastPublishedVersionId НЕ ставим сейчас
                };

                try
                {
                    // Сохраняем problem первым (через репозиторий, чтобы учесть логику репо)
                    var createdProblem = await problemRepo.CreateAsync(problemEntity);

                    // Затем сохраняем версию напрямую через DbContext (или через репозиторий версии, если он есть)
                    // Убедимся, что version.ProblemId установлен на фактический createdProblem.Id
                    version.ProblemId = createdProblem.Id;
                    db.ProblemVersions.Add(version);
                    await db.SaveChangesAsync();

                    // После того как версия создана — обновляем problem: устанавливаем last_published_version (id) и сохраняем.
                    createdProblem.LastPublishedVersionId = version.Id;
                    // Если у вас есть навигационное свойство, не присваивайте объект навигации сразу — достаточно id
                    db.Problems.Update(createdProblem);
                    await db.SaveChangesAsync();

                    foreach (var lang in languages)
                    {
                        var entity = new ProblemVersionLanguage()
                        {
                            LanguageId = lang.Id,
                            VersionId = versionId
                        };

                        await problemLanguageRepo.CreateAsync(entity);
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Problem-Seeding][Warning] Failed to seed problem {data.Slug}. Skipping. Reason: " + ex);
                }
            }
        }
    }


    //private static async Task SeedUsersAsync(IServiceProvider sp)
    //{
    //    await using var scope = sp.CreateAsyncScope();
    //    {
    //        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    //        var prot = scope.ServiceProvider.GetRequiredService<ISecretProtector>();

    //        IEnumerable<string> roles = ["User", "Admin", "Editor"];

    //        foreach (var role in roles)
    //        {
    //            if (await repo.GetRoleByNameAsync(role) is not null)
    //                continue;

    //            await repo.AddRoleAsync(role);
    //        }


    //        try
    //        {
    //            var name = "antoneo228";
    //            var mail = "antonurbanovic@gmail.com";
    //            var passw = "Abcd12345";
    //            var emailHash = CryptoHelpers.ComputeSha256Hex(mail);

    //            var adminUsr = await repo.GetByHashedEmailAsync(emailHash);

    //            if (adminUsr is not null)
    //            {
    //                _adminId = adminUsr.Id;
    //            }
    //            else
    //            {
    //                var newId = Guid.NewGuid();

    //                var now = DateTimeOffset.UtcNow;

    //                var newUser = new UserEntity()
    //                {
    //                    Id = newId,
    //                    CreatedAt = DateTimeOffset.UtcNow,
    //                    Name = name,
    //                    EmailHash = emailHash,
    //                    EncryptedEmail = prot.Protect(mail),
    //                    RefreshToken = null,
    //                    RefreshTokenExpiryTime = DateTimeOffset.MinValue,

    //                    SecurityStamp = newId.ToString(),
    //                    ConcurrencyStamp = newId.ToString(),
    //                    UserName = name,
    //                    Email = newId.ToString(),
    //                    NormalizedUserName = newId.ToString().ToUpperInvariant(),
    //                    NormalizedEmail = newId.ToString().ToUpperInvariant(),
    //                    EmailConfirmed = true,
    //                    LockoutEnabled = true,
    //                    AccessFailedCount = 0,
    //                    TwoFactorEnabled = false,
    //                    PhoneNumberConfirmed = false,
    //                };

    //                var res = await repo.CreateAsync(newUser, passw);

    //                if (!res.Succeeded)
    //                    Console.WriteLine(string.Join('\n', res.Errors));

    //                var roleRes = await repo.AddToRolesAsync(newUser, ["User", "Admin", "Editor"]);

    //                _adminId = newId;
    //            }
    //        }
    //        catch (ConflictException)
    //        {

    //        }

    //        try
    //        {
    //            var name = "test_editor";
    //            var mail = "a@gmail.com";
    //            var passw = "Abcd12345";
    //            var emailHash = CryptoHelpers.ComputeSha256Hex(mail);

    //            var editorUsr = await repo.GetByHashedEmailAsync(emailHash);

    //            if (editorUsr is null)
    //            {
    //                var newId = Guid.NewGuid();

    //                var now = DateTimeOffset.UtcNow;

    //                var newUser = new UserEntity()
    //                {
    //                    Id = newId,
    //                    CreatedAt = DateTimeOffset.UtcNow,
    //                    CreatedBy = _adminId,
    //                    Name = name,
    //                    EmailHash = emailHash,
    //                    EncryptedEmail = prot.Protect(mail),
    //                    RefreshToken = null,
    //                    RefreshTokenExpiryTime = DateTimeOffset.MinValue,

    //                    SecurityStamp = newId.ToString(),
    //                    ConcurrencyStamp = newId.ToString(),
    //                    UserName = name,
    //                    Email = newId.ToString(),
    //                    NormalizedUserName = newId.ToString().ToUpperInvariant(),
    //                    NormalizedEmail = newId.ToString().ToUpperInvariant(),
    //                    EmailConfirmed = true,
    //                    LockoutEnabled = true,
    //                    AccessFailedCount = 0,
    //                    TwoFactorEnabled = false,
    //                    PhoneNumberConfirmed = false,
    //                };

    //                var res = await repo.CreateAsync(newUser, passw);

    //                if (!res.Succeeded)
    //                    Console.WriteLine(string.Join('\n', res.Errors));

    //                var roleRes = await repo.AddToRolesAsync(newUser, ["User", "Editor"]);
    //            }
    //        }
    //        catch (ConflictException)
    //        {

    //        }

    //        try
    //        {
    //            var name = "generic_user";
    //            var mail = "b@gmail.com";
    //            var passw = "Abcd12345";
    //            var emailHash = CryptoHelpers.ComputeSha256Hex(mail);

    //            var genUsr = await repo.GetByHashedEmailAsync(emailHash);

    //            if (genUsr is null)
    //            {
    //                var newId = Guid.NewGuid();

    //                var now = DateTimeOffset.UtcNow;

    //                var newUser = new UserEntity()
    //                {
    //                    Id = newId,
    //                    CreatedAt = DateTimeOffset.UtcNow,
    //                    CreatedBy = _adminId,
    //                    Name = name,
    //                    EmailHash = emailHash,
    //                    EncryptedEmail = prot.Protect(mail),
    //                    RefreshToken = null,
    //                    RefreshTokenExpiryTime = DateTimeOffset.MinValue,

    //                    SecurityStamp = newId.ToString(),
    //                    ConcurrencyStamp = newId.ToString(),
    //                    UserName = name,
    //                    Email = newId.ToString(),
    //                    NormalizedUserName = newId.ToString().ToUpperInvariant(),
    //                    NormalizedEmail = newId.ToString().ToUpperInvariant(),
    //                    EmailConfirmed = true,
    //                    LockoutEnabled = true,
    //                    AccessFailedCount = 0,
    //                    TwoFactorEnabled = false,
    //                    PhoneNumberConfirmed = false,
    //                };

    //                var res = await repo.CreateAsync(newUser, passw);

    //                if (!res.Succeeded)
    //                    Console.WriteLine(string.Join('\n', res.Errors));

    //                var roleRes = await repo.AddToRolesAsync(newUser, ["User"]);
    //            }
    //        }
    //        catch (ConflictException)
    //        {

    //        }
    //    }
    //}

    static IServiceProvider BuildServiceProvider()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var confRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

                services.AddSingleton<IConfiguration>(confRoot);

                services.ConfigureDbContext(confRoot);
                services.ConfigureObjectStorage(confRoot);
                services.ConfigureRepositories();
            })
            .Build();

        return host.Services;
    }

    static async Task<int> Main()
    {
        /* Steps
         * 1. Apply migration - done
         * 2. seed languages - done
         * 3. seed problems and their latest versions - done
         * 4. seed users - done
         */

        var sp = BuildServiceProvider();

        try
        {
            MigrateDb(sp);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Migration][Error] Failed to migrate db. Reason: " + ex);
            return 1;
        }

        //try
        //{
        //    await SeedUsersAsync(sp);
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine("[User-Seeding][Error] Failed to seed users. Reason: " + ex);
        //    _seedingFailed = true;
        //}


        IEnumerable<LanguageEntity> actualLanguages = [];

        try
        {
            actualLanguages = await SeedLanguagesAsync(sp);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Language-Seeding][Error] Failed to seed languages. Reason: " + ex);
            return 1;
        }

        try
        {
            await SeedProblemsAsync(sp);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Problem-Seeding][Error] Failed to seed problems. Reason: " + ex);
            _seedingFailed = true;
        }

        if (_seedingFailed)
        {
            Console.WriteLine("[Error] One or more errors occured during seeding. Investigate logs. Operation failed.");
            return 1;
        }

        return 0;
    }
}
