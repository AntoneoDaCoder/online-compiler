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
                using System.Collections;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using System.Threading;
                using NUnit.Framework;
                using NUnitLite;
                """;

        public string GenerateSource(ManifestDto manifest, string languageCode, string userCode, string entrypointContainerClass = "SolutionContainer", int defaultTimeoutMs = 2000)
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

            // include helpers.inline (filter by languageCode)
            var languageBlocks = manifest.Helpers?.FindAll(hb => hb.LanguageCode == languageCode);
            if (languageBlocks is not null)
                foreach (var block in languageBlocks)
                    if (block.Inline is not null)
                    {
                        sb.AppendLine(block.Inline);
                        sb.AppendLine();
                    }

            // always generate entrypoint container
            sb.AppendLine($"    public static class {entrypointContainerClass}");
            sb.AppendLine("    {");
            sb.AppendLine(userCode ?? string.Empty);
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

                // render inputs into local variables based on signature
                var paramList = manifest.Signature?.Parameters ?? new List<ParameterDescriptor>();
                int paramCount = paramList.Count;

                if (st.Inputs.HasValue && paramCount > 0)
                {
                    var j = JToken.Parse(st.Inputs.Value.GetRawText());

                    if (paramCount == 1)
                    {
                        var pType = paramList[0].Type;
                        var rendered = CSharpTokenParser.Render(j, pType);
                        sb.AppendLine($"            var arg0 = {rendered};");
                    }
                    else
                    {
                        if (j is JArray arr)
                        {
                            for (int i = 0; i < paramCount; i++)
                            {
                                var pType = i < paramCount ? paramList[i].Type : null;
                                JToken item = i < arr.Count ? arr[i] : JValue.CreateNull();
                                var rendered = CSharpTokenParser.Render(item, pType);
                                sb.AppendLine($"            var arg{i} = {rendered};");
                            }
                        }
                        else
                        {
                            var p0Type = paramList[0].Type;
                            var rendered0 = CSharpTokenParser.Render(j, p0Type);
                            sb.AppendLine($"            var arg0 = {rendered0};");
                            for (int i = 1; i < paramCount; i++)
                                sb.AppendLine($"            var arg{i} = default(object);");
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < paramCount; i++)
                        sb.AppendLine($"            var arg{i} = default(object);");
                }

                // determine return type descriptor (now TypeDescriptor)
                var returnTypeDescriptor = manifest.Signature?.ReturnType ?? new TypeDescriptor { Kind = "primitive", Name = "void" };
                var parsed = ParseReturnType(returnTypeDescriptor);

                // build invocation args string
                string invocationArgs = GenerateArgsInvocationBySignature(paramCount);

                // If parsed.ResultTypeCSharp is 'object' try to infer type from expected token (if present)
                string effectiveRt = parsed.ResultTypeCSharp;
                TypeDescriptor? effectiveRtDescriptor = parsed.ResultTypeDescriptor;
                bool needRuntimeCast = false;

                if (effectiveRt == "object" && st.Expected.HasValue)
                {
                    var expectedTokenTemp = JToken.Parse(st.Expected.Value.GetRawText());
                    var inferred = InferTypeDescriptorFromJToken(expectedTokenTemp);
                    if (inferred != null)
                    {
                        effectiveRtDescriptor = inferred;
                        effectiveRt = CSharpTokenParser.RenderTypeName(inferred);
                        needRuntimeCast = parsed.ResultTypeCSharp != effectiveRt;
                    }
                }

                // call & await with Task.WaitAsync(timeout)
                if (!parsed.HasResult)
                {
                    // void / no result path
                    sb.AppendLine($"            try");
                    sb.AppendLine($"            {{");
                    sb.AppendLine($"                var __call = Task.Run(() => {{ {entrypointContainerClass}.{manifest.Entrypoint}({invocationArgs}); }});");
                    sb.AppendLine($"                await __call.WaitAsync(__timeout);");
                    sb.AppendLine($"            }}");
                    sb.AppendLine($"            catch (TimeoutException) {{ Assert.Fail(\"Test execution timed out\"); }}");

                    // success -> increment counter
                    sb.AppendLine("            __TestMonitor.Inc();");
                }
                else
                {
                    var rt = effectiveRt;
                    // declare __actual with concrete/effective type so NUnit/collections get correct types
                    sb.AppendLine($"            {rt} __actual = default({rt});");
                    sb.AppendLine($"            try");
                    sb.AppendLine($"            {{");
                    sb.AppendLine($"                var __task = Task.Run(() => {entrypointContainerClass}.{manifest.Entrypoint}({invocationArgs}));");
                    sb.AppendLine($"                await __task.WaitAsync(__timeout);");
                    if (needRuntimeCast)
                        sb.AppendLine($"                __actual = ({rt}) await __task;");
                    else
                        sb.AppendLine($"                __actual = await __task;");
                    sb.AppendLine($"            }}");
                    sb.AppendLine($"            catch (TimeoutException) {{ Assert.Fail(\"Test execution timed out\"); }}");

                    // render expected using effective descriptor (if available) so types match
                    if (st.Expected.HasValue)
                    {
                        var expectedToken = JToken.Parse(st.Expected.Value.GetRawText());
                        var expectedRendered = CSharpTokenParser.Render(expectedToken, effectiveRtDescriptor);
                        sb.AppendLine($"            var __expected = {expectedRendered};");
                    }
                    else
                    {
                        sb.AppendLine($"            var __expected = default({rt});");
                    }

                    var comparator = string.IsNullOrWhiteSpace(st.Comparator) ? "eq" : st.Comparator;
                    // call helper that uses NUnit asserts and provides good diagnostics
                    sb.AppendLine($"            RunnerHelpers.AssertCompare(__actual, __expected, \"{comparator}\", \"Sample test '{st.Name}'\");");

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

                    sb.AppendLine($"            try");
                    sb.AppendLine($"            {{");
                    sb.AppendLine($"                var __call = AdvancedTestsContainer.{methodName}();");
                    sb.AppendLine($"                await __call.WaitAsync(__timeout);");
                    sb.AppendLine($"            }}");
                    sb.AppendLine($"            catch (TimeoutException) {{ Assert.Fail(\"Advanced test timed out\"); }}");

                    sb.AppendLine("            __TestMonitor.Inc();");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    }"); // class
            sb.AppendLine("}"); // namespace

            return sb.ToString();
        }

        private string GenerateArgsInvocationBySignature(int paramCount)
        {
            if (paramCount == 0) return "";
            if (paramCount == 1) return "arg0";
            return string.Join(", ", Enumerable.Range(0, paramCount).Select(i => $"arg{i}"));
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

        // --- Parse return type from TypeDescriptor (no Task<T> support here; returns IsTask=false)
        private (bool IsTask, bool HasResult, string ResultTypeCSharp, TypeDescriptor? ResultTypeDescriptor) ParseReturnType(TypeDescriptor? td)
        {
            if (td == null) return (false, false, "void", null);

            // void
            if (td.Kind == "primitive" && string.Equals(td.Name, "void", StringComparison.OrdinalIgnoreCase))
                return (false, false, "void", td);

            // array / primitive / class / nullable
            var rtName = CSharpTokenParser.RenderTypeName(td);
            return (false, true, rtName, td);
        }

        // ----- new helper: infer TypeDescriptor from a JToken (basic)
        private TypeDescriptor? InferTypeDescriptorFromJToken(JToken token)
        {
            if (token == null) return null;
            switch (token.Type)
            {
                case JTokenType.Null:
                    return null;
                case JTokenType.Integer:
                    return new TypeDescriptor { Kind = "primitive", Name = "int" };
                case JTokenType.Float:
                    return new TypeDescriptor { Kind = "primitive", Name = "double" };
                case JTokenType.Boolean:
                    return new TypeDescriptor { Kind = "primitive", Name = "bool" };
                case JTokenType.String:
                    return new TypeDescriptor { Kind = "primitive", Name = "string" };
                case JTokenType.Array:
                    {
                        var arr = token.Children().ToArray();
                        if (arr.Length == 0)
                        {
                            return new TypeDescriptor { Kind = "array", Items = new TypeDescriptor { Kind = "class", Name = "object" } };
                        }
                        TypeDescriptor? first = null;
                        bool allSame = true;
                        foreach (var c in arr)
                        {
                            var td = InferTypeDescriptorFromJToken(c);
                            if (first == null) first = td;
                            else
                            {
                                if (!TypeDescriptorsEqual(first, td)) { allSame = false; break; }
                            }
                        }
                        if (allSame && first != null)
                        {
                            if (first.Kind == "primitive")
                                return new TypeDescriptor { Kind = "array", Items = new TypeDescriptor { Kind = "primitive", Name = first.Name } };
                            return new TypeDescriptor { Kind = "array", Items = new TypeDescriptor { Kind = "class", Name = first.Name ?? "object" } };
                        }
                        return new TypeDescriptor { Kind = "array", Items = new TypeDescriptor { Kind = "class", Name = "object" } };
                    }
                case JTokenType.Object:
                    return new TypeDescriptor { Kind = "class", Name = "object" };
                default:
                    return new TypeDescriptor { Kind = "class", Name = "object" };
            }
        }

        private bool TypeDescriptorsEqual(TypeDescriptor? a, TypeDescriptor? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Kind != b.Kind) return false;
            if (a.Kind == "primitive") return a.Name == b.Name;
            if (a.Kind == "array")
            {
                if (a.Items == null && b.Items == null) return true;
                if (a.Items == null || b.Items == null) return false;
                return TypeDescriptorsEqual(a.Items, b.Items);
            }
            return a.Name == b.Name;
        }
    }
}