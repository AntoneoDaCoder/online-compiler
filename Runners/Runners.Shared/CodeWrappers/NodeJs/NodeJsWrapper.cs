using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using Shared.Helpers;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Runners.Shared.CodeWrappers.NodeJs
{
    public class NodeJsWrapper : ITestWrapper
    {
        private static readonly string[] DefaultBannedModules = new[]
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


        public string GenerateSource(ManifestDto manifest, string userCode, string entrypointContainerClass = "Solution", int defaultTimeoutMs = 2000)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            var sb = new StringBuilder();

            manifest.Entrypoint = manifest.Entrypoint.ToLowerInvariant();

            // Inject base source with banned modules
            var sourceWithBans = NodeBaseSourceCode.Source.Replace("{{BANNED_MODULES}}", JsonConvert.SerializeObject(DefaultBannedModules));

            sb.AppendLine(sourceWithBans);
            sb.AppendLine();

            // inline helpers from manifest for this language
            var languageBlocks = manifest.Helpers;
            foreach (var block in languageBlocks)
            {
                if (!string.IsNullOrWhiteSpace(block.Inline))
                {
                    sb.AppendLine(block.Inline);
                    sb.AppendLine();
                }
            }



            // Wrap user code into a simple exports-like container object
            sb.AppendLine($"const {entrypointContainerClass} = {{}};");
            sb.AppendLine($"(function(exports) {{");
            sb.AppendLine("    // user code starts");
            if (!string.IsNullOrEmpty(userCode))
            {
                var userLines = userCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var l in userLines) sb.AppendLine("    " + l);
            }
            sb.AppendLine("    // user code ends");

            // Auto-export entrypoint
            string entrypoint = manifest.Entrypoint;
            sb.AppendLine($"    if (typeof exports[{JsonConvert.SerializeObject(entrypoint)}] === 'undefined' && typeof {entrypoint} !== 'undefined') {{");
            sb.AppendLine($"        exports[{JsonConvert.SerializeObject(entrypoint)}] = {entrypoint};");
            sb.AppendLine("    }");

            sb.AppendLine($"}})({entrypointContainerClass});");

            // Advanced tests container
            if (manifest.AdvancedTests != null && manifest.AdvancedTests.Count > 0)
            {
                sb.AppendLine("const AdvancedTestsContainer = {};");
                sb.AppendLine("(function(exports) {");
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

            // Runner: generate __runAllTests function
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

                // Render inputs based on signature
                var paramList = manifest.Signature?.Parameters ?? new List<ParameterDescriptor>();
                int paramCount = paramList.Count;

                JToken? inputsToken = null;
                if (st.Inputs.HasValue)
                    inputsToken = JToken.Parse(st.Inputs.Value.GetRawText());

                var args = InputNormalizer.NormalizeInputs(inputsToken, paramCount, st.Name);

                for (int i = 0; i < paramCount; i++)
                {
                    var pType = paramList[i].Type;
                    var rendered = NodeJsTokenParser.Render(args[i], pType);
                    sb.AppendLine($"            const arg{i} = {rendered};");
                }

                var invocationArgs = GenerateArgsInvocationBySignature(paramCount);
                sb.AppendLine($"            const __timeoutMs = {timeoutMsExpr};");
                sb.AppendLine("            try {");
                sb.AppendLine("                const __callPromise = (async () => {");
                sb.AppendLine($"                    const fn = {entrypointContainerClass}[{JsonConvert.SerializeObject(manifest.Entrypoint)}];");
                sb.AppendLine("                    if (typeof fn !== 'function') throw new Error('Entrypoint not found');");
                sb.AppendLine($"                    return fn({invocationArgs});");
                sb.AppendLine("                })();");
                sb.AppendLine("                const __result = await Promise.race([");
                sb.AppendLine("                    __callPromise,");
                sb.AppendLine("                    new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), __timeoutMs))");
                sb.AppendLine("                ]);");

                if (st.Expected.HasValue)
                {
                    var expectedToken = JToken.Parse(st.Expected.Value.GetRawText());
                    var expectedRendered = NodeJsTokenParser.Render(expectedToken, manifest.Signature?.ReturnType);
                    sb.AppendLine($"                const __expected = {expectedRendered};");
                }
                else
                {
                    sb.AppendLine("                const __expected = undefined;");
                }

                var comparator = string.IsNullOrWhiteSpace(st.Comparator) ? "eq" : st.Comparator;
                sb.AppendLine($"                const __cmp = RunnerHelpers.assertCompare(__result, __expected, {JsonConvert.SerializeObject(comparator)}, {JsonConvert.SerializeObject($"Sample test '{st.Name}'")});");
                sb.AppendLine("                if (__cmp && __cmp.ok) { __TestMonitor.inc(); } else {");
                sb.AppendLine("                    __hadFailures = true;");
                sb.AppendLine("                    console.log('FailedTest:' + " + JsonConvert.SerializeObject(testName) + " + ':' + (__cmp && __cmp.reason ? __cmp.reason : 'Comparator failed'));");
                sb.AppendLine("                }");



                sb.AppendLine("            } catch (err) {");
                sb.AppendLine("                if (err && err.message === 'timeout') {");
                sb.AppendLine("                    __hadFailures = true;");
                sb.AppendLine($"                    console.log('FailedTest:' + {JsonConvert.SerializeObject(testName)} + ':Test execution timed out');");
                sb.AppendLine("                } else {");
                sb.AppendLine("                    __hadFailures = true;");
                sb.AppendLine($"                    console.log('FailedTest:' + {JsonConvert.SerializeObject(testName)} + ':Error during execution: ' + __formatErrorForOutput(err));");
                sb.AppendLine("                }");
                sb.AppendLine("            }");



                sb.AppendLine("        } catch (outerErr) {");
                sb.AppendLine("            __hadFailures = true;");
                sb.AppendLine("            console.log('FailedTest:' + " + JsonConvert.SerializeObject(testName) + " + ':Setup error: ' + (outerErr && outerErr.message ? outerErr.message : String(outerErr)));");
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
                    sb.AppendLine($"            const __advTimeoutMs = {advTimeout};");
                    sb.AppendLine("            try {");
                    sb.AppendLine($"                const advFn = AdvancedTestsContainer[{JsonConvert.SerializeObject(methodName)}];");
                    sb.AppendLine("                if (typeof advFn !== 'function') throw new Error('Advanced test not found');");
                    sb.AppendLine("                const __advPromise = (async () => advFn())();");
                    sb.AppendLine("                await Promise.race([__advPromise, new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), __advTimeoutMs))]);");
                    sb.AppendLine("                __TestMonitor.inc();");
                    sb.AppendLine("            } catch (err) {");
                    sb.AppendLine("                if (err && err.message === 'timeout') {");
                    sb.AppendLine("                    __hadFailures = true;");
                    sb.AppendLine("                    console.log('FailedTest:' + " + JsonConvert.SerializeObject(testName) + " + ':Advanced test timed out');");
                    sb.AppendLine("                } else {");
                    sb.AppendLine("                    __hadFailures = true;");
                    sb.AppendLine("                    console.log('FailedTest:' + " + JsonConvert.SerializeObject(testName) + " + ':Error during execution: ' + __formatErrorForOutput(err));");
                    sb.AppendLine("                }");
                    sb.AppendLine("            }");

                    sb.AppendLine("        } catch (outer) {");
                    sb.AppendLine("            __hadFailures = true;");
                    sb.AppendLine("            console.log('FailedTest:' + " + JsonConvert.SerializeObject(testName) + " + ':Setup error: ' + String(outer));");
                    sb.AppendLine("        }");

                }
            }

            sb.AppendLine("    } finally {");
            sb.AppendLine("        try {");
            sb.AppendLine("            console.log('PassedTests:' + __TestMonitor.get());");
            sb.AppendLine("            try { if (typeof process !== 'undefined' && process) process.exitCode = __hadFailures ? 1 : 0; } catch(e) {}");
            sb.AppendLine("        } catch (e) {}");
            sb.AppendLine("    }}"); // end __runAllTests

            sb.AppendLine();
            sb.AppendLine("__runAllTests().catch(function(err) {");
            sb.AppendLine("    try { console.log('FailedTest:Runner:Fatal error: ' + (err && err.message ? err.message : String(err))); } catch (e) {}");
            sb.AppendLine("console.log('PassedTests:' + __TestMonitor.get());\r\ntry {\r\n    if (typeof process !== 'undefined' && process) {\r\n " +
                "       // если были ошибки, выставляем exit code 1, иначе 0\r\n        process.exitCode = __hadFailures ? 1 : 0;\r\n    }\r\n} catch(e) {}\r\n");
            sb.AppendLine("})");

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
