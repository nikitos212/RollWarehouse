using System;

namespace RollWarehouse.Presentation.Http.Models
{
    public class QueryRequest
    {
        public Guid? IdMin { get; set; }
        public Guid? IdMax { get; set; }
        public double? WeightMin { get; set; }
        public double? WeightMax { get; set; }
        public double? LengthMin { get; set; }
        public double? LengthMax { get; set; }
        public DateTime? DateAddedFrom { get; set; }
        public DateTime? DateAddedTo { get; set; }
        public DateTime? DateRemovedFrom { get; set; }
        public DateTime? DateRemovedTo { get; set; }
    }
}
