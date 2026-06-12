package com.mems.helpers;

public final class JavaBaseSourceCode {
    public static final String SOURCE = """
        import java.util.*;
        import java.util.stream.*;
        import org.junit.Assert;
        import org.junit.runner.Result;
        import org.junit.runner.notification.Failure;
        import com.fasterxml.jackson.databind.ObjectMapper;

        final class RunnerHelpers {
            private RunnerHelpers() {}

            private static Object[] asObjectArray(Object value) {
                if (value == null) return null;
                if (value instanceof String) return null;
                if (value instanceof Object[]) return (Object[]) value;
                if (value instanceof int[]) {
                    int[] a = (int[]) value;
                    Object[] r = new Object[a.length];
                    for (int i = 0; i < a.length; i++) r[i] = a[i];
                    return r;
                }
                if (value instanceof long[]) {
                    long[] a = (long[]) value;
                    Object[] r = new Object[a.length];
                    for (int i = 0; i < a.length; i++) r[i] = a[i];
                    return r;
                }
                if (value instanceof double[]) {
                    double[] a = (double[]) value;
                    Object[] r = new Object[a.length];
                    for (int i = 0; i < a.length; i++) r[i] = a[i];
                    return r;
                }
                if (value instanceof Iterable) {
                    List<Object> out = new ArrayList<>();
                    for (Object o : (Iterable<?>) value) out.add(o);
                    return out.toArray();
                }
                return null;
            }

            public static void AssertCompare(Object actual, Object expected, String comparator, String testName) {
                comparator = comparator == null ? "eq" : comparator.toLowerCase(Locale.ROOT);
                testName = testName == null ? "" : testName;
                switch (comparator) {
                    case "eq":
                        Assert.assertEquals(testName, expected, actual);
                        return;
                    case "neq":
                        Assert.assertNotEquals(testName, expected, actual);
                        return;
                    case "seq_eq": {
                        Object[] a = asObjectArray(actual);
                        Object[] e = asObjectArray(expected);
                        if (a == null || e == null) {
                            Assert.fail(testName + ": seq_eq requires both actual and expected to be collections");
                        }
                        Assert.assertArrayEquals(testName, e, a);
                        return;
                    }
                    case "seq_eq_sorted": {
                        Object[] a = asObjectArray(actual);
                        Object[] e = asObjectArray(expected);
                        if (a == null || e == null) {
                            Assert.fail(testName + ": seq_eq_sorted requires both actual and expected to be collections");
                        }
                        Map<Object, Integer> cntA = countMultiset(a);
                        Map<Object, Integer> cntE = countMultiset(e);
                        Assert.assertEquals(testName, cntE, cntA);
                        return;
                    }
                    case "contains": {
                        Object[] a = asObjectArray(actual);
                        if (a != null) {
                            boolean ok = Arrays.asList(a).stream().anyMatch(x -> Objects.equals(x, expected));
                            Assert.assertTrue(testName + ": contains failed", ok);
                            return;
                        }
                        Object[] e = asObjectArray(expected);
                        if (e != null) {
                            boolean ok = Arrays.asList(e).stream().anyMatch(x -> Objects.equals(x, actual));
                            Assert.assertTrue(testName + ": contains failed (reverse)", ok);
                            return;
                        }
                        Assert.fail(testName + ": contains requires one side to be collection");
                        return;
                    }
                    case "lt":
                    case "gt":
                    case "le":
                    case "ge": {
                        if (actual == null || expected == null) {
                            Assert.fail(testName + ": numeric comparator requires non-null operands");
                        }
                        try {
                            double a = Double.parseDouble(actual.toString());
                            double e = Double.parseDouble(expected.toString());
                            switch (comparator) {
                                case "lt": Assert.assertTrue(testName, a < e); return;
                                case "gt": Assert.assertTrue(testName, a > e); return;
                                case "le": Assert.assertTrue(testName, a <= e); return;
                                case "ge": Assert.assertTrue(testName, a >= e); return;
                            }
                        } catch (Exception ex) {
                            Assert.fail(testName + ": numeric comparator failed to convert operands to numbers");
                        }
                        return;
                    }
                    default:
                        throw new UnsupportedOperationException("Comparator '" + comparator + "' not supported");
                }
            }

            private static Map<Object,Integer> countMultiset(Object[] arr) {
                Map<Object,Integer> m = new HashMap<>();
                for (Object o : arr) {
                    Integer c = m.get(o);
                    if (c == null) m.put(o, 1);
                    else m.put(o, c + 1);
                }
                return m;
            }
        }

            final class __ReportHelpers {
                  private static final ObjectMapper MAPPER = new ObjectMapper();
              
                  private __ReportHelpers() {}
              
                  static void emitReport(int totalTests, Result result) {
                      try {
                          Map<String, Object> report = new LinkedHashMap<>();
                          report.put("totalTests", totalTests);
                          report.put("passedTests", Math.max(0, result.getRunCount() - result.getFailureCount() - result.getIgnoreCount()));
              
                          List<Map<String, String>> failedTests = new ArrayList<>();
                          for (Failure f : result.getFailures()) {
                              Map<String, String> item = new LinkedHashMap<>();
              
                              String name = null;
                              try {
                                  if (f.getDescription() != null) {
                                      name = f.getDescription().getMethodName();
                                  }
                              } catch (Throwable ignore) {}
              
                              if (name == null || name.isEmpty()) {
                                  try { name = f.getTestHeader(); } catch (Throwable ignore) {}
                              }
                              if (name == null || name.isEmpty()) {
                                  name = "unknown";
                              }
              
                              String reason = null;
                              try { reason = f.getMessage(); } catch (Throwable ignore) {}
                              if (reason == null || reason.isEmpty()) {
                                  try { reason = f.toString(); } catch (Throwable ignore) {}
                              }
                              if (reason == null || reason.isEmpty()) {
                                  reason = "Unknown error";
                              }
              
                              item.put("name", name);
                              item.put("reason", reason);
                              failedTests.add(item);
                          }
              
                          report.put("failedTests", failedTests);
              
                          System.out.println("__TEST_REPORT_BEGIN__");
                          System.out.println(MAPPER.writeValueAsString(report));
                          System.out.println("__TEST_REPORT_END__");
                          System.out.flush();
                      } catch (Throwable t) {
                          System.out.println("__TEST_REPORT_BEGIN__");
                          System.out.println("{\\"totalTests\\":" + totalTests + ",\\"passedTests\\":0,\\"failedTests\\":[{\\"name\\":\\"Runner\\",\\"reason\\":\\"" + String.valueOf(t).replace("\\"", "\\\\\\"") + "\\"}]}");
                          System.out.println("__TEST_REPORT_END__");
                          System.out.flush();
                      }
                  }
              }
        """;
}