using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBK.Models
{
    public class RoomType
    {
        [Key]
        public int RoomTypeID { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }

        // Navigation property
        public virtual ICollection<Room> Rooms { get; set; }
    }
}