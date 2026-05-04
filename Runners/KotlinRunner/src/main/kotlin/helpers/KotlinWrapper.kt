package helpers

import manifest.*

object KotlinWrapper : ITestWrapper {

    private const val BOILERPLATE_IMPORTS = """
        import kotlin.*
        import kotlin.collections.*
        import org.junit.*
        import org.junit.runner.*
        import org.junit.runners.*
        import org.junit.Assert
        import java.util.Locale
        import org.junit.runner.notification.Failure
        """

    @Throws(Exception::class)
    override fun generateSource(
        manifest: ManifestDto,
        languageCodeIn: String?,
        userCode: String?,
        entrypointContainerClassIn: String?,
        defaultTimeoutMs: Long
    ): String {
        var languageCode = languageCodeIn
        var entrypointContainerClass = entrypointContainerClassIn
        if (entrypointContainerClass.isNullOrEmpty()) entrypointContainerClass = "SolutionContainer"

        val sb = StringBuilder()
        sb.append(BOILERPLATE_IMPORTS).append("\n")
        sb.append(KotlinBaseSourceCode.SOURCE).append("\n\n")

        // inline helpers (filter by languageCode)
        // вместо: if (manifest.helpers != null) { for (hb in manifest.helpers) { ... } }
        val helpers = manifest.helpers ?: emptyList()
        for (hb in helpers) {
            val lang = hb.languageCode ?: ""
            if (languageCode == null) languageCode = "kotlin"
            if (languageCode.equals(lang, ignoreCase = true) && !hb.inline.isNullOrBlank()) {
                sb.append(hb.inline).append("\n\n")
            }
        }


        // user code wrapped into an object so we can call functions as ObjectName.func(...)
        sb.append("object ").append(entrypointContainerClass).append(" {\n")
        if (!userCode.isNullOrEmpty()) {
            val lines = userCode.split(Regex("\\r?\\n"))
            for (ln in lines) {
                sb.append("    ").append(ln).append("\n")
            }
        }
        sb.append("}\n\n")

        // Advanced tests container (object)
        val advList = manifest.advancedTests ?: emptyList()
        val hasAdv = advList.isNotEmpty()
        if (hasAdv) {
            sb.append("object AdvancedTestsContainer {\n")
            for (adv in advList) {
                val lang = adv.languagecode ?: ""
                if (!languageCode.isNullOrEmpty() && !languageCode.equals("all", ignoreCase = true) && !languageCode.equals(lang, ignoreCase = true)) {
                    continue
                }
                if (!adv.source.isNullOrBlank()) {
                    sb.append(adv.source).append("\n\n")
                } else {
                    val m = sanitizeMethodName(adv.name)
                    sb.append("    fun ").append(m).append("() { }\n\n")
                }
            }
            sb.append("}\n\n")
        }


        // Generated tests class
        sb.append("class GeneratedTests {\n\n")
        // no-arg constructor not needed in Kotlin

        val samples = manifest.sampleTests
        var idx = 0
        if (samples != null) {
            for (st in samples) {
                idx++
                val timeout = if (st.timeoutMs > 0) st.timeoutMs else defaultTimeoutMs
                val methodName = sanitizeMethodName("Sample_${st.name}_$idx")
                sb.append("  @Test(timeout = $timeout)\n")
                sb.append("  fun ").append(methodName).append("() {\n")

                val paramCount = manifest.signature.parameters.size

                // inputs может быть null, массивом или одиночным значением
                val inputsNode = st.inputs
                val arrNode = if (inputsNode != null && inputsNode.isArray)
                    inputsNode as com.fasterxml.jackson.databind.node.ArrayNode
                else null

                if (paramCount > 0) {
                    if (paramCount == 1) {
                        // один параметр — если inputs массив, берём первый элемент, иначе используем сам inputs
                        val nodeForParam = when {
                            arrNode != null -> if (arrNode.size() > 0) arrNode.get(0) else com.fasterxml.jackson.databind.node.NullNode.instance
                            inputsNode != null -> inputsNode
                            else -> com.fasterxml.jackson.databind.node.NullNode.instance
                        }
                        val p = manifest.signature.parameters[0]
                        val rendered = KotlinTokenParser.render(nodeForParam, p.type)
                        sb.append("    ").append(renderedDeclaration(p.type, "arg0", rendered)).append("\n")
                    } else {
                        // несколько параметров — ожидаем массив; если не массив, используем inputs как первый параметр и дефолты для остальных
                        if (arrNode != null) {
                            for (i in 0 until paramCount) {
                                val p = manifest.signature.parameters[i]
                                val itemNode = if (i < arrNode.size()) arrNode.get(i) else com.fasterxml.jackson.databind.node.NullNode.instance
                                val rendered = KotlinTokenParser.render(itemNode, p.type)
                                sb.append("    ").append(renderedDeclaration(p.type, "arg$i", rendered)).append("\n")
                            }
                        } else {
                            // inputs не массив — попытка использовать как первый параметр, остальные — дефолты
                            val firstNode = inputsNode ?: com.fasterxml.jackson.databind.node.NullNode.instance
                            val p0 = manifest.signature.parameters[0]
                            val rendered0 = KotlinTokenParser.render(firstNode, p0.type)
                            sb.append("    ").append(renderedDeclaration(p0.type, "arg0", rendered0)).append("\n")
                            for (i in 1 until paramCount) {
                                val p = manifest.signature.parameters[i]
                                val defaultVal = getDefaultValueForType(KotlinTokenParser.renderTypeName(p.type))
                                sb.append("    ").append(renderedDeclarationForDefault(p.type, "arg$i", defaultVal)).append("\n")
                            }
                        }
                    }
                } // else: paramCount == 0 -> ничего не объявляем

                val rtDescriptor = manifest.signature.returnType
                val rt = KotlinTokenParser.renderTypeName(rtDescriptor)
                val hasResult =  rt != "Unit"

                val argsList = (0 until paramCount).joinToString(", ") { "arg$it" }

                if (!hasResult) {
                    sb.append("    try {\n")
                    sb.append("      ").append(entrypointContainerClass).append(".").append(manifest.entrypoint?.lowercase())
                        .append("(").append(argsList).append(")\n")
                    sb.append("    } catch (t: Throwable) { t.printStackTrace();  throw AssertionError(\"Test execution threw: ${'$'}t\", t)\n}\n")
                } else {
                    val defaultVal = getDefaultValueForType(rt)
                    if (defaultVal == "null") {
                        sb.append("    var __actual: ").append(rt).append("? = null\n")
                    } else {
                        sb.append("    var __actual: ").append(rt).append(" = ").append(defaultVal).append("\n")
                    }
                    sb.append("    try {\n")
                    sb.append("      __actual = ").append(entrypointContainerClass).append(".").append(manifest.entrypoint?.lowercase())
                        .append("(").append(argsList).append(")\n")
                    sb.append("    } catch (t: Throwable) { t.printStackTrace();  throw AssertionError(\"Test execution threw: ${'$'}t\", t)\n}\n")
                }

                val comparator = st.comparator ?: "eq"
                if (st.expected != null && hasResult) {
                    if (comparator.equals("contains", ignoreCase = true)) {
                        val elem = if (rtDescriptor.kind.equals("array", ignoreCase = true) && rtDescriptor.items != null) rtDescriptor.items else rtDescriptor
                        val expectedRendered = KotlinTokenParser.render(st.expected, elem)
                        val expectedTypeName = KotlinTokenParser.renderTypeName(elem)
                        val def = if (getDefaultValueForType(expectedTypeName) == "null") "$expectedTypeName?" else expectedTypeName
                        sb.append("    var __expected: ").append(def).append(" = ").append(expectedRendered).append("\n")
                    } else {
                        val expectedRendered = KotlinTokenParser.render(st.expected, rtDescriptor)
                        val defType = rt
                        if (getDefaultValueForType(defType) == "null") sb.append("    var __expected: ").append(defType).append("? = ").append(expectedRendered).append("\n")
                        else sb.append("    var __expected: ").append(defType).append(" = ").append(expectedRendered).append("\n")
                    }
                } else if (hasResult) {
                    if (comparator.equals("contains", ignoreCase = true)) {
                        val elem = if (rtDescriptor.kind.equals("array", ignoreCase = true) && rtDescriptor.items != null) rtDescriptor.items else rtDescriptor
                        val expectedTypeName = KotlinTokenParser.renderTypeName(elem)
                        val defaultValForElem = getDefaultValueForType(expectedTypeName)
                        if (defaultValForElem == "null") sb.append("    var __expected: ").append(expectedTypeName).append("? = null\n")
                        else sb.append("    var __expected: ").append(expectedTypeName).append(" = ").append(defaultValForElem).append("\n")
                    } else {
                        val defaultValForRt = getDefaultValueForType(rt)
                        if (defaultValForRt == "null") sb.append("    var __expected: ").append(rt).append("? = null\n")
                        else sb.append("    var __expected: ").append(rt).append(" = ").append(defaultValForRt).append("\n")
                    }
                }

                if (hasResult) {
                    val stNameEsc = st.name?.replace("\"", "\\\"") ?: ""
                    sb.append("    RunnerHelpers.AssertCompare(__actual, __expected, \"").append(comparator).append("\", \"").append(stNameEsc).append("\")\n")
                } else {
                    sb.append("    \n")
                }

                sb.append("  }\n\n")
            }
        }


        // Advanced tests wrapper methods calling AdvancedTestsContainer functions
        val list = manifest.advancedTests ?: emptyList()
        var aidx = 0
        for (adv in list) {
            aidx++
            val sanitizedAdvName = sanitizeMethodName(adv.name)
            val timeout = if (adv.timeoutMs > 0L) adv.timeoutMs  else defaultTimeoutMs
            sb.append("  @Test(timeout = $timeout)\n")
            val methodName = sanitizeMethodName("Advanced_${adv.name}_$aidx")
            sb.append("  fun ").append(methodName).append("() {\n")
            sb.append("    try {\n")
            sb.append("      AdvancedTestsContainer.").append(sanitizedAdvName).append("()\n")
            sb.append("    } catch (t: Throwable) { t.printStackTrace();  throw AssertionError(\"Advanced test threw: ${'$'}t\", t)\n}\n")
            sb.append("  }\n\n")
        }


        // main function as companion object
        sb.append("  companion object {\n")
        sb.append("    @JvmStatic\n")
        sb.append("    fun main(args: Array<String>) {\n")
        sb.append("      try {\n")
        sb.append("        val junit = JUnitCore()\n")
        sb.append("        junit.addListener(org.junit.internal.TextListener(System.out))\n")
        sb.append("        val result = junit.run(GeneratedTests::class.java)\n")
        sb.append("        for (f in result.failures) {\n")
        sb.append("          System.err.println(\"[TEST FAILED] \" + f.testHeader)\n")
        sb.append("          System.err.println(f.message)\n")
        sb.append("        }\n")
        sb.append("        val total = result.runCount - result.ignoreCount\n")
        sb.append("        val passed = total - result.failureCount\n")
        sb.append("        println(\"Passed tests:${'$'}passed/${'$'}total\");")
        sb.append("        System.out.flush()\n")
        sb.append("        if (result.wasSuccessful()) kotlin.system.exitProcess(0) else kotlin.system.exitProcess(1)\n")
        sb.append("      } catch (t: Throwable) { t.printStackTrace(); kotlin.system.exitProcess(2) }\n")
        sb.append("    }\n")
        sb.append("  }\n") // end companion

        sb.append("}\n") // end GeneratedTests

        return sb.toString()
    }

