using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBK.Models
{
    public class Room
    {
        [Key]
        public int RoomID { get; set; }

        [Required, StringLength(100)]
        public string RoomName { get; set; }

        [Required]
        public decimal Price { get; set; }

        public int? StarRating { get; set; } // Nullable

        [Required]
        public int Beds { get; set; }

        [Required]
        public int Bathrooms { get; set; }

        [StringLength(255)]
        public string? Description { get; set; } // Nullable

        [StringLength(20)]
        public string? Status { get; set; } // Nullable

        public string? Image { get; set; } // Nullable

        public int? RoomTypeID { get; set; } // Nullable

        [ForeignKey("RoomTypeID")]
        public RoomType? RoomType { get; set; } // Nullable
    }
}
