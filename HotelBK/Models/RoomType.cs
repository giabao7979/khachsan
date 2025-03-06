namespace HotelBK.Models
{
    public class RoomType
    {
        public int RoomTypeID { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }

        // Navigation property for related rooms
        public ICollection<Room> Rooms { get; set; }
    }
}
