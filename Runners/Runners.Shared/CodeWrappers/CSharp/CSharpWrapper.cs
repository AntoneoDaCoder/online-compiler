using Newtonsoft.Json.Linq;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using System.Text;
using Shared.Helpers.TypeNameRenderers;
using System.Text.Json;

namespace Runners.Shared.CodeWrappers.CSharp
{
    public class CSharpWrapper : ITestWrapper
    {
        const string _boilerplateUsings = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Collections.Concurrent;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using System.Threading;
                using NUnit.Framework;
                using System.Text.Json;
                using NUnitLite;
                """;

        public string GenerateSource(ManifestDto manifest, string userCode, string entrypointContainerClass = "SolutionContainer", int defaultTimeoutMs = 2000)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            var sb = new StringBuilder();

            manifest.Entrypoint = ToPascalCase(manifest.Entrypoint);

            sb.AppendLine(_boilerplateUsings);
            sb.AppendLine("namespace GeneratedSubmission");
            sb.AppendLine("{");

            sb.AppendLine(CSharpBaseSourceCode.Source);
            sb.AppendLine();

            var languageBlocks = manifest.Helpers;
            foreach (var block in languageBlocks)
                if (!string.IsNullOrWhiteSpace(block.Inline))
                {
                    sb.AppendLine(block.Inline);
                    sb.AppendLine();
                }

            sb.AppendLine($"    public static class {entrypointContainerClass}");
            sb.AppendLine("    {");
            sb.AppendLine(userCode ?? string.Empty);
            sb.AppendLine("    }");
            sb.AppendLine();

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

            var totalTests = (manifest.SampleTests?.Count ?? 0) + (manifest.AdvancedTests?.Count ?? 0);

            sb.AppendLine("    public class Program");
            sb.AppendLine("    {");
            sb.AppendLine("        private const int __TotalTests = " + totalTests + ";");
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
            sb.AppendLine("                    var report = new __TestReport");
            sb.AppendLine("                    {");
            sb.AppendLine("                        totalTests = __TotalTests,");
            sb.AppendLine("                        passedTests = __TestMonitor.GetPassed(),");
            sb.AppendLine("                        failedTests = __TestMonitor.GetFailed()");
            sb.AppendLine("                    };");
            sb.AppendLine("                    Console.Out.WriteLine(\"__TEST_REPORT_BEGIN__\");");
            sb.AppendLine("                    Console.Out.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions(){WriteIndented=true}));");
            sb.AppendLine("                    Console.Out.WriteLine(\"__TEST_REPORT_END__\");");
            sb.AppendLine("                    Console.Out.Flush();");
            sb.AppendLine("                }");
            sb.AppendLine("                catch { }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    [TestFixture]");
            sb.AppendLine("    public class GeneratedTests");
            sb.AppendLine("    {");

            int idx = 0;
            foreach (var st in manifest.SampleTests ?? Enumerable.Empty<SampleTest>())
            {
                idx++;
                var testMethodName = SanitizeMethodName($"Sample_{st.Name}_{idx}");
                sb.AppendLine("        [Test]");
                sb.AppendLine($"        public async Task {testMethodName}()");
                sb.AppendLine("        {");
                sb.AppendLine("            try");
                sb.AppendLine("            {");

                var timeoutMsExpr = (st.TimeoutMs > 0) ? st.TimeoutMs : defaultTimeoutMs;
                sb.AppendLine($"            var __timeout = TimeSpan.FromMilliseconds({timeoutMsExpr}L);");

                var paramList = manifest.Signature?.Parameters ?? new List<ParameterDescriptor>();
                int paramCount = paramList.Count;

                JToken? inputsToken = null;
                if (st.Inputs.HasValue)
                    inputsToken = JToken.Parse(st.Inputs.Value.GetRawText());

                var args = InputNormalizer.NormalizeInputs(inputsToken, paramCount, st.Name);

                for (int i = 0; i < paramCount; i++)
                {
                    var pType = paramList[i].Type;
                    var rendered = CSharpTokenParser.Render(args[i], pType);
                    sb.AppendLine($"            var arg{i} = {rendered};");
                }


                var returnTypeDescriptor = manifest.Signature?.ReturnType ?? new TypeDescriptor { Kind = "primitive", Name = "void" };

                var parsed = ParseReturnType(returnTypeDescriptor);

                string invocationArgs = GenerateArgsInvocationBySignature(paramCount);

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
                        effectiveRt = CSharpTypeNameRenderer.RenderTypeName(inferred);
                        needRuntimeCast = parsed.ResultTypeCSharp != effectiveRt;
                    }
                }

                if (!parsed.HasResult)
                {
                    sb.AppendLine($"            try");
                    sb.AppendLine($"            {{");
                    sb.AppendLine($"                var __call = Task.Run(() => {{ {entrypointContainerClass}.{manifest.Entrypoint}({invocationArgs}); }});");
                    sb.AppendLine($"                await __call.WaitAsync(__timeout);");
                    sb.AppendLine($"            }}");
                    sb.AppendLine($"            catch (TimeoutException) {{ Assert.Fail(\"Test execution timed out\"); }}");
                    sb.AppendLine("                __TestMonitor.Inc();");
                    sb.AppendLine("            }");
                    sb.AppendLine("            catch (Exception ex)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                __TestMonitor.AddFailure(\"Sample test '{st.Name}'\", ex.Message);");
                    sb.AppendLine("                throw;");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                }
                else
                {
                    var rt = effectiveRt;
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
                    sb.AppendLine($" RunnerHelpers.AssertCompare(__actual, __expected, \"{comparator}\", \"Sample test '{st.Name}'\");");
                    sb.AppendLine("                __TestMonitor.Inc();");
                    sb.AppendLine("            }");
                    sb.AppendLine("            catch (Exception ex)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                __TestMonitor.AddFailure(\"Sample test '{st.Name}'\", ex.Message);");
                    sb.AppendLine("                throw;");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                }

                sb.AppendLine();
            }
            sb.AppendLine();


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
                    sb.AppendLine("            try");
                    sb.AppendLine("            {");


                    var advTimeout = adv.TimeoutMs > 0 ? adv.TimeoutMs : defaultTimeoutMs;
                    sb.AppendLine($"            var __timeout = TimeSpan.FromMilliseconds({advTimeout}L);");

                    sb.AppendLine($"            try");
                    sb.AppendLine($"            {{");
                    sb.AppendLine($"                var __call = AdvancedTestsContainer.{methodName}();");
                    sb.AppendLine($"                await __call.WaitAsync(__timeout);");
                    sb.AppendLine($"            }}");
                    sb.AppendLine($"            catch (TimeoutException) {{ Assert.Fail(\"Advanced test timed out\"); }}");

                    sb.AppendLine("                __TestMonitor.Inc();");
                    sb.AppendLine("            }");
                    sb.AppendLine("            catch (Exception ex)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                __TestMonitor.AddFailure(\"Advanced test '{methodName}'\", ex.Message);");
                    sb.AppendLine("                throw;");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

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
            var rtName = CSharpTypeNameRenderer.RenderTypeName(td);
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

        private static string ToPascalCase(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;

            if (word.Length == 1)
                return word.ToUpperInvariant();

            return char.ToUpperInvariant(word[0]) + word[1..];
        }
    }
}