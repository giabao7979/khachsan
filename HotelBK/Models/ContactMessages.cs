using System;
using System.ComponentModel.DataAnnotations;

namespace HotelBK.Models
{
    public class ContactMessages
    {
        [Key]
        public int ContactID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chủ đề")]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn")]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; } = false;
    }
}