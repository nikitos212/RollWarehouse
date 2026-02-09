using System.ComponentModel.DataAnnotations;

namespace RollWarehouse.Presentation.Http.Models
{
    public class CreateRollRequest
    {
        [Required]
        public double? Length { get; set; }

        [Required]
        public double? Weight { get; set; }
    }
}