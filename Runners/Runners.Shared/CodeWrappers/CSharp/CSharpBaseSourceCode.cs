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
			private static bool SeqEquals(IEnumerable<object?> a, IEnumerable<object?> b)
			{
				var aa = a.ToArray();
				var bb = b.ToArray();
				if (aa.Length != bb.Length) return false;
				for (int i = 0; i < aa.Length; i++)
					if (!object.Equals(aa[i], bb[i])) return false;
				return true;
			}

			public static bool Compare(object? actual, object? expected, string comparator)
			{
				comparator = comparator?.ToLowerInvariant() ?? ""eq"";
				if (comparator == ""eq"") return object.Equals(actual, expected);
				if (comparator == ""neq"") return !object.Equals(actual, expected);

				if (comparator == ""seq_eq"" || comparator == ""seq_eq_sorted"")
				{
					var aEnum = (actual as System.Collections.IEnumerable)?.Cast<object?>().ToArray();
					var eEnum = (expected as System.Collections.IEnumerable)?.Cast<object?>().ToArray();
					if (aEnum == null || eEnum == null) return false;
					if (comparator == ""seq_eq_sorted"")
					{
						var aSorted = aEnum.Select(x => x?.ToString()).OrderBy(x => x).ToArray();
						var eSorted = eEnum.Select(x => x?.ToString()).OrderBy(x => x).ToArray();
						if (aSorted.Length != eSorted.Length) return false;
						for (int i = 0; i < aSorted.Length; i++)
							if (!string.Equals(aSorted[i], eSorted[i], StringComparison.Ordinal)) return false;
						return true;
					}
					return SeqEquals(aEnum, eEnum);
				}

				if (comparator == ""contains"")
				{
					var aEnum = (actual as System.Collections.IEnumerable)?.Cast<object?>().ToArray();
					if (aEnum == null || expected == null) return false;
					return aEnum.Any(x => object.Equals(x, expected));
				}

				if (new[] { ""lt"", ""gt"", ""le"", ""ge"" }.Contains(comparator))
				{
					if (actual == null || expected == null) return false;
					try
					{
						var a = Convert.ToDouble(actual);
						var e = Convert.ToDouble(expected);
						return comparator switch
						{
							""lt"" => a < e,
							""gt"" => a > e,
							""le"" => a <= e,
							""ge"" => a >= e,
							_ => false
						};
					}
					catch { return false; }
				}

				throw new NotSupportedException($""Comparator '{comparator}' not supported"");
			}
		}
		";
    }

}
