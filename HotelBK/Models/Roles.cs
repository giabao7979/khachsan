using System.ComponentModel.DataAnnotations;

namespace HotelBK.Models
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required, MaxLength(50)]
        public string RoleName { get; set; }
    }
}
