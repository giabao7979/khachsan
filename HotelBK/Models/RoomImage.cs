using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBK.Models
{
    public class RoomImage
    {
        [Key]
        public int ImageID { get; set; }

        public int RoomID { get; set; }

        [Required]
        [StringLength(255)]
        public string ImagePath { get; set; }

        public bool IsMainImage { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("RoomID")]
        public Room Room { get; set; }
    }
}