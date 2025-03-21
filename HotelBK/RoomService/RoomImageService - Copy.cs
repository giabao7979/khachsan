using HotelBK.Data;
using HotelBK.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBK.Services
{
    public class RoomImageService : IRoomImageService
    {
        private readonly HotelContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public RoomImageService(HotelContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IEnumerable<RoomImage>> GetRoomImagesByRoomId(int roomId)
        {
            return await _context.RoomImages
                .Where(ri => ri.RoomID == roomId)
                .OrderBy(ri => ri.DisplayOrder)
                .ToListAsync();
        }

        public async Task<RoomImage> GetMainImageByRoomId(int roomId)
        {
            return await _context.RoomImages
                .FirstOrDefaultAsync(ri => ri.RoomID == roomId && ri.IsMainImage);
        }

        public async Task<RoomImage> AddRoomImage(RoomImage roomImage, IFormFile imageFile)
        {
            // Xử lý upload hình ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                // Tạo tên file duy nhất
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);

                // Đảm bảo thư mục tồn tại
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "rooms");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Lưu file
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn ảnh
                roomImage.ImagePath = "/images/rooms/" + fileName;

                // Kiểm tra xem đã có ảnh chính chưa
                bool hasMainImage = await _context.RoomImages
                    .AnyAsync(ri => ri.RoomID == roomImage.RoomID && ri.IsMainImage);

                // Nếu chưa có ảnh chính, đặt ảnh này làm ảnh chính
                if (!hasMainImage)
                {
                    roomImage.IsMainImage = true;
                }

                // Lấy DisplayOrder cao nhất hiện tại
                var maxOrder = await _context.RoomImages
                    .Where(ri => ri.RoomID == roomImage.RoomID)
                    .MaxAsync(ri => (int?)ri.DisplayOrder) ?? 0;

                roomImage.DisplayOrder = maxOrder + 1;

                _context.RoomImages.Add(roomImage);
                await _context.SaveChangesAsync();

                // Nếu đây là ảnh chính, cập nhật vào bảng Room
                if (roomImage.IsMainImage)
                {
                    var room = await _context.Rooms.FindAsync(roomImage.RoomID);
                    if (room != null)
                    {
                        room.Image = roomImage.ImagePath;
                        await _context.SaveChangesAsync();
                    }
                }

                return roomImage;
            }

            return null;
        }

        public async Task<bool> DeleteRoomImage(int imageId)
        {
            var roomImage = await _context.RoomImages.FindAsync(imageId);
            if (roomImage == null)
                return false;

            // Xóa file ảnh nếu tồn tại
            string webRootPath = _hostEnvironment.WebRootPath;
            string imagePath = roomImage.ImagePath.TrimStart('/');
            string fullPath = Path.Combine(webRootPath, imagePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            _context.RoomImages.Remove(roomImage);

            // Nếu xóa ảnh chính, cần cập nhật ảnh chính mới
            if (roomImage.IsMainImage)
            {
                var nextMainImage = await _context.RoomImages
                    .Where(ri => ri.RoomID == roomImage.RoomID && ri.ImageID != imageId)
                    .OrderBy(ri => ri.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (nextMainImage != null)
                {
                    nextMainImage.IsMainImage = true;

                    // Cập nhật trong bảng Room
                    var room = await _context.Rooms.FindAsync(roomImage.RoomID);
                    if (room != null)
                    {
                        room.Image = nextMainImage.ImagePath;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetAsMainImage(int imageId)
        {
            var roomImage = await _context.RoomImages.FindAsync(imageId);
            if (roomImage == null)
                return false;

            // Đặt tất cả các ảnh khác của phòng này về không phải ảnh chính
            var otherImages = await _context.RoomImages
                .Where(ri => ri.RoomID == roomImage.RoomID && ri.ImageID != imageId)
                .ToListAsync();

            foreach (var image in otherImages)
            {
                image.IsMainImage = false;
            }

            // Đặt ảnh hiện tại làm ảnh chính
            roomImage.IsMainImage = true;

            // Cập nhật trong bảng Room
            var room = await _context.Rooms.FindAsync(roomImage.RoomID);
            if (room != null)
            {
                room.Image = roomImage.ImagePath;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDisplayOrder(int imageId, int newOrder)
        {
            var roomImage = await _context.RoomImages.FindAsync(imageId);
            if (roomImage == null)
                return false;

            roomImage.DisplayOrder = newOrder;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}