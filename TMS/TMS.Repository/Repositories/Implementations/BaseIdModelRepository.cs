using TMS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository.Repositories.Implementations
{
    internal class BaseIdModelRepository<T> : IBaseIdModelRepository<T> where T : BaseIdModel
    {
        private readonly ApplicationDBContext _context;
        private readonly DbSet<T> _dbSet;
        public BaseIdModelRepository(ApplicationDBContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public IQueryable<T> GetAsync(string[]? includes = null)
        {
            var query = _dbSet.AsNoTracking();
            if (includes != null && includes.Length > 0)
            {
                foreach (var icludeItem in includes)
                {
                    query = query.Include(icludeItem);
                }
            }
            return query;
        }
        public Task<T?> GetAsync(int id, string[]? includes = null)
        {
            var query = _dbSet.AsQueryable();
            if (includes != null && includes.Length > 0)
            {
                foreach (var icludeItem in includes)
                {
                    query = query.Include(icludeItem);
                }
            }
            return query.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return true;
        }

        public async Task<bool> AddAsync(List<T> entity)
        {
            await _dbSet.AddRangeAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.Entry(entity).Property(t => t.Id).IsModified = false;
            return await Task.FromResult(true);
        }

        public async Task<IEnumerable<T>> Find(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).AsNoTracking().ToListAsync();
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate)
        {
            if (predicate == null)
                return _dbSet.CountAsync();
            return _dbSet.Where(predicate).CountAsync();
        }
    }
}
