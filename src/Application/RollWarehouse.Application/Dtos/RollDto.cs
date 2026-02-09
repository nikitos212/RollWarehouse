using System;

namespace RollWarehouse.Application.Dtos
{
    public class RollDto
    {
        public Guid Id { get; set; }
        public double Length { get; set; }
        public double Weight { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime? DateRemoved { get; set; }
    }
}
