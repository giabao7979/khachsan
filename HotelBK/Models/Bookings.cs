using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBK.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(15)]
        public string Phone { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Range(1, 10)]
        [Display(Name = "Số lượng người")]
        public int RoomCount { get; set; }

        [Required(ErrorMessage = "Phòng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng")]
        [ForeignKey("Room")]
        public int RoomID { get; set; }
        public virtual Room? Room { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(500)]
        [Display(Name = "Yêu cầu đặc biệt")]
        public string? SpecialRequest { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
    }
}