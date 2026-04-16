using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ServerAPIApp.DAL.Extensions
{
    internal static class QueryExtensions
    {
        public static IQueryable<T> Includes<T>(this IQueryable<T> query, params Expression<Func<T, object>>[] includes)
                    where T : class
        {
            if (includes != null && includes.Length != 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query;
        }
    }
}
