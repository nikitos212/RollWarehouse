using System;

namespace RollWarehouse.Domain.Entities
{
    public class Roll
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public double Length { get; set; }
        public double Weight { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public DateTime? DateRemoved { get; set; }
        public bool IsActiveAt(DateTime moment)
        {
            return DateAdded <= moment && (DateRemoved == null || DateRemoved > moment);
        }
        public bool Overlaps(DateTime start, DateTime end)
        {
            var removed = DateRemoved ?? DateTime.MaxValue;
            return DateAdded < end && removed > start;
        }
    }
}
