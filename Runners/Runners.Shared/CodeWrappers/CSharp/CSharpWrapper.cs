using Newtonsoft.Json.Linq;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using System.Text;

namespace Runners.Shared.CodeWrappers.CSharp
{
    public class CSharpWrapper : ITestWrapper
    {
        const string _boilerplateUsings = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using System.Threading;
                using NUnit.Framework;
                using NUnitLite;
                """;

        public string GenerateSource(ManifestDto manifest, string userCode, string entrypointContainerClass = "SolutionContainer", int defaultTimeoutMs = 2000)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            var sb = new StringBuilder();

            // usings
            sb.AppendLine(_boilerplateUsings);

            sb.AppendLine("namespace GeneratedSubmission");
            sb.AppendLine("{");

            // RunnerHelpers
            sb.AppendLine(CSharpBaseSourceCode.Source);
            sb.AppendLine();

            // Test counter/monitor
            sb.AppendLine("    public static class __TestMonitor");
            sb.AppendLine("    {");
            sb.AppendLine("        private static int _passed = 0;");
            sb.AppendLine("        public static void Inc() => System.Threading.Interlocked.Increment(ref _passed);");
            sb.AppendLine("        public static int Get() => System.Threading.Volatile.Read(ref _passed);");
            sb.AppendLine("    }");
            sb.AppendLine();

            // include helpers.inline
            if (!string.IsNullOrWhiteSpace(manifest.Helpers?.Inline))
            {
                sb.AppendLine(manifest.Helpers.Inline);
                sb.AppendLine();
            }

            // user code inside container
            sb.AppendLine($"    public static class {entrypointContainerClass}");
            sb.AppendLine("    {");
            sb.AppendLine(userCode);
            sb.AppendLine("    }");
            sb.AppendLine();

            // advanced tests code (if provided). Place inside AdvancedTestsContainer
            if (manifest.AdvancedTests != null && manifest.AdvancedTests.Count > 0)
            {
                sb.AppendLine("    public static class AdvancedTestsContainer");
                sb.AppendLine("    {");
                foreach (var adv in manifest.AdvancedTests)
                {
                    sb.AppendLine(adv.Source ?? "");
                    sb.AppendLine();
                }
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Program.Main wrapper — ensure we always print PassedTests:<n> to stdout on termination
            sb.AppendLine("    public class Program");
            sb.AppendLine("    {");
            sb.AppendLine("        static int Main(string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                var argsWithNoResult = args.Concat(new[] { \"--noresult\" }).ToArray();");
            sb.AppendLine("                var result = new AutoRun().Execute(argsWithNoResult);");
            sb.AppendLine("                return result;");
            sb.AppendLine("            }");
            sb.AppendLine("            finally");
            sb.AppendLine("            {");
            sb.AppendLine("                try");
            sb.AppendLine("                {");
            sb.AppendLine("                    Console.WriteLine($\"PassedTests:{__TestMonitor.Get()}\");");
            sb.AppendLine("                    Console.Out.Flush();");
            sb.AppendLine("                }");
            sb.AppendLine("                catch { }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // GeneratedTests
            sb.AppendLine("    [TestFixture]");
            sb.AppendLine("    public class GeneratedTests");
            sb.AppendLine("    {");

            // Sample tests
            int idx = 0;
            foreach (var st in manifest.SampleTests ?? Enumerable.Empty<SampleTest>())
            {
                idx++;
                var testMethodName = SanitizeMethodName($"Sample_{st.Name}_{idx}");
                sb.AppendLine("        [Test]");
                sb.AppendLine($"        public async Task {testMethodName}()");
                sb.AppendLine("        {");

                // compute per-test timeout (use long internally)
                var timeoutMsExpr = (st.TimeoutMs > 0) ? st.TimeoutMs : defaultTimeoutMs;
                sb.AppendLine($"            var __timeout = TimeSpan.FromMilliseconds({timeoutMsExpr}L);");

                // render inputs into local variables
                if (st.Inputs.HasValue)
                {
                    var j = JToken.Parse(st.Inputs.Value.GetRawText());
                    if (j is JArray arr)
                    {
                        for (int i = 0; i < arr.Count; i++)
                        {
                            var pType = (manifest.Signature.Parameters != null && manifest.Signature.Parameters.Count > i)
                                ? manifest.Signature.Parameters[i].Type
                                : null;
                            var rendered = CSharpTokenParser.Render(arr[i], pType);
                            var csharpType = pType != null ? CSharpTokenParser.RenderTypeName(pType) : "object";
                            sb.AppendLine($"            var arg{i} = {rendered};");
                        }
                        var argsList = string.Join(", ", Enumerable.Range(0, arr.Count).Select(i => $"arg{i}"));
                        sb.AppendLine($"            object?[] __args = new object?[] {{ {argsList} }};");
                    }
                    else
                    {
                        var pType = manifest.Signature.Parameters != null && manifest.Signature.Parameters.Count > 0 ? manifest.Signature.Parameters[0].Type : null;
                        var rendered = CSharpTokenParser.Render(j, pType);
                        sb.AppendLine($"            var arg0 = {rendered};");
                        sb.AppendLine($"            object?[] __args = new object?[] {{ arg0 }};");
                    }
                }
                else
                {
                    sb.AppendLine($"            object?[] __args = new object?[] {{}};");
                }

                // determine return type
                var returnTypeRaw = manifest.Signature?.ReturnType ?? "void";
                var parsed = ParseReturnType(returnTypeRaw);

                // call & await with timeout
                if (!parsed.HasResult)
                {
                    if (parsed.IsTask)
                    {
                        sb.AppendLine($"            var __call = {entrypointContainerClass}.{manifest.Entrypoint}({GenerateArgsInvocation(manifest)});");
                        sb.AppendLine($"            var __completed = await Task.WhenAny(__call, Task.Delay(__timeout));");
                        sb.AppendLine($"            if (!ReferenceEquals(__completed, __call)) Assert.Fail(\"Test execution timed out\");");
                    }
                    else
                    {
                        sb.AppendLine($"            var __call = Task.Run(() => {{ {entrypointContainerClass}.{manifest.Entrypoint}({GenerateArgsInvocation(manifest)}); }});");
                        sb.AppendLine($"            var __completed = await Task.WhenAny(__call, Task.Delay(__timeout));");
                        sb.AppendLine($"            if (!ReferenceEquals(__completed, __call)) Assert.Fail(\"Test execution timed out\");");
                    }

                    // success -> increment counter
                    sb.AppendLine("            __TestMonitor.Inc();");
                }
                else
                {
                    var rt = parsed.ResultTypeCSharp;
                    if (parsed.IsTask)
                    {
                        sb.AppendLine($"            var __call = {entrypointContainerClass}.{manifest.Entrypoint}({GenerateArgsInvocation(manifest)});");
                        sb.AppendLine($"            var __completed = await Task.WhenAny(__call, Task.Delay(__timeout));");
                        sb.AppendLine($"            if (!ReferenceEquals(__completed, __call)) Assert.Fail(\"Test execution timed out\");");
                        sb.AppendLine($"            var __actual = await __call;");
                    }
                    else
                    {
                        sb.AppendLine($"            var __call = Task.Run(() => {entrypointContainerClass}.{manifest.Entrypoint}({GenerateArgsInvocation(manifest)}));");
                        sb.AppendLine($"            var __completed = await Task.WhenAny(__call, Task.Delay(__timeout));");
                        sb.AppendLine($"            if (!ReferenceEquals(__completed, __call)) Assert.Fail(\"Test execution timed out\");");
                        sb.AppendLine($"            var __actual = await __call;");
                    }

                    if (st.Expected.HasValue)
                    {
                        var expectedToken = JToken.Parse(st.Expected.Value.GetRawText());
                        var expectedRendered = CSharpTokenParser.Render(expectedToken, parsed.ResultTypeDescriptor);
                        sb.AppendLine($"            var __expected = {expectedRendered};");
                    }
                    else
                    {
                        sb.AppendLine($"            var __expected = default({rt});");
                    }

                    var comparator = string.IsNullOrWhiteSpace(st.Comparator) ? "eq" : st.Comparator;
                    sb.AppendLine($"            if (!RunnerHelpers.Compare(__actual, __expected, \"{comparator}\"))");
                    sb.AppendLine($"                Assert.Fail($\"Sample test '{st.Name}' failed. Expected={{__expected}} Actual={{__actual}}\");");

                    // success -> increment counter
                    sb.AppendLine("            __TestMonitor.Inc();");
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            } // end sample tests

            // Advanced tests
            if (manifest.AdvancedTests != null)
            {
                int advIdx = 0;
                foreach (var adv in manifest.AdvancedTests)
                {
                    advIdx++;
                    var methodName = adv.Name;
                    var testMethodName = SanitizeMethodName($"Advanced_{methodName}_{advIdx}");
                    sb.AppendLine("        [Test]");
                    sb.AppendLine($"        public async Task {testMethodName}()");
                    sb.AppendLine("        {");

                    var advTimeout = adv.TimeoutMs > 0 ? adv.TimeoutMs : defaultTimeoutMs;
                    sb.AppendLine($"            var __timeout = TimeSpan.FromMilliseconds({advTimeout}L);");

                    // call advanced test method (assume it returns Task or Task<T> or void)
                    sb.AppendLine($"            var __call = AdvancedTestsContainer.{methodName}();");
                    sb.AppendLine($"            var __completed = await Task.WhenAny(__call, Task.Delay(__timeout));");
                    sb.AppendLine($"            if (!ReferenceEquals(__completed, __call)) Assert.Fail(\"Advanced test timed out\");");

                    // success -> increment counter
                    sb.AppendLine("            __TestMonitor.Inc();");

                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    }"); // class
            sb.AppendLine("}"); // namespace

            return sb.ToString();
        }

        private string SanitizeMethodName(string name)
        {
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }

        private (bool IsTask, bool HasResult, string ResultTypeCSharp, TypeDescriptor? ResultTypeDescriptor) ParseReturnType(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return (false, false, "void", null);

            raw = raw.Trim();
            if (raw.StartsWith("Task<") && raw.EndsWith(">"))
            {
                var inner = raw.Substring(5, raw.Length - 6).Trim();
                var descr = new TypeDescriptor();
                if (inner.EndsWith("[]"))
                {
                    descr.Kind = "array";
                    descr.Items = new TypeDescriptor { Kind = "class", Name = inner.Substring(0, inner.Length - 2) };
                }
                else if (IsPrimitiveName(inner))
                {
                    descr.Kind = "primitive";
                    descr.Name = inner;
                }
                else
                {
                    descr.Kind = "class";
                    descr.Name = inner;
                }
                return (true, true, MapToCSharpType(inner), descr);
            }
            if (raw == "Task")
                return (true, false, "void", null);

            if (raw == "void")
                return (false, false, "void", null);

            if (raw.EndsWith("[]"))
            {
                var inner = raw.Substring(0, raw.Length - 2);
                var descr = IsPrimitiveName(inner) ? new TypeDescriptor { Kind = "array", Items = new TypeDescriptor { Kind = "primitive", Name = inner } }
                                                   : new TypeDescriptor { Kind = "array", Items = new TypeDescriptor { Kind = "class", Name = inner } };
                return (false, true, MapToCSharpType(raw), descr);
            }

            var td = IsPrimitiveName(raw) ? new TypeDescriptor { Kind = "primitive", Name = raw } : new TypeDescriptor { Kind = "class", Name = raw };
            return (false, true, MapToCSharpType(raw), td);
        }

        private bool IsPrimitiveName(string n)
        {
            return new[] { "int", "long", "double", "string", "bool", "void" }.Contains(n);
        }

        private string MapToCSharpType(string raw)
        {
            raw = raw.Trim();
            if (raw.EndsWith("[]"))
            {
                var inner = raw.Substring(0, raw.Length - 2);
                return inner + "[]";
            }
            if (raw == "int" || raw == "long" || raw == "double" || raw == "string" || raw == "bool")
                return raw;
            return raw;
        }

        private string GenerateArgsInvocation(ManifestDto manifest)
        {
            var count = manifest.Signature?.Parameters?.Count ?? 0;
            if (count == 0) return "";
            return string.Join(", ", Enumerable.Range(0, count).Select(i => $"arg{i}"));
        }
    }
}