    private fun sanitizeMethodName(name: String?): String {
        if (name == null) return "m"
        return name.replace(Regex("[^A-Za-z0-9_]"), "_")
    }

    private fun getDefaultValueForType(rt: String?): String {
        if (rt == null) return "null"
        return when (rt) {
            "Int" -> "0"
            "Long" -> "0L"
            "Double" -> "0.0"
            "Boolean" -> "false"
            "Unit" -> "" // treated as no-result
            "IntArray" -> "intArrayOf()"
            "LongArray" -> "longArrayOf()"
            "DoubleArray" -> "doubleArrayOf()"
            else -> {
                // For other arrays like Array<T> or classes/strings - return null (declare nullable)
                "null"
            }
        }
    }

    private fun renderedDeclaration(t: TypeDescriptor?, name: String, renderedValue: String): String {
        val typeName = KotlinTokenParser.renderTypeName(t)
        return if (renderedValue == "null") {
            "var $name: $typeName? = null"
        } else {
            "var $name: $typeName = $renderedValue"
        }
    }

    private fun renderedDeclarationForDefault(t: TypeDescriptor?, name: String, defaultValue: String): String {
        val typeName = KotlinTokenParser.renderTypeName(t)
        return if (defaultValue == "null") {
            "var $name: $typeName? = null"
        } else {
            "var $name: $typeName = $defaultValue"
        }
    }
}
