class SimpleExample
{
    public class Solution
    {
        public Dictionary<string, List<string>> GetEmployeesOlderThan30GroupedByDepartment(AppDbContext context)
        {
            var query = context.Departments
                .Select(d => new
                {
                    Department = d.Name,
                    Employees = d.Employees
                        .Where(e => e.Age > 30)
                        .OrderByDescending(e => e.Age)
                        .Select(e => e.Name)
                        .ToList()
                })
                .Where(x => x.Employees.Any());

            return query.ToDictionary(x => x.Department, x => x.Employees);
        }
    }
}

