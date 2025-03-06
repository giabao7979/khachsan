using System.ComponentModel.DataAnnotations;

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

        public int? StarRating { get; set; }

        [Required]
        public int Beds { get; set; }

        [Required]
        public int Bathrooms { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string? Image { get; set; } // Lưu đường dẫn ảnh

        public int RoomTypeID { get; set; } // Foreign key for RoomType
        public RoomType RoomType { get; set; }
    }
}
