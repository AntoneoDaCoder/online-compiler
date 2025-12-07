using System;
using System.Collections.Generic;
using System.Text;

namespace Runners.Shared.CodeWrappers.CSharp
{
    public static class CSharpBaseSourceCode
    {
        public const string Source = @"
	public static class RunnerHelpers
{
    private static IEnumerable<object?>? AsEnumerable(object? value)
    {
        if (value == null) return null;
        if (value is string) return null;
        if (value is IEnumerable en)
            return en.Cast<object?>().ToArray();
        return null;
    }

    public static void AssertCompare(object? actual, object? expected, string comparator, string testName = null!)
    {
        comparator = comparator?.ToLowerInvariant() ?? ""eq"";
        testName ??= string.Empty;

        switch (comparator)
        {
            case ""eq"":
                // works for scalars and sequences (sequence equality compares elements)
                Assert.That(actual, Is.EqualTo(expected), testName);
                return;

            case ""neq"":
                Assert.That(actual, Is.Not.EqualTo(expected), testName);
                return;

            case ""seq_eq"":
                {
                    var a = AsEnumerable(actual);
                    var e = AsEnumerable(expected);
                    if (a == null || e == null)
                        Assert.Fail($""{testName}: seq_eq requires both actual and expected to be collections (actual type={actual?.GetType().Name}, expected type={expected?.GetType().Name})"");
                    Assert.That(a, Is.EqualTo(e), testName);
                    return;
                }

            case ""seq_eq_sorted"":
                {
                    var a = AsEnumerable(actual);
                    var e = AsEnumerable(expected);
                    if (a == null || e == null)
                        Assert.Fail($""{testName}: seq_eq_sorted requires both actual and expected to be collections"");
                    // Equivalent checks same elements with same counts, order-insensitive
                    Assert.That(a, Is.EquivalentTo(e), testName);
                    return;
                }

            case ""contains"":
                {
                    var a = AsEnumerable(actual);
                    if (a != null)
                    {
                        // Does.Contain expects the element to match; for complex types NUnit compares by Equals
                        Assert.That(a, Does.Contain(expected), testName);
                        return;
                    }
                    var e = AsEnumerable(expected);
                    if (e != null)
                    {
                        Assert.That(e, Does.Contain(actual), testName);
                        return;
                    }
                    Assert.Fail($""{testName}: contains requires one side to be a collection"");
                    return;
                }

            case ""lt"":
                AssertNumericComparison(actual, expected, (a, b) => Assert.That(a, Is.LessThan(b), testName), comparator, testName);
                return;
            case ""gt"":
                AssertNumericComparison(actual, expected, (a, b) => Assert.That(a, Is.GreaterThan(b), testName), comparator, testName);
                return;
            case ""le"":
                AssertNumericComparison(actual, expected, (a, b) => Assert.That(a, Is.LessThanOrEqualTo(b), testName), comparator, testName);
                return;
            case ""ge"":
                AssertNumericComparison(actual, expected, (a, b) => Assert.That(a, Is.GreaterThanOrEqualTo(b), testName), comparator, testName);
                return;

            default:
                throw new NotSupportedException($""Comparator '{comparator}' not supported"");
        }
    }

    private static void AssertNumericComparison(object? actual, object? expected, Action<double, double> assertAction, string comparator, string testName)
    {
        if (actual == null || expected == null)
            Assert.Fail($""{testName}: numeric comparator '{comparator}' requires non-null operands"");

        try
        {
            var a = Convert.ToDouble(actual);
            var e = Convert.ToDouble(expected);
            assertAction(a, e);
        }
        catch (Exception)
        {
            Assert.Fail($""{testName}: numeric comparator '{comparator}' failed to convert operands to numbers (actual type={actual?.GetType().Name}, expected type={expected?.GetType().Name})"");
        }
    }
}
		";
    }

}
