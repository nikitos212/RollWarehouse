using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RollWarehouse.Domain.Entities;

namespace RollWarehouse.Application.Abstractions.Ports
{
    public record RollFilter(Guid? IdMin, Guid? IdMax, double? WeightMin, double? WeightMax, double? LengthMin, double? LengthMax, DateTime? DateAddedFrom, DateTime? DateAddedTo, DateTime? DateRemovedFrom, DateTime? DateRemovedTo);

    public interface IRollRepository
    {
        Task<Roll> AddAsync(Roll roll);
        Task<Roll?> GetByIdAsync(Guid id);
        Task<Roll?> DeleteAsync(Guid id);
        Task<IEnumerable<Roll>> ListAsync();
        Task<IEnumerable<Roll>> ListFilteredAsync(RollFilter filter);
        Task<IEnumerable<Roll>> GetForPeriodAsync(DateTime start, DateTime end);
    }
}
