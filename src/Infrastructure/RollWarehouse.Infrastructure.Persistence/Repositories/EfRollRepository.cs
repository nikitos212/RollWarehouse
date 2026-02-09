using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RollWarehouse.Application.Abstractions.Ports;
using RollWarehouse.Domain.Entities;

namespace RollWarehouse.Infrastructure.Persistence.Repositories
{
    public class EfRollRepository : IRollRepository
    {
        private readonly PersistenceContext _db;
        public EfRollRepository(PersistenceContext db) { _db = db; }
        public async Task<Roll> AddAsync(Roll roll)
        {
            _db.Rolls.Add(roll);
            await _db.SaveChangesAsync();
            return roll;
        }
        public async Task<Roll?> GetByIdAsync(Guid id)
        {
            return await _db.Rolls.FindAsync(id);
        }
        public async Task<Roll?> DeleteAsync(Guid id)
        {
            var r = await _db.Rolls.FindAsync(id);
            if (r == null) return null;
            if (r.DateRemoved == null)
            {
                r.DateRemoved = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return r;
        }
        public async Task<IEnumerable<Roll>> ListAsync()
        {
            return await _db.Rolls.AsNoTracking().ToListAsync();
        }
        public async Task<IEnumerable<Roll>> ListFilteredAsync(RollFilter filter)
        {
            var q = _db.Rolls.AsQueryable();
            if (filter.WeightMin.HasValue) q = q.Where(r => r.Weight >= filter.WeightMin.Value);
            if (filter.WeightMax.HasValue) q = q.Where(r => r.Weight <= filter.WeightMax.Value);
            if (filter.LengthMin.HasValue) q = q.Where(r => r.Length >= filter.LengthMin.Value);
            if (filter.LengthMax.HasValue) q = q.Where(r => r.Length <= filter.LengthMax.Value);
            if (filter.DateAddedFrom.HasValue) q = q.Where(r => r.DateAdded >= filter.DateAddedFrom.Value);
            if (filter.DateAddedTo.HasValue) q = q.Where(r => r.DateAdded <= filter.DateAddedTo.Value);
            if (filter.DateRemovedFrom.HasValue) q = q.Where(r => r.DateRemoved != null && r.DateRemoved >= filter.DateRemovedFrom.Value);
            if (filter.DateRemovedTo.HasValue) q = q.Where(r => r.DateRemoved != null && r.DateRemoved <= filter.DateRemovedTo.Value);
            if (filter.IdMin.HasValue) q = q.Where(r => r.Id.CompareTo(filter.IdMin.Value) >= 0);
            if (filter.IdMax.HasValue) q = q.Where(r => r.Id.CompareTo(filter.IdMax.Value) <= 0);
            return await q.AsNoTracking().ToListAsync();
        }
        public async Task<IEnumerable<Roll>> GetForPeriodAsync(DateTime start, DateTime end)
        {
            return await _db.Rolls.Where(r => r.DateAdded < end && (r.DateRemoved == null || r.DateRemoved > start)).AsNoTracking().ToListAsync();
        }
    }
}
