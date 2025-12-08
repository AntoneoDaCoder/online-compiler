package com.mems.helpers;

import com.mems.manifest.*;
import com.fasterxml.jackson.databind.JsonNode;
import java.util.List;
import java.util.stream.IntStream;
import java.util.stream.Collectors;

public class JavaWrapper implements ITestWrapper {

    private static final String BOILERPLATE_IMPORTS = """
        import java.util.*;
        import java.util.stream.*;
        import java.io.*;
        import org.junit.*;
        import org.junit.runner.*;
        import org.junit.runners.*;
        import static org.junit.Assert.*;
        import org.junit.internal.*;
        import org.junit.runner.notification.Failure;
        """;

    @Override
    public String generateSource(ManifestDto manifest, String languageCode, String userCode, String entrypointContainerClass, long defaultTimeoutMs) throws Exception {
        if (entrypointContainerClass == null || entrypointContainerClass.isEmpty()) entrypointContainerClass = "SolutionContainer";
        StringBuilder sb = new StringBuilder();

        sb.append(BOILERPLATE_IMPORTS).append("\n");
        sb.append(JavaBaseSourceCode.SOURCE).append("\n\n");

        if (manifest.helpers != null) {
            for (HelpersBlock hb : manifest.helpers) {
                if (hb == null) continue;
                String lang = hb.languageCode == null ? "" : hb.languageCode;
                if (languageCode == null) languageCode = "java";
                if (languageCode.equalsIgnoreCase(lang) && hb.inline != null && !hb.inline.trim().isEmpty()) {
                    sb.append(hb.inline).append("\n\n");
                }
            }
        }

        sb.append("class ").append(entrypointContainerClass).append(" {\n");
        if (userCode != null && !userCode.isEmpty()) {
            String[] lines = userCode.split("\\r?\\n");
            for (String ln : lines) {
                sb.append("    ").append(ln).append("\n");
            }
        }
        sb.append("}\n\n");

        boolean hasAdv = manifest.advancedTests != null && manifest.advancedTests.size() > 0;
        if (hasAdv) {
            sb.append("class AdvancedTestsContainer {\n");
            for (AdvancedTest adv : manifest.advancedTests) {
                if (adv == null) continue;
                String lang = adv.languagecode == null ? "" : adv.languagecode;
                if (!"java".equalsIgnoreCase(lang) && languageCode != null && !"all".equalsIgnoreCase(languageCode)) continue;
                if (adv.source != null && !adv.source.trim().isEmpty()) {
                    sb.append(adv.source).append("\n\n");
                } else {
                    String m = sanitizeMethodName(adv.name);
                    sb.append("    public static void ").append(m).append("() throws Exception { }\n\n");
                }
            }
            sb.append("}\n\n");
        }

        sb.append("public class GeneratedTests {\n\n");
        sb.append("  public GeneratedTests() {}\n\n");

        List<SampleTest> samples = manifest.sampleTests;
        int idx = 0;
        if (samples != null) {
            for (SampleTest st : samples) {
                idx++;
                long timeout = ( st.timeoutMs > 0) ? st.timeoutMs : defaultTimeoutMs;
                String methodName = sanitizeMethodName("Sample_" + st.name + "_" + idx);
                sb.append(String.format("  @Test(timeout = %d)\n", timeout));
                sb.append(String.format("  public void %s() throws Exception {\n", methodName));

                int paramCount = manifest.signature == null ? 0 : (manifest.signature.parameters == null ? 0 : manifest.signature.parameters.size());
                if (st.inputs != null && paramCount > 0 && st.inputs.isArray()) {
                    for (int i = 0; i < paramCount; i++) {
                        ParameterDescriptor p = manifest.signature.parameters.get(i);
                        JsonNode item = st.inputs.get(i);
                        String rendered = JavaTokenParser.render(item, p.type);
                        sb.append("    ").append(renderedDeclaration(p.type, "arg" + i, rendered)).append("\n");
                    }
                } else {
                    for (int i = 0; i < paramCount; i++) {
                        ParameterDescriptor p = manifest.signature.parameters.get(i);
                        sb.append("    ").append(renderedDeclaration(p.type, "arg" + i, getDefaultValueForType(JavaTokenParser.renderTypeName(p.type)))).append("\n");
                    }
                }

                TypeDescriptor rtDescriptor = manifest.signature == null ? null : manifest.signature.returnType;
                String rt = JavaTokenParser.renderTypeName(rtDescriptor);
                boolean hasResult = rtDescriptor != null && !"void".equals(rt);

                if (!hasResult) {
                    sb.append("    try { ");
                    sb.append(entrypointContainerClass).append(".").append(manifest.entrypoint).append("(");
                    sb.append(IntStream.range(0, paramCount).mapToObj(i -> "arg" + i).collect(Collectors.joining(", ")));
                    sb.append("); } catch (Throwable t) { t.printStackTrace(); Assert.fail(\"Test execution threw: \" + t); }\n");
                } else {
                    sb.append("    ").append(rt).append(" __actual = ").append(getDefaultValueForType(rt)).append(";\n");
                    sb.append("    try {\n");
                    sb.append("      __actual = ").append(entrypointContainerClass).append(".").append(manifest.entrypoint).append("(");
                    sb.append(IntStream.range(0, paramCount).mapToObj(i -> "arg" + i).collect(Collectors.joining(", ")));
                    sb.append(");\n");
                    sb.append("    } catch (Throwable t) { t.printStackTrace(); Assert.fail(\"Test execution threw: \" + t); }\n");
                }

                String comparator = st.comparator == null ? "eq" : st.comparator;
                if (st.expected != null && hasResult) {
                    if ("contains".equalsIgnoreCase(comparator)) {
                        TypeDescriptor elem = null;
                        if (rtDescriptor != null && "array".equalsIgnoreCase(rtDescriptor.kind) && rtDescriptor.items != null) {
                            elem = rtDescriptor.items;
                        } else {
                            elem = rtDescriptor;
                        }
                        String expectedRendered = JavaTokenParser.render(st.expected, elem);
                        String expectedTypeName = JavaTokenParser.renderTypeName(elem);
                        sb.append("    ").append(expectedTypeName).append(" __expected = ").append(expectedRendered).append(";\n");
                    } else {
                        String expectedRendered = JavaTokenParser.render(st.expected, rtDescriptor);
                        sb.append("    ").append(rt).append(" __expected = ").append(expectedRendered).append(";\n");
                    }
                } else if (hasResult) {
                    if ("contains".equalsIgnoreCase(comparator)) {
                        TypeDescriptor elem = null;
                        if (rtDescriptor != null && "array".equalsIgnoreCase(rtDescriptor.kind) && rtDescriptor.items != null) {
                            elem = rtDescriptor.items;
                        } else {
                            elem = rtDescriptor;
                        }
                        String expectedTypeName = JavaTokenParser.renderTypeName(elem);
                        sb.append("    ").append(expectedTypeName).append(" __expected = ").append(getDefaultValueForType(expectedTypeName)).append(";\n");
                    } else {
                        sb.append("    ").append(rt).append(" __expected = ").append(getDefaultValueForType(rt)).append(";\n");
                    }
                }

                if (hasResult) {
                    sb.append("    RunnerHelpers.AssertCompare(__actual, __expected, \"").append(comparator).append("\", \"").append(st.name).append("\");\n");
                } else {
                    sb.append("    \n");
                }

                sb.append("  }\n\n");
            }
        }

        if (manifest.advancedTests != null) {
            int aidx = 0;
            for (AdvancedTest adv : manifest.advancedTests) {
                aidx++;
                String sanitizedAdvName = sanitizeMethodName(adv.name);
                long timeout = ( adv.timeoutMs > 0) ? adv.timeoutMs : defaultTimeoutMs;
                sb.append(String.format("  @Test(timeout = %d)\n", timeout));
                String methodName = sanitizeMethodName("Advanced_" + adv.name + "_" + aidx);
                sb.append("  public void ").append(methodName).append("() throws Exception {\n");
                sb.append("    try {\n");
                sb.append("      AdvancedTestsContainer.").append(sanitizedAdvName).append("();\n");
                sb.append("    } catch (Throwable t) { t.printStackTrace(); Assert.fail(\"Advanced test threw: \" + t); }\n");
                sb.append("  }\n\n");
            }
        }

        sb.append("  public static void main(String[] args) {\n");
        sb.append("    try {\n");
        sb.append("      JUnitCore junit = new JUnitCore();\n");
        sb.append("      junit.addListener(new org.junit.internal.TextListener(System.out));\n");
        sb.append("      Result result = junit.run(GeneratedTests.class);\n");
        sb.append("      for (Failure f : result.getFailures()) {\n");
        sb.append("        System.err.println(\"[TEST FAILED] \" + f.getTestHeader());\n");
        sb.append("        System.err.println(f.getMessage());\n");
        sb.append("      }\n");
        sb.append("      int passed = (int)result.getRunCount() - result.getFailureCount() - result.getIgnoreCount();\n");
        sb.append("      System.out.println(\"PassedTests:\" + passed);\n");
        sb.append("      System.out.flush();\n");
        sb.append("      if (result.wasSuccessful()) System.exit(0); else System.exit(1);\n");
        sb.append("    } catch (Throwable t) { t.printStackTrace(); System.exit(2); }\n");
        sb.append("  }\n");

        sb.append("}\n");

        return sb.toString();
    }

    private String sanitizeMethodName(String name) {
        if (name == null) return "m";
        return name.replaceAll("[^A-Za-z0-9_]", "_");
    }

    private String getDefaultValueForType(String rt) {
        if (rt == null) return "null";
        switch (rt) {
            case "int": return "0";
            case "long": return "0L";
            case "double": return "0.0";
            case "boolean": return "false";
            case "void": return "";
            default:
                if (rt.endsWith("[]")) {
                    String inner = rt.substring(0, rt.length()-2);
                    if ("int".equals(inner)) return "new int[0]";
                    if ("long".equals(inner)) return "new long[0]";
                    if ("double".equals(inner)) return "new double[0]";
                    return "null";
                }
                return "null";
        }
    }

    private String renderedDeclaration(TypeDescriptor t, String name, String renderedValue) {
        String typeName = JavaTokenParser.renderTypeName(t);
        return typeName + " " + name + " = " + renderedValue + ";";
    }
}
