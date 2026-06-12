object KotlinBaseSourceCode {
    const val SOURCE =
        """
        import kotlin.*
        import kotlin.collections.*
        import org.junit.*
        import org.junit.runner.*
        import org.junit.runners.*
        import org.junit.Assert
        import java.util.Locale
        import org.junit.runner.notification.Failure
            
        object RunnerHelpers {
            private fun asObjectArray(value: Any?): Array<Any?>? {
                if (value == null) return null
                if (value is String) return null
                if (value is Array<*>) return value as Array<Any?>
                if (value is IntArray) {
                    return Array(value.size) { i -> value[i] }
                }
                if (value is LongArray) {
                    return Array(value.size) { i -> value[i] }
                }
                if (value is DoubleArray) {
                    return Array(value.size) { i -> value[i] }
                }
                if (value is Iterable<*>) {
                    val out = ArrayList<Any?>()
                    for (o in value) out.add(o)
                    return out.toTypedArray()
                }
                return null
            }

            @JvmStatic
            fun AssertCompare(actual: Any?, expected: Any?, comparator: String?, testName: String?) {
                val comp = comparator?.lowercase(Locale.ROOT) ?: "eq"
                val tname = testName ?: ""
                when (comp) {
                    "eq" -> {
                        Assert.assertEquals(tname, expected, actual)
                        return
                    }

                    "neq" -> {
                        Assert.assertNotEquals(tname, expected, actual)
                        return
                    }

                    "seq_eq" -> {
                        val a = asObjectArray(actual)
                        val e = asObjectArray(expected)
                        if (a == null || e == null) {
                            Assert.fail("${'$'}tname: seq_eq requires both actual and expected to be collections")
                        }
                        Assert.assertArrayEquals(tname, e, a)
                        return
                    }

                    "seq_eq_sorted" -> {
                        val a = asObjectArray(actual)
                        val e = asObjectArray(expected)
                        if (a == null || e == null) {
                            Assert.fail("${'$'}tname:  seq_eq_sorted requires both actual and expected to be collections")
                        }
                        val cntA = countMultiset(a!!)
                        val cntE = countMultiset(e!!)
                        Assert.assertEquals(tname, cntE, cntA)
                        return
                    }

                    "contains" -> {
                        val a = asObjectArray(actual)
                        if (a != null) {
                            val ok = a.any { it == expected }
                            Assert.assertTrue("${'$'}tname: contains failed", ok)
                            return
                        }
                        val e = asObjectArray(expected)
                        if (e != null) {
                            val ok = e.any { it == actual }
                            Assert.assertTrue("${'$'}tname: contains failed (reverse)", ok)
                            return
                        }
                        Assert.fail("${'$'}tname:  contains requires one side to be collection")
                        return
                    }

                    "lt", "gt", "le", "ge" -> {
                        if (actual == null || expected == null) {
                            Assert.fail("${'$'}tname:  numeric comparator requires non-null operands")
                        }
                        try {
                            val a = actual.toString().toDouble()
                            val e = expected.toString().toDouble()
                            when (comp) {
                                "lt" -> {
                                    Assert.assertTrue(tname, a < e); return
                                }

                                "gt" -> {
                                    Assert.assertTrue(tname, a > e); return
                                }

                                "le" -> {
                                    Assert.assertTrue(tname, a <= e); return
                                }

                                "ge" -> {
                                    Assert.assertTrue(tname, a >= e); return
                                }
                            }
                        } catch (ex: Exception) {
                            Assert.fail("${'$'}tname:  numeric comparator failed to convert operands to numbers")
                        }
                        return
                    }

                    else -> throw UnsupportedOperationException("Comparator '${'$'}comp' not supported")
                }
            }

            private fun countMultiset(arr: Array<Any?>): Map<Any?, Int> {
                val m = HashMap<Any?, Int>()
                for (o in arr) {
                    val key: Any? = o
                    val c = m[key]
                    if (c == null) m[key] = 1 else m[key] = c + 1
                }
                return m
            }
        }

        data class __FailedTest(val name: String, val reason: String)
        data class __TestReport(val totalTests: Int, val passedTests: Int, val failedTests: List<__FailedTest>)

        object __ReportHelpers {
            private fun escapeJson(s: String): String {
                val out = StringBuilder(s.length + 16)
                for (ch in s) {
                    when (ch) {
                        '\\' -> out.append("\\\\")
                        '"' -> out.append("\\\"")
                        '\b' -> out.append("\\b")
                        '\u000C' -> out.append("\\f")
                        '\n' -> out.append("\\n")
                        '\r' -> out.append("\\r")
                        '\t' -> out.append("\\t")
                        else -> {
                            if (ch < ' ') out.append("\\u%04x".format(ch.code))
                            else out.append(ch)
                        }
                    }
                }
                return out.toString()
            }

            private fun q(s: String): String = "\"" + escapeJson(s) + "\""

            @JvmStatic
            fun emitReport(totalTests: Int, result: org.junit.runner.Result) {
                val passed = (result.runCount - result.failureCount - result.ignoreCount).coerceAtLeast(0)

                val sb = StringBuilder()
                sb.append("{")
                sb.append("\"totalTests\":").append(totalTests).append(",")
                sb.append("\"passedTests\":").append(passed).append(",")
                sb.append("\"failedTests\":[")
                result.failures.forEachIndexed { index, f ->
                    if (index > 0) sb.append(",")

                    val name = try {
                        f.description?.methodName
                    } catch (_: Throwable) {
                        null
                    } ?: try {
                        f.testHeader
                    } catch (_: Throwable) {
                        null
                    } ?: "unknown"

                    val reason = try {
                        f.exception?.message
                    } catch (_: Throwable) {
                        null
                    } ?: try {
                        f.message
                    } catch (_: Throwable) {
                        null
                    } ?: try {
                        f.exception?.toString()
                    } catch (_: Throwable) {
                        null
                    } ?: try {
                        f.toString()
                    } catch (_: Throwable) {
                        null
                    } ?: "Unknown error"

                    val finalReason = if (reason.isBlank()) "Unknown error" else reason

                    sb.append("{")
                    sb.append("\"name\":").append(q(name)).append(",")
                    sb.append("\"reason\":").append(q(finalReason))
                    sb.append("}")
                }
                sb.append("]}")

                println("__TEST_REPORT_BEGIN__")
                println(sb.toString())
                println("__TEST_REPORT_END__")
                System.out.flush()
            }
            
            @JvmStatic
            fun emitFatalReport(totalTests: Int, t: Throwable) {
                val reason = (t.message ?: t.toString())
                println("__TEST_REPORT_BEGIN__")
                println(
                    "{\"totalTests\":${'$'}totalTests," +
                    "\"passedTests\":0," +
                    "\"failedTests\":[{\"name\":\"Runner\",\"reason\":" + q(if (reason.isBlank()) "Unknown error" else reason) + "}]}"

                )
                println("__TEST_REPORT_END__")
                System.out.flush()
            }
        }
        """
}