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
                    },
                    new AdditionalDefinition()
                    {
                        Language="nodejs",
                        Value=
                        """
                        class Item {
                            constructor(value, weight) {
                                this.value = value;
                                this.weight = weight;
                            }
                            get ratio() {
                                return this.value / this.weight;
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
                new()
                {
                    Name = "Test_SingleItem_FitsExactly",
                    TestLanguage = "java",
                    TestInitialization =
                    """
                        Item[] items = { new Item(60, 10) };
                        int capacity = 10;
                    """,
                    InputExpression = "double result = new Solution().fractionalKnapsack(items, capacity);",
                    OutputExpression = "assertEquals(60.0, result, 1e-6);"
                },
                new()
                {
                    Name = "Test_SingleItem_Partial",
                    TestLanguage = "java",
                    TestInitialization =
                    """
                        Item[] items = { new Item(100, 20) };
                        int capacity = 10;
                    """,
                    InputExpression = "double result = new Solution().fractionalKnapsack(items, capacity);",
                    OutputExpression = "assertEquals(50.0, result, 1e-6);"
                },
                new()
                {
                    Name = "Test_MultipleItems_Mixed",
                    TestLanguage = "java",
                    TestInitialization =
                    """
                        Item[] items = {
                            new Item(60, 10),
                            new Item(100, 20),
                            new Item(120, 30)
                        };
                        int capacity = 50;
                    """,
                    InputExpression = "double result = new Solution().fractionalKnapsack(items, capacity);",
                    OutputExpression = "assertEquals(240.0, result, 1e-6);"
                },
                new()
                {
                    Name = "Test_ZeroCapacity",
                    TestLanguage = "java",
                    TestInitialization =
                    """
                        Item[] items = { new Item(100, 1) };
                    """,
                    InputExpression = "double result = new Solution().fractionalKnapsack(items, 0);",
                    OutputExpression = "assertEquals(0.0, result, 1e-6);"
                },
                new()
                {
                    Name = "Test_EmptyItems",
                    TestLanguage = "java",
                    TestInitialization =
                    """
                        Item[] items = new Item[0];
                    """,
                    InputExpression = "double result = new Solution().fractionalKnapsack(items, 50);",
                    OutputExpression = "assertEquals(0.0, result, 1e-6);"
                },
                new()
                {
                    Name = "Test_HeavyItems_ShouldChooseBestRatio",
                    TestLanguage = "java",
                    TestInitialization =
                    """
                        Item[] items = {
                            new Item(100, 50), // ratio = 2.0
                            new Item(60, 10)   // ratio = 6.0
                        };
                    """,
                    InputExpression = "double result = new Solution().fractionalKnapsack(items, 20);",
                    OutputExpression = "assertEquals(80.0, result, 1e-6);"
                },
                new()
                {
                    Name = "Test_Performance_WithLargeInput",
                    TestLanguage = "java",
                    TestInitialization =
                    """
                        Item[] items = new Item[1000];
                        java.util.Random rnd = new java.util.Random(42);
                            for (int i = 0; i < items.length; i++) {
                            int value = rnd.nextInt(999) + 1;
                            int weight = rnd.nextInt(99) + 1;
                            items[i] = new Item(value, weight);
                        }
                        java.util.List<Item> list = java.util.Arrays.asList(items);
                        java.util.Collections.shuffle(list, rnd);
                        list.toArray(items);
                        int capacity = 10000;
                        """,
                    InputExpression = "double result = new Solution().fractionalKnapsack(items, capacity);",
                    OutputExpression = "assertTrue(result > 0);"
                },
                  new()
                    {
                        Name = "Test_SingleItem_FitsExactly",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                                const items = [ new Item(60, 10) ];
                                const capacity = 10;
                        """,
                        InputExpression = "const result = new Solution().fractionalKnapsack(items, capacity);",
                        OutputExpression = "NodeTestGenerator.assertApproxEqual(result, 60.0, 1e-6, \"Test_SingleItem_FitsExactly\");"
                    },
                    new()
                    {
                        Name = "Test_SingleItem_Partial",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                               const items = [ new Item(100, 20) ];
                               const capacity = 10;
                        """,
                        InputExpression = "const result = new Solution().fractionalKnapsack(items, capacity);",
                        OutputExpression = "NodeTestGenerator.assertApproxEqual(result, 50.0, 1e-6, \"Test_SingleItem_Partial\");"
                    },
                    new()
                    {
                        Name = "Test_MultipleItems_Mixed",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                               const items = [
                            new Item(60, 10),
                            new Item(100, 20),
                            new Item(120, 30)
                        ];
                               const capacity = 50;
                        """,
                        InputExpression = "const result = new Solution().fractionalKnapsack(items, capacity);",
                        OutputExpression = "NodeTestGenerator.assertApproxEqual(result, 240.0, 1e-6, \"Test_MultipleItems_Mixed\");"
                    },
                    new()
                    {
                        Name = "Test_ZeroCapacity",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                                const items = [ new Item(100, 1) ];
                        """,
                        InputExpression = "const result = new Solution().fractionalKnapsack(items, 0);",
                        OutputExpression = "NodeTestGenerator.assertApproxEqual(result, 0, 1e-6, \"Test_ZeroCapacity\");"
                    },
                    new()
                    {
                        Name = "Test_EmptyItems",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                                const items = [];
                        """,
                        InputExpression = "const result = new Solution().fractionalKnapsack(items, 50);",
                        OutputExpression = "NodeTestGenerator.assertApproxEqual(result, 0, 1e-6, \"Test_EmptyItems\");"
                    },
                    new()
                    {
                        Name = "Test_HeavyItems_ShouldChooseBestRatio",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                               const items = [
                            new Item(100, 50), // ratio = 2.0
                            new Item(60, 10)   // ratio = 6.0
                        ];
                        const capacity = 20;
                        """,
                        InputExpression = "const result = new Solution().fractionalKnapsack(items, capacity);",
                        OutputExpression = "NodeTestGenerator.assertApproxEqual(result, 80.0, 1e-6, \"Test_HeavyItems_ShouldChooseBestRatio\");"
                    },
                    new()
                    {
                        Name = "Test_Performance_WithLargeInput",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                               const items = [];
                        const rnd = () => Math.floor(Math.random() * 1000) + 1;
                        for (let i = 0; i < 1000; i++) {
                            const value = rnd();
                            const weight = Math.floor(Math.random() * 99) + 1;
                            items.push(new Item(value, weight));
                        }
                        // тасуем массив
                        items.sort(() => Math.random() - 0.5);

                        const capacity = 10000;
                        """,
                        InputExpression = "const result = new Solution().fractionalKnapsack(items, capacity);",
                        OutputExpression = "NodeTestGenerator.assertGreater(result, 0, \"Test_Performance_WithLargeInput\");"
                    },
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
                        Name = "Test_SingleValue",
                        TestLanguage = "java",
                        TestInitialization =
                        """
                            int[] arr = new int[]{123456};
                        """,
                        InputExpression = "int result = new Solution().findMinimum(arr);",
                        OutputExpression = "assertEquals(123456, result);"
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
                    },
                       new()
                    {
                        Name="Test_SingleValue",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                            const arr = [123456];
                        """,
                        InputExpression = "const result = new Solution().findMinimum(arr);",
                        OutputExpression = "NodeTestGenerator.assertEqual(result, 123456, 'Test_SingleValue');"
                    },
                    new()
                    {
                        Name="Test_SeveralValues",
                        TestLanguage = "nodejs",
                        TestInitialization =
                        """
                            const size = Math.floor(Math.random() * 900) + 100;
                            const arr = Array.from({ length: size }, () => Math.floor(Math.random() * (1200120 + 1000)) - 1000);
                        """,
                        InputExpression = "const result = new Solution().findMinimum(arr);",
                        OutputExpression = "NodeTestGenerator.assertEqual(result, Math.min(...arr), 'Test_SeveralValues');"
                    },
                    new()
                    {
                        Name="Test_MixedValues",
                        TestLanguage = "typescript",
                        TestInitialization =
                        """
                            const arr: number[] = [10, -5, 0, 100, -20, 50];
                        """,
                        InputExpression = "const result = new Solution().findMinimum(arr);",
                        OutputExpression = "NodeTestGenerator.assertEqual(result, -20, 'Test_MixedValues');"
                    },
                    new()
                    {
                        Name="Test_SingleValue",
                        TestLanguage = "typescript",
                        TestInitialization =
                        """
                            const arr: number[] = [123456];
                        """,
                        InputExpression = "const result = new Solution().findMinimum(arr);",
                        OutputExpression = "NodeTestGenerator.assertEqual(result, 123456, 'Test_SingleValue');"
                    },
                    new()
                    {
                        Name="Test_SeveralValues",
                        TestLanguage = "typescript",
                        TestInitialization =
                        """
                            const size: number = Math.floor(Math.random() * 900) + 100;
                            const arr: number[] = Array.from({ length: size }, () => 
                                Math.floor(Math.random() * (1200120 + 1000)) - 1000
                            );
                        """,
                        InputExpression = "const result = new Solution().findMinimum(arr);",
                        OutputExpression = "NodeTestGenerator.assertEqual(result, Math.min(...arr), 'Test_SeveralValues');"
                    },
                    new()
                    {
                        Name="Test_NegativeValues",
                        TestLanguage = "typescript",
                        TestInitialization =
                        """
                            const arr: number[] = [-5, -10, -3, -8, -1];
                        """,
                        InputExpression = "const result = new Solution().findMinimum(arr);",
                        OutputExpression = "NodeTestGenerator.assertEqual(result, -10, 'Test_NegativeValues');"
                    },
                }
            };

            _database[heavyTemplate.Name] = heavyTemplate;
            _database[lightTemplate.Name] = lightTemplate;

            var sqlTemplate = new Problem
            {
                Name = "CustomersWithExpensiveOrders",
                AdditionalDefinitions = new List<AdditionalDefinition>
                {
                    new()
                    {
                        Language = "sql",
                        Value = @"CREATE TABLE Customers(Id INT PRIMARY KEY, Name TEXT);
                                  CREATE TABLE Orders(Id INT PRIMARY KEY, CustomerId INT, Amount REAL,
                                                       FOREIGN KEY(CustomerId) REFERENCES Customers(Id));
                                  INSERT INTO Customers VALUES (1, 'Tim'), (2, 'Tom'), (3, 'Don');
                                  INSERT INTO Orders VALUES (1, 1, 150), (2, 1, 90), (3, 2, 200), (4, 3, 50);"
                    }
                },
                TestCases = new List<TestCase>
                {
                    new()
                    {
                        Name = "Test_TimAndTom_ShouldFail",
                        TestLanguage = "sql",
                        TestInitialization = "",
                        InputExpression = @"
                                            SELECT CASE
                                                WHEN (SELECT COUNT(*) FROM (...)) = 3
                                                THEN 1 ELSE 0
                                            END",
                        OutputExpression = ""
                    }
                }
            };

            _database[sqlTemplate.Name] = sqlTemplate;

            sqlTemplate = new Problem
            {
                Name = "CategoriesWithHighTotalPrice",
                AdditionalDefinitions = new List<AdditionalDefinition>
                {
                    new()
                    {
                        Language = "sql",
                        Value = @"CREATE TABLE Categories(Id INT PRIMARY KEY, Name TEXT);
                                  CREATE TABLE Products(Id INT PRIMARY KEY, CategoryId INT, Price REAL,
                                                         FOREIGN KEY(CategoryId) REFERENCES Categories(Id));
                                  INSERT INTO Categories VALUES (1, 'Electronics'), (2, 'Furniture'), (3, 'Clothes');
                                  INSERT INTO Products VALUES (1, 1, 300), (2, 1, 250), (3, 2, 600), (4, 3, 100), (5, 3, 50);"
                    }
                },
                TestCases = new List<TestCase>
                {
                    new()
                    {
                        Name = "Test_ElectronicsExists",
                        TestLanguage = "sql",
                        TestInitialization = "",
                        InputExpression = @"
                                            SELECT CASE
                                                WHEN
                                                    (SELECT COUNT(*) FROM (...)
                                                        WHERE Name IN ('Electronics', 'Furniture')
                                                    ) = 2
                                                AND
                                                    (SELECT COUNT(*) FROM (...)) = 2
                                                THEN 1 ELSE 0
                                            END;",
                        OutputExpression = ""
                    }
                }
            };

            _database[sqlTemplate.Name] = sqlTemplate;


            sqlTemplate = new Problem
            {
                Name = "StudentsWithMultipleCourses",
                AdditionalDefinitions = new List<AdditionalDefinition>
                {
                    new()
                    {
                        Language = "sql",
                        Value = @"CREATE TABLE Students(Id INT PRIMARY KEY, Name TEXT);
                                  CREATE TABLE Courses(Id INT PRIMARY KEY, Title TEXT);
                                  CREATE TABLE Enrollments(StudentId INT, CourseId INT,
                                                           FOREIGN KEY(StudentId) REFERENCES Students(Id),
                                                           FOREIGN KEY(CourseId) REFERENCES Courses(Id));
                                  INSERT INTO Students VALUES (1, 'Tim'), (2, 'Tom'), (3, 'Don');
                                  INSERT INTO Courses VALUES (1, 'Math'), (2, 'Physics'), (3, 'History');
                                  INSERT INTO Enrollments VALUES (1, 1), (1, 2), (2, 2), (3, 1), (3, 3);"
                    }
                },
                TestCases = new List<TestCase>
                {
                    new()
                    {
                        Name = "Test_TimAndDonExist",
                        TestLanguage = "sql",
                        TestInitialization = "",
                        InputExpression = @"
                                            SELECT CASE
                                                WHEN (SELECT COUNT(*) FROM (...)) = 2
                                                 AND EXISTS (SELECT 1 FROM (...) WHERE Name='Tim')
                                                 AND EXISTS (SELECT 1 FROM (...) WHERE Name='Don')
                                                 AND NOT EXISTS (SELECT 1 FROM (...) WHERE Name='Tom')
                                                THEN 1 ELSE 0
                                            END",
                        OutputExpression = ""
                    },
                }
            };

            _database[sqlTemplate.Name] = sqlTemplate;

            var compileErrorProblem = new Problem
            {
                Name = "CompileError",
                TestCases = new List<TestCase>
                {
                    new TestCase
                    {
                        Name = "testAdd",
                        TestLanguage = "java",
                        TestInitialization = "",
                        InputExpression = "int result = Solution.add(2, 3);",
                        OutputExpression = "assertEquals(5, result);"
                    }
                }
            };

            _database[compileErrorProblem.Name] = compileErrorProblem;

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
