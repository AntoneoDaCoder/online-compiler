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
                Name = "FractionalKnapsack",
                AdditionalDefinitions = new List<AdditionalDefinition>()
                {
                    new AdditionalDefinition()
                    {
                        Language = "csharp",
                        Value=
                        """
                            public class Item
                            {
                                public int Value;
                                public int Weight;
                                public double Ratio => (double)Value / Weight;
                            }
                        """,
                    },
                    new AdditionalDefinition()
                    {
                        Language = "swift",
                        Value =
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
                        """
                    },
                    new AdditionalDefinition()
                    {
                        Language = "java",
                        Value = """
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
                        """
                    }
                },
                TestCases = new List<TestCase>()
                {
                    new()
                    {
                        Name = "Test_SingleItem_FitsExactly",
                        TestLanguage = "csharp",
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
                        TestLanguage = "csharp",
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
                        TestLanguage = "csharp",
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
                        TestLanguage = "csharp",
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
                        TestLanguage = "csharp",
                        InputExpression = "var result = new Solution().FractionalKnapsack(Array.Empty<Item>(), 50);",
                        OutputExpression = "NUnit.Framework.Assert.That(result, Is.EqualTo(0).Within(1e-6));"
                    },
                    new()
                    {
                        Name = "Test_HeavyItems_ShouldChooseBestRatio",
                        TestLanguage = "csharp",
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
                        TestLanguage = "csharp",
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
                new()
                {
                    Name = "Test_SingleItem_FitsExactly",
                        TestLanguage = "swift",
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
                        TestLanguage = "swift",
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
                        TestLanguage = "swift",
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
                        TestLanguage = "swift",
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
                        TestLanguage = "swift",
                    TestInitialization = """
                    let items: [Item] = []
                    """,
                    InputExpression = "let result = Solution().fractionalKnapsack(items, 50)",
                     OutputExpression = "SwiftTestGenerator.assertApproxEqual(result,0, accuracy: 1e-6, testName: \"Test_EmptyItems\")"
                },
                new()
                {
                    Name = "Test_HeavyItems_ShouldChooseBestRatio",
                        TestLanguage = "swift",
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
                        TestLanguage = "swift",
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
                },
                 new TestCase
                    {
                        Name = "testTimeOut",
                        TestLanguage="java",
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

            var lightTemplate = new Problem()
            {
                Name = "ArrayMin",
                TestCases = new List<TestCase>()
                {
                    new()
                    {
                        Name="Test_SingleValue",
                        TestLanguage = "csharp",
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
                        TestLanguage = "csharp",
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
                    new()
                    {
                        Name="Test_SingleValue",
                        TestLanguage = "swift",
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
                        TestLanguage = "swift",
                        TestInitialization =
                        """
                              var rng = SystemRandomNumberGenerator()
                              let size = Int.random(in: 100..<1000, using: &rng)
                              let arr = (0..<size).map { _ in Int.random(in: -1000..<1200120, using: &rng) }
                        """,
                        InputExpression = "let result = Solution().findMinimum(arr)",
                        OutputExpression = "SwiftTestGenerator.assertEqual(result, arr.min()!, testName: \"Test_SeveralValues\")"
                    },
                    new()
                    {
                        Name="Test_SeveralValues",
                        TestLanguage = "java",
                        TestInitialization =
                        """
                            Random rnd = new Random();
                            int[] arr = new int[rnd.nextInt(900) + 100]; 

                            for(int i = 0; i < arr.length; i++) {
                                arr[i] = rnd.nextInt(1200120 + 1000) - 1000;
                            }
                        """,
                        InputExpression="int result = new Solution().findMinimum(arr);",
                        OutputExpression = "assertTrue(result == Arrays.stream(arr).min().getAsInt());"
                    }
                }
            };

            _database[heavyTemplate.Name] = heavyTemplate;
            _database[lightTemplate.Name] = lightTemplate;

            var sqlTemplate = new Problem
            {
                Name = "FirstSqlProblem",
                AdditionalDefinitions = new List<AdditionalDefinition>
                {
                    new()
                    {
                        Language="sql",
                        Value=@"CREATE TABLE Customers(Id INT, Name TEXT);
                                INSERT INTO Customers VALUES (1, 'Alice'), (2, 'Bob');"
                    }
                },
                TestCases = new List<TestCase>()
                {
                    new()
                    {
                        Name="Test_AliceExists",
                        TestLanguage="sql",
                        TestInitialization="",
                        InputExpression="SELECT CASE WHEN EXISTS (SELECT 1 FROM (...) WHERE Name = 'Alice') THEN 1 ELSE 0 END",
                        OutputExpression=""
                    }
                }
            };

            _database[sqlTemplate.Name] = sqlTemplate;

            var linqTemplate = new Problem
            {
                Name = "CorrectLinqExample",
                AdditionalDefinitions = new List<AdditionalDefinition>
                {
                    new()
                    {
                        Language="csharp",
                        Value=@"public class Department
                                {
                                    public int Id { get; set; }
                                    public string Name { get; set; }
                                    public List<Employee> Employees { get; set; } = new();
                                }

                                public class Employee
                                {
                                    public int Id { get; set; }
                                    public string Name { get; set; }
                                    public int Age { get; set; }
                                    public int DepartmentId { get; set; }
                                    public Department Department { get; set; }
                                }

                                public class AppDbContext : DbContext
                                {
                                    public DbSet<Department> Departments { get; set; }
                                    public DbSet<Employee> Employees { get; set; }

                                    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
                                }"
                    }
                },
                TestCases = new List<TestCase>()
                {
                    new()
                    {
                        Name="Test_ReturnsCorrectAmount",
                        TestLanguage="csharp",
                        TestInitialization="",
                        InputExpression="var result = _service.GetEmployeesOlderThan30GroupedByDepartment();",
                        OutputExpression="Assert.Equal(3, result.Count);"
                    }
                }
            };

            _database[linqTemplate.Name] = linqTemplate;


            //var compileErrorProblem = new Problem
            //{
            //    Name = "Add",
            //    TestCases = new List<TestCase>
            //    {
            //        new TestCase
            //        {
            //            Name = "testAdd",
            //            TestLanguage = "java",
            //            TestInitialization = "",
            //            InputExpression = "int result = Solution.add(2, 3);",
            //            OutputExpression = "assertEquals(5, result);"
            //        }
            //    }
            //};

            //_database[compileErrorProblem.Name] = compileErrorProblem;

            //var forbiddenProblem = new Problem
            //{
            //    Name = "NetworkUsage",
            //    TestCases = new List<TestCase>
            //    {
            //        new TestCase
            //        {
            //            Name = "testAdd",
            //            TestInitialization = "",
            //            InputExpression = """
            //            Solution.openSite(); 
            //            int result = 2+3;
            //            """,
            //            OutputExpression = "assertEquals(5, result);"
            //        }
            //    }
            //};
            //_database[forbiddenProblem.Name] = forbiddenProblem;

        }


        public Problem GetProblem(string name)
        {
            return _database[name];
        }
    }
}
