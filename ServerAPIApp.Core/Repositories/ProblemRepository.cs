using Shared.Models;

namespace ServerAPIApp.Core.Repositories
{
    public class ProblemRepository
    {
        private Dictionary<string, Problem> _database = new Dictionary<string, Problem>();

        public ProblemRepository()
        {
            InitRepository();
        }

        private void InitRepository()
        {
            var heavyTemplate = new Problem()
            {
                AdditionalDefinitions =
                """
                    public class Item
                    {
                        public int Value;
                        public int Weight;
                        public double Ratio => (double)Value / Weight;
                    }
                """,
                TestCases = new List<TestCase>()
                {
                    new()
                    {
                        Name = "Test_SingleItem_FitsExactly",
                        TestInitialization =
                        """
                                var items = new[] { new Item { Value = 60, Weight = 10 } };
                                int capacity = 10;
                        """,
                        InputExpression = "var result = new Solution().FractionalKnapsack(items, capacity);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(60.0).Within(1e-6));"
                    },
                    new()
                    {
                        Name = "Test_SingleItem_Partial",
                        TestInitialization =
                        """
                               var items = new[] { new Item { Value = 100, Weight = 20 } };
                               int capacity = 10;
                        """,
                        InputExpression = "var result = new Solution().FractionalKnapsack(items, capacity);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(50.0).Within(1e-6));"
                    },
                    new()
                    {
                        Name = "Test_MultipleItems_Mixed",
                        TestInitialization =
                        """
                               var items = new[]
                               {
                                    new Item { Value = 60, Weight = 10 },
                                    new Item { Value = 100, Weight = 20 },
                                    new Item { Value = 120, Weight = 30 }
                               };
                               int capacity = 50;
                        """,
                        InputExpression = "var result = new Solution().FractionalKnapsack(items, capacity);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(240.0).Within(1e-6));"
                    },
                    new()
                    {
                        Name = "Test_ZeroCapacity",
                        TestInitialization =
                        """
                                var items = new[] { new Item { Value = 100, Weight = 1 } };
                        """,
                        InputExpression = "var result = new Solution().FractionalKnapsack(items, 0);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(0).Within(1e-6));"
                    },
                    new()
                    {
                        Name = "Test_EmptyItems",
                        InputExpression = "var result = new Solution().FractionalKnapsack(Array.Empty<Item>(), 50);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(0).Within(1e-6));"
                    },
                    new()
                    {
                        Name = "Test_HeavyItems_ShouldChooseBestRatio",
                        TestInitialization =
                        """
                               var items = new[]
                               {
                                    new Item { Value = 100, Weight = 50 }, // ratio = 2.0
                                    new Item { Value = 60, Weight = 10 }   // ratio = 6.0
                               };
                        """,
                        InputExpression = "var result = new Solution().FractionalKnapsack(items, 20);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(80).Within(1e-6));"
                    },
                    new()
                    {
                        Name = "Test_Performance_WithLargeInput",
                        TestInitialization =
                        """
                                var items = new Item[1000];
                                var rnd = new Random(42);

                                for (int i = 0; i < items.Length; i++)
                                {
                                    int value = rnd.Next(1, 1000);
                                    int weight = rnd.Next(1, 100);
                                    items[i] = new Item { Value = value, Weight = weight };
                                }
                                items = items.OrderBy(_ => rnd.Next()).ToArray();
                                int capacity = 10000;
                        """,
                        InputExpression = "var result = new Solution().FractionalKnapsack(items, capacity);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.GreaterThan(0));"
                    },
                }
            };

            var lightTemplate = new Problem()
            {
                TestCases = new List<TestCase>()
                {
                    new()
                    {
                        Name="Test_SingleValue",
                        TestInitialization =
                        """
                                var arr = new int[]{123456};
                        """,
                        InputExpression = "var result = new Solution().FindMinimum(arr);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(123456));"
                    },
                    new()
                    {
                        Name="Test_SeveralValues",
                        TestInitialization =
                        """
                                var rnd = new Random();
                                var arr = new int[rnd.Next(100,1000)];

                                for(int i=0;i<arr.Length;i++)
                                {
                                    arr[i] = rnd.Next(-1000,1200120);
                                }
                        """,
                        InputExpression = "var result = new Solution().FindMinimum(arr);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(arr.Min()));"
                    },
                }
            };

            var heavyTemplateSwift = new Problem()
            {
                AdditionalDefinitions =
                """
                class Item {
                    var value: Int
                    var weight: Int
                    var ratio: Double { return Double(value) / Double(weight) }

                    init(value: Int, weight: Int) {
                        self.value = value
                        self.weight = weight
                    }
                }
                """,
                TestCases = new List<TestCase>()
                {
                new()
                {
                    Name = "Test_SingleItem_FitsExactly",
                    TestInitialization =
                    """
                        let items = [Item(value: 60, weight: 10)]
                        let capacity = 10
                    """,
                    InputExpression = "let result = Solution().fractionalKnapsack(items, capacity)",
                    OutputExpression = "SwiftTestGenerator.assertApproxEqual(result, 60, accuracy: 1e-6, testName: \"Test_SingleItem_FitsExactly\")"
                },
                new()
                {
                    Name = "Test_SingleItem_Partial",
                    TestInitialization =
                    """
                        let items = [Item(value: 100, weight: 20)]
                        let capacity = 10
                    """,
                    InputExpression = "let result = Solution().fractionalKnapsack(items, capacity)",
                    OutputExpression = "SwiftTestGenerator.assertApproxEqual(result, 50, accuracy: 1e-6, testName: \"Test_SingleItem_Partial\")"
                },
                new()
                {
                    Name = "Test_MultipleItems_Mixed",
                    TestInitialization =
                    """
                    let items = [
                        Item(value: 60, weight: 10),
                        Item(value: 100, weight: 20),
                        Item(value: 120, weight: 30)
                    ]
                    let capacity = 50
                    """,
                    InputExpression = "let result = Solution().fractionalKnapsack(items, capacity)",
                      OutputExpression = "SwiftTestGenerator.assertApproxEqual(result, 240, accuracy: 1e-6, testName: \"Test_MultipleItems_Mixed\")"
                },
                new()
                {
                    Name = "Test_ZeroCapacity",
                    TestInitialization =
                    """
                    let items = [Item(value: 100, weight: 1)]
                    """,
                    InputExpression = "let result = Solution().fractionalKnapsack(items, 0)",
                     OutputExpression = "SwiftTestGenerator.assertApproxEqual(result, 0, accuracy: 1e-6, testName: \"Test_ZeroCapacity\")"
                },
                new()
                {
                    Name = "Test_EmptyItems",
                    TestInitialization = """
                    let items: [Item] = []
                    """,
                    InputExpression = "let result = Solution().fractionalKnapsack(items, 50)",
                     OutputExpression = "SwiftTestGenerator.assertApproxEqual(result,0, accuracy: 1e-6, testName: \"Test_EmptyItems\")"
                },
                new()
                {
                    Name = "Test_HeavyItems_ShouldChooseBestRatio",
                    TestInitialization =
                    """
                    let items = [
                        Item(value: 100, weight: 50), // ratio = 2.0
                        Item(value: 60, weight: 10)   // ratio = 6.0
                    ]
                 """,
                    InputExpression = "let result = Solution().fractionalKnapsack(items, 20)",
                     OutputExpression = "SwiftTestGenerator.assertApproxEqual(result, 80, accuracy: 1e-6, testName: \"Test_HeavyItems_ShouldChooseBestRatio\")"
                },
                new()
                {
                    Name = "Test_Performance_WithLargeInput",
                    TestInitialization =
                    """
                        var items = [Item]()
                        var rng = SystemRandomNumberGenerator()
                        for _ in 0..<1000 {
                        let value = Int.random(in: 1..<1000, using: &rng)
                        let weight = Int.random(in: 1..<100, using: &rng)
                        items.append(Item(value: value, weight: weight))
                    }
                    items.shuffle()
                    let capacity = 10000
                    """,
                    InputExpression = "let result = Solution().fractionalKnapsack(items, capacity)",
                     OutputExpression = "SwiftTestGenerator.assertGreater(result, 0, testName: \"Test_Performance_WithLargeInput\")"
                }
                }
            };

            var lightTemplateSwift = new Problem()
            {
                TestCases = new List<TestCase>()
                {
                    new()
                    {
                        Name="Test_SingleValue",
                        TestInitialization =
                        """
                                let arr = [123456]
                        """,
                        InputExpression = "let result = Solution().findMinimum(arr)",
                        OutputExpression = "SwiftTestGenerator.assertEqual(result, 123456, testName: \"Test_SingleValue\")"
                    },
                    new()
                    {
                        Name="Test_SeveralValues",
                        TestInitialization =
                        """
                              var rng = SystemRandomNumberGenerator()
                              let size = Int.random(in: 100..<1000, using: &rng)
                              let arr = (0..<size).map { _ in Int.random(in: -1000..<1200120, using: &rng) }
                        """,
                        InputExpression = "let result = Solution().findMinimum(arr)",
                        OutputExpression = "SwiftTestGenerator.assertEqual(result, arr.min()!, testName: \"Test_SeveralValues\")"
                    },
                }
            };


            heavyTemplate.Name = "HeavyCorrectExample.cs";
            _database["HeavyCorrectExample.cs"] = heavyTemplate;

            heavyTemplate.Name = "TimeoutExample.cs";
            _database["TimeoutExample.cs"] = heavyTemplate;

            lightTemplate.Name = "LightCorrectExample.cs";
            _database["LightCorrectExample.cs"] = lightTemplate;

            lightTemplate.Name = "CompileErrorExample.cs";
            _database["CompileErrorExample.cs"] = lightTemplate;

            lightTemplate.Name = "CheatingExample.cs";
            _database["CheatingExample.cs"] = lightTemplate;



            heavyTemplateSwift.Name = "HeavyCorrectExample.swift";
            _database["HeavyCorrectExample.swift"] = heavyTemplateSwift;

            heavyTemplateSwift.Name = "TimeoutExample.swift";
            _database["TimeoutExample.swift"] = heavyTemplateSwift;

            lightTemplateSwift.Name = "LightCorrectExample.swift";
            _database["LightCorrectExample.swift"] = lightTemplateSwift;

            lightTemplateSwift.Name = "CompileErrorExample.swift";
            _database["CompileErrorExample.swift"] = lightTemplateSwift;

            lightTemplateSwift.Name = "CheatingExample.swift";
            _database["CheatingExample.swift"] = lightTemplateSwift;

            var compileErrorProblem = new Problem
            {
                AdditionalDefinitions = "",
                TestCases = new List<TestCase>
                {
                    new TestCase
                    {
                        Name = "testAdd",
                        TestInitialization = "",
                        InputExpression = "int result = Solution.add(2, 3);",
                        OutputExpression = "assertEquals(5, result);"
                    }
                }
            };

            compileErrorProblem.Name = "CompileError.java";
            _database["CompileError.java"] = compileErrorProblem;

            var commonTemplate = new Problem
            {
                AdditionalDefinitions = """
                public static class Item {
                    public int value;
                    public int weight;

                    public Item(int value, int weight) {
                        this.value = value;
                        this.weight = weight;
                    }

                    public double getRatio() {
                        return (double) value / weight;
                    }
                }
                """,
                TestCases = new List<TestCase>
                {
                    new TestCase
                    {
                        Name = "testTimeOut",
                        TestInitialization = """
                            Item[] items = new Item[30];
                            for (int i = 0; i < items.length; i++) {
                                items[i] = new Item(100, 1);
                            }
                        """,
                        InputExpression = "double result = new Solution().fractionalKnapsack(items, 15);",
                        OutputExpression = "assertTrue(result > 0);"
                    }
                }
            };

            commonTemplate.Name = "TimeOut.java";
            _database["TimeOut.java"] = commonTemplate;

            var forbiddenProblem = new Problem
            {
                AdditionalDefinitions = "",
                TestCases = new List<TestCase>
                {
                    new TestCase
                    {
                        Name = "testAdd",
                        TestInitialization = "",
                        InputExpression = """
                        Solution.openSite(); 
                        int result = 2+3;
                        """,
                        OutputExpression = "assertEquals(5, result);"
                    }
                }
            };

            forbiddenProblem.Name = "ForbiddenExample.java";
            _database["ForbiddenExample.java"] = forbiddenProblem;

        }

        public Problem GetProblem(string name)
        {
            return _database[name];
        }
    }
}
