using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBK.Models
{
    public class Service
    {
        [Key]
        public int ServiceID { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [ForeignKey("User")]
        public int CreatedBy { get; set; }
        public User User { get; set; }
    }
}