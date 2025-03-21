using Microsoft.AspNetCore.Mvc;
using HotelBK.Models;
using HotelBK.Data;
using System;
using System.Threading.Tasks;

namespace HotelBK.Controllers
{
    public class ContactController : Controller
    {
        private readonly HotelContext _context;

        public ContactController(HotelContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(new ContactMessages());
        }

        [HttpPost]
        public async Task<IActionResult> SubmitContact(ContactMessages model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            // Thêm thời gian tạo
            model.CreatedAt = DateTime.Now;
            model.IsRead = false;

            // Lưu tin nhắn liên hệ vào database
            _context.ContactMessages.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tin nhắn của bạn đã được gửi thành công. Chúng tôi sẽ liên hệ lại trong thời gian sớm nhất!";
            return RedirectToAction("Index");
        }
    }
}