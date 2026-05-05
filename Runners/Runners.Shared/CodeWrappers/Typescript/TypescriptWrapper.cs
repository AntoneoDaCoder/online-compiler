using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using System.Text;

namespace Runners.Shared.CodeWrappers.Typescript
{
    public class TypescriptWrapper : ITestWrapper
    {
        public static readonly string[] DefaultBannedModules = new[]
        {
            "fs", "fs/promises", "path",
            "net", "dgram", "tls", "http", "https", "http2",
            "child_process", "cluster", "repl",
            "vm", "eval", "async_hooks",
            "zlib", "stream", "crypto",
            "module", "os", "perf_hooks",
            "inspector", "dns", "readline", "tty",
            "events", "util", "buffer", "console"
        };

        private const int DefaultTimeoutMs = 2000;
        public string GenerateSource(ManifestDto manifest, string userCode, string entrypointContainerName = "SolutionContainer", int defaultTimeoutMs = DefaultTimeoutMs)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            var sb = new StringBuilder();

            manifest.Entrypoint = manifest.Entrypoint.ToLowerInvariant();

            // inject base source with banned modules
            var sourceWithBans = TypescriptBaseSourceCode.Source.Replace("{{BANNED_MODULES}}", JsonConvert.SerializeObject(DefaultBannedModules));
            sb.AppendLine(sourceWithBans);
            sb.AppendLine();

            // declare globals for tsc environments without @types/node
            sb.AppendLine("// Declarations for Node globals");
            sb.AppendLine("declare var process: any;");
            sb.AppendLine("declare var require: any;");
            sb.AppendLine();

            // include manifest helpers inline for this language (languageCode 'typescript' or 'ts' allowed)
            var languageBlocks = manifest.Helpers ?? new List<HelpersBlock>();
            foreach (var block in languageBlocks)
            {
                if (!string.IsNullOrWhiteSpace(block.Inline))
                {
                    sb.AppendLine(block.Inline);
                    sb.AppendLine();
                }
            }

            // user code container
            sb.AppendLine($"const {entrypointContainerName}: any = {{}};");
            sb.AppendLine($"(function(exports: any) {{");
            if (!string.IsNullOrEmpty(userCode))
            {
                var userLines = userCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var l in userLines) sb.AppendLine("    " + l);
            }

