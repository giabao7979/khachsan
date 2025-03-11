using HotelBK.Data;
using HotelBK.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Controllers
{
    public class RoomController : Controller
    {
        private readonly HotelContext _context;

        public RoomController(HotelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .ToListAsync();
            return View(rooms);
        }
        public async Task<IActionResult> Detail(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }
    }
}