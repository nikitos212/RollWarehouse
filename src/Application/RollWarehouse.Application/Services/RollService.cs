using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using RollWarehouse.Application.Abstractions.Ports;
using RollWarehouse.Domain.Entities;

namespace RollWarehouse.Application.Services
{
    public class RollsStatistics
    {
        public int AddedCount { get; set; }
        public int RemovedCount { get; set; }
        public double? AverageLength { get; set; }
        public double? AverageWeight { get; set; }
        public double? MaxLength { get; set; }
        public double? MinLength { get; set; }
        public double? MaxWeight { get; set; }
        public double? MinWeight { get; set; }
        public double TotalWeight { get; set; }
        public double? MaxIntervalSeconds { get; set; }
        public double? MinIntervalSeconds { get; set; }
    }

    public class RollsPeriodDayExtrema
    {
        public DateTime? DayWithMinCount { get; set; }
        public DateTime? DayWithMaxCount { get; set; }
        public DateTime? DayWithMinTotalWeight { get; set; }
        public DateTime? DayWithMaxTotalWeight { get; set; }
    }

    public class RollService
    {
        private readonly IRollRepository _repo;
        public RollService(IRollRepository repo)
        {
            _repo = repo;
        }

        public async Task<Roll> AddRollAsync(double length, double weight)
        {
            if (length <= 0) throw new ArgumentException("Length must be > 0");
            if (weight <= 0) throw new ArgumentException("Weight must be > 0");
            var r = new Roll { Length = length, Weight = weight, DateAdded = DateTime.UtcNow };
            return await _repo.AddAsync(r);
        }

        public async Task<Roll?> DeleteRollAsync(Guid id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;
            return await _repo.DeleteAsync(id);
        }

        public async Task<IEnumerable<Roll>> ListFilteredAsync(RollFilter filter)
        {
            return await _repo.ListFilteredAsync(filter);
        }

        public async Task<RollsStatistics> GetStatisticsAsync(DateTime start, DateTime end)
        {
            if (end <= start) throw new ArgumentException("End must be after start");
            var rolls = (await _repo.GetForPeriodAsync(start, end)).ToList();
            var addedCount = rolls.Count(r => r.DateAdded >= start && r.DateAdded < end);
            var removedCount = rolls.Count(r => r.DateRemoved.HasValue && r.DateRemoved.Value >= start && r.DateRemoved.Value < end);
            var lengths = rolls.Select(r => r.Length).ToList();
            var weights = rolls.Select(r => r.Weight).ToList();
            double? avgLength = lengths.Any() ? lengths.Average() : null;
            double? avgWeight = weights.Any() ? weights.Average() : null;
            double? maxLength = lengths.Any() ? lengths.Max() : null;
            double? minLength = lengths.Any() ? lengths.Min() : null;
            double? maxWeight = weights.Any() ? weights.Max() : null;
            double? minWeight = weights.Any() ? weights.Min() : null;
            double totalWeight = weights.Sum();
            var durations = rolls.Where(r => r.DateRemoved.HasValue).Select(r => (r.DateRemoved!.Value - r.DateAdded).TotalSeconds).ToList();
            double? maxDur = durations.Any() ? durations.Max() : null;
            double? minDur = durations.Any() ? durations.Min() : null;
            return new RollsStatistics
            {
                AddedCount = addedCount,
                RemovedCount = removedCount,
                AverageLength = avgLength,
                AverageWeight = avgWeight,
                MaxLength = maxLength,
                MinLength = minLength,
                MaxWeight = maxWeight,
                MinWeight = minWeight,
                TotalWeight = totalWeight,
                MaxIntervalSeconds = maxDur,
                MinIntervalSeconds = minDur
            };
        }

        public async Task<RollsPeriodDayExtrema> GetPeriodDayExtremaAsync(DateTime start, DateTime end)
        {
            if (end <= start) throw new ArgumentException("End must be after start");
            var rolls = (await _repo.GetForPeriodAsync(start, end)).ToList();
            var dayCounts = new Dictionary<DateTime, int>();
            var dayWeights = new Dictionary<DateTime, double>();
            for (var day = start.Date; day < end.Date.AddDays(1); day = day.AddDays(1))
            {
                var dayStart = day;
                var dayEnd = day.AddDays(1);
                var present = rolls.Where(r => r.Overlaps(dayStart, dayEnd)).ToList();
                dayCounts[dayStart] = present.Count;
                dayWeights[dayStart] = present.Sum(r => r.Weight);
            }
            if (!dayCounts.Any()) return new RollsPeriodDayExtrema();
            var minCountDay = dayCounts.OrderBy(kv => kv.Value).First().Key;
            var maxCountDay = dayCounts.OrderByDescending(kv => kv.Value).First().Key;
            var minWeightDay = dayWeights.OrderBy(kv => kv.Value).First().Key;
            var maxWeightDay = dayWeights.OrderByDescending(kv => kv.Value).First().Key;
            return new RollsPeriodDayExtrema
            {
                DayWithMinCount = minCountDay,
                DayWithMaxCount = maxCountDay,
                DayWithMinTotalWeight = minWeightDay,
                DayWithMaxTotalWeight = maxWeightDay
            };
        }
    }
}
