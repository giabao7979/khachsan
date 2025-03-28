﻿using System.ComponentModel.DataAnnotations;
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
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Range(1, 5)]
        public int? StarRating { get; set; }

        [Required]
        public int Beds { get; set; }

        [Required]
        public int Bathrooms { get; set; }
    
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression(@"^(Đang ở|Bảo trì|Còn trống)$")]
        public string Status { get; set; } = "Còn trống";

        public string? Image { get; set; } 

        public int? RoomTypeID { get; set; }

        [ForeignKey("RoomTypeID")]
        public RoomType? RoomType { get; set; }

        public int ViewCount { get; set; } = 0;

        // Navigation property tới các ảnh của phòng
        public virtual ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}