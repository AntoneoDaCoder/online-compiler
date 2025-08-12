class CorrectLinqExample
{
    public class Solution
    {
        public class EmployeeService
        {
            private readonly AppDbContext _context;

            public EmployeeService(AppDbContext context)
            {
                _context = context;
            }

            public Dictionary<string, List<string>> GetEmployeesOlderThan30GroupedByDepartment()
            {
                var query = _context.Departments
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
}

