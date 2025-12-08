// JavaBaseSourceCode.java
package com.mems.helpers;

public final class JavaBaseSourceCode {
    public static final String SOURCE = """
        import java.util.*;
        import java.util.stream.*;
        import org.junit.Assert;

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
                    boolean foundKey = false;
                    // try to use sensible keys: primitives boxed and strings and objects via equals/hash
                    Object key = o;
                    Integer c = m.get(key);
                    if (c == null) m.put(key, 1);
                    else m.put(key, c + 1);
                }
                return m;
            }
        }
        """;
}