            sb.AppendLine($"    if (typeof exports[{JsonConvert.SerializeObject(manifest.Entrypoint)}] === 'undefined'" +
                $" && typeof {manifest.Entrypoint} !== 'undefined') {{");
            sb.AppendLine($"        exports[{JsonConvert.SerializeObject(manifest.Entrypoint)}] = {manifest.Entrypoint};");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}})({entrypointContainerName});");

            // advanced tests container
            if (manifest.AdvancedTests != null && manifest.AdvancedTests.Count > 0)
            {
                sb.AppendLine("const AdvancedTestsContainer: any = {};");
                sb.AppendLine("(function(exports: any) {");
                foreach (var adv in manifest.AdvancedTests)
                {
                    var advSrc = adv.Source ?? "";
                    var advLines = advSrc.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    foreach (var l in advLines) sb.AppendLine("    " + l);
                    sb.AppendLine();
                }
                sb.AppendLine("})(AdvancedTestsContainer);");
                sb.AppendLine();
            }

            // runner function
            sb.AppendLine("async function __runAllTests() {");
            sb.AppendLine("    try {");
            sb.AppendLine("        // Sample tests");

            int idx = 0;
            foreach (var st in manifest.SampleTests ?? Enumerable.Empty<SampleTest>())
            {
                idx++;
                var testName = SanitizeTestName($"Sample_{st.Name}_{idx}");
                var timeoutMsExpr = st.TimeoutMs > 0 ? st.TimeoutMs : defaultTimeoutMs;
                sb.AppendLine($"        // === {testName} ===");
                sb.AppendLine("        try {");

                // render inputs (explicit any typing)
                var paramList = manifest.Signature?.Parameters ?? new List<ParameterDescriptor>();
                int paramCount = paramList.Count;

                JToken? inputsToken = null;
                if (st.Inputs.HasValue)
                    inputsToken = JToken.Parse(st.Inputs.Value.GetRawText());

                var args = InputNormalizer.NormalizeInputs(inputsToken, paramCount, st.Name);

                for (int i = 0; i < paramCount; i++)
                {
                    var pType = paramList[i].Type;
                    var rendered = TypeScriptTokenParser.Render(args[i], pType);
                    sb.AppendLine($"            const arg{i}: any = {rendered};");
                }


                var invocationArgs = GenerateArgsInvocationBySignature(paramCount);
                sb.AppendLine($"            const __timeoutMs: number = {timeoutMsExpr};");
                sb.AppendLine("            try {");
                sb.AppendLine("                const __callPromise: Promise<any> = (async () => {");
                sb.AppendLine($"                    const fn: any = {entrypointContainerName}[{JsonConvert.SerializeObject(manifest.Entrypoint)}];");
                sb.AppendLine("                    if (typeof fn !== 'function') throw new Error('Entrypoint not found');");
                sb.AppendLine($"                    return fn({invocationArgs});");
                sb.AppendLine("                })();");
                sb.AppendLine("                const __result: any = await Promise.race([");
                sb.AppendLine("                    __callPromise,");
                sb.AppendLine("                    new Promise<any>((_, reject) => setTimeout(() => reject(new Error('timeout')), __timeoutMs))");
                sb.AppendLine("                ]);");

                if (st.Expected.HasValue)
                {
                    var expectedToken = JToken.Parse(st.Expected.Value.GetRawText());
                    var expectedRendered = TypeScriptTokenParser.Render(expectedToken, manifest.Signature?.ReturnType);
                    sb.AppendLine($"                const __expected: any = {expectedRendered};");
                }
                else
                {
                    sb.AppendLine("                const __expected: any = undefined;");
                }

                var comparator = string.IsNullOrWhiteSpace(st.Comparator) ? "eq" : st.Comparator;
                sb.AppendLine($"                const __cmp = RunnerHelpers.assertCompare(__result, __expected, {JsonConvert.SerializeObject(comparator)}, {JsonConvert.SerializeObject($"Sample test '{st.Name}'")});");
                sb.AppendLine("                if (__cmp && __cmp.ok) { __TestMonitor.inc(); } else {");
                sb.AppendLine("                    __hadFailures = true;");
                sb.AppendLine($"                    __TestMonitor.addFailure({JsonConvert.SerializeObject(testName)}, (__cmp && __cmp.reason ? __cmp.reason : 'Comparator failed'));");
                sb.AppendLine("                }");

                // error handling with safe .message usage
                sb.AppendLine("            } catch (err) {");
                sb.AppendLine("                if (err && (err as any).message === 'timeout') {");
                sb.AppendLine("                    __hadFailures = true;");
                sb.AppendLine($"                    __TestMonitor.addFailure({JsonConvert.SerializeObject(testName)}, 'Test execution timed out');");
                sb.AppendLine("                } else {");
                sb.AppendLine("                    __hadFailures = true;");
                sb.AppendLine($"                    __TestMonitor.addFailure({JsonConvert.SerializeObject(testName)}, __formatErrorForOutput(err));");
                sb.AppendLine("                }");
                sb.AppendLine("            }");

                sb.AppendLine("        } catch (outerErr) {");
                sb.AppendLine("            __hadFailures = true;");
                sb.AppendLine($"           __TestMonitor.addFailure({JsonConvert.SerializeObject(testName)}, (outerErr && (outerErr as any).message ? (outerErr as any).message : String(outerErr)));");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // Advanced tests
            if (manifest.AdvancedTests != null && manifest.AdvancedTests.Count > 0)
            {
                int advIdx = 0;
                foreach (var adv in manifest.AdvancedTests)
                {
                    advIdx++;
                    var methodName = adv.Name;
                    var testName = SanitizeTestName($"Advanced_{methodName}_{advIdx}");
                    var advTimeout = adv.TimeoutMs > 0 ? adv.TimeoutMs : defaultTimeoutMs;
                    sb.AppendLine($"        // === {testName} ===");
                    sb.AppendLine("        try {");
                    sb.AppendLine($"            const __advTimeoutMs: number = {advTimeout};");
                    sb.AppendLine("            try {");
                    sb.AppendLine($"                const advFn: any = AdvancedTestsContainer[{JsonConvert.SerializeObject(methodName)}];");
                    sb.AppendLine("                if (typeof advFn !== 'function') throw new Error('Advanced test not found');");
                    sb.AppendLine("                const __advPromise: Promise<any> = (async () => advFn())();");
                    sb.AppendLine("                await Promise.race([__advPromise, new Promise<any>((_, reject) => setTimeout(() => reject(new Error('timeout')), __advTimeoutMs))]);");
                    sb.AppendLine("                __TestMonitor.inc();");

                    sb.AppendLine("            } catch (err) {");
                    sb.AppendLine("                if (err && (err as any).message === 'timeout') {");
                    sb.AppendLine("                    __hadFailures = true;");
                    sb.AppendLine($"                    __TestMonitor.addFailure({JsonConvert.SerializeObject(testName)}, 'Test execution timed out');");
                    sb.AppendLine("                } else {");
                    sb.AppendLine("                    __hadFailures = true;");
                    sb.AppendLine($"                    __TestMonitor.addFailure({JsonConvert.SerializeObject(testName)}, __formatErrorForOutput(err));");
                    sb.AppendLine("                }");
                    sb.AppendLine("            }");

                    sb.AppendLine("        } catch (outerErr) {");
                    sb.AppendLine("            __hadFailures = true;");
                    sb.AppendLine($"           __TestMonitor.addFailure({JsonConvert.SerializeObject(testName)}, (outerErr && (outerErr as any).message ? (outerErr as any).message : String(outerErr)));");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }

            // finalize
            var totalTests = (manifest.SampleTests?.Count ?? 0) + (manifest.AdvancedTests?.Count ?? 0);

            sb.AppendLine("    } finally {");
            sb.AppendLine("        try {");
            sb.AppendLine($"            __emitReport({totalTests});");
            sb.AppendLine("            try { if (typeof process !== 'undefined' && process) process.exitCode = (__TestMonitor.getFailed().length > 0 || __hadFailures) ? 1 : 0; } catch(e) {}");
            sb.AppendLine("            try { setTimeout(() => { try { process.exit(process.exitCode || 0); } catch(e){} }, 400); } catch(e) {}");
            sb.AppendLine("        } catch (e) {}");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            sb.AppendLine();
            sb.AppendLine("__runAllTests().catch(function(err) {");
            sb.AppendLine("    try {");
            sb.AppendLine("        __hadFailures = true;");
            sb.AppendLine("        __TestMonitor.addFailure('Runner', 'Fatal error: ' + (err && err.message ? err.message : String(err)));");
            sb.AppendLine($"        __emitReport({totalTests});");
            sb.AppendLine("    } catch (e) {}");
            sb.AppendLine("    try { if (typeof process !== 'undefined' && process) process.exitCode = 1; } catch(e) {}");
            sb.AppendLine("    try { setTimeout(() => { try { process.exit(process.exitCode || 1); } catch(e){} }, 400); } catch(e) {}");
            sb.AppendLine("});");

            return sb.ToString();
        }

        private string GenerateArgsInvocationBySignature(int paramCount)
        {
            if (paramCount == 0) return "";
            if (paramCount == 1) return "arg0";
            return string.Join(", ", Enumerable.Range(0, paramCount).Select(i => $"arg{i}"));
        }

        private string SanitizeTestName(string name)
        {
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }
    }
}
