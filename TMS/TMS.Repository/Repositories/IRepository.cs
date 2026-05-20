using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository.Repositories
{
    internal interface IRepository<T> where T : class
    {
        IQueryable<T> GetAsync(string[]? includes = null);
        Task<T?> GetAsync(int id, string[]? includes = null);
        Task<bool> AddAsync(T entity);
        Task<bool> AddAsync(List<T> entity);
        Task<bool> UpdateAsync(T entity);
        Task<IEnumerable<T>> Find(Expression<Func<T, bool>> predicate, string[]? includes = null);
        Task SaveChangesAsync();
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    }
}
