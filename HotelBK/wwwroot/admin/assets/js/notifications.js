// Hàm cập nhật thông báo khi trang được tải và mỗi 60 giây
document.addEventListener('DOMContentLoaded', function () {
    // Lấy thông báo ban đầu khi trang tải
    fetchBookingNotifications();
    fetchContactNotifications();

    // Cập nhật thông báo mỗi 60 giây
    setInterval(function () {
        fetchBookingNotifications();
        fetchContactNotifications();
    }, 60000);

    // Xử lý sự kiện đánh dấu đã đọc tất cả thông báo đặt phòng
    document.getElementById('markAllRoomRead').addEventListener('click', function (e) {
        e.preventDefault();
        markAllBookingsAsRead();
    });

    // Xử lý sự kiện đánh dấu đã đọc tất cả tin nhắn liên hệ
    document.getElementById('markAllMessageRead').addEventListener('click', function (e) {
        e.preventDefault();
        markAllContactsAsRead();
    });

    // Chuyển hướng đến trang quản lý đặt phòng
    document.getElementById('viewAllRoomNotifications').addEventListener('click', function (e) {
        e.preventDefault();
        window.location.href = '/Admin/Bookings';
    });

    // Xử lý liên kết đến trang liên hệ
    document.getElementById('viewAllMessageNotifications').addEventListener('click', function (e) {
        e.preventDefault();
        window.location.href = '/Admin/ContactMessages';
    });
});

// Lấy thông báo đặt phòng mới
function fetchBookingNotifications() {
    fetch('/Admin/Notifications/GetBookingNotifications')
        .then(response => response.json())
        .then(data => {
            updateBookingNotifications(data);
        })
        .catch(error => console.error('Lỗi khi lấy thông báo đặt phòng:', error));
}

// Lấy thông báo tin nhắn liên hệ mới
function fetchContactNotifications() {
    fetch('/Admin/Notifications/GetContactNotifications')
        .then(response => response.json())
        .then(data => {
            updateContactNotifications(data);
        })
        .catch(error => console.error('Lỗi khi lấy thông báo liên hệ:', error));
}

// Cập nhật giao diện thông báo đặt phòng
function updateBookingNotifications(data) {
    // Cập nhật số lượng thông báo
    document.querySelector('.notification-count-room').textContent = data.count > 0 ? data.count : '0';

    // Lấy container cho các mục thông báo
    const container = document.getElementById('roomNotificationItems');
    container.innerHTML = '';

    if (data.items.length === 0) {
        container.innerHTML = `
            <li class="notification-item">
                <div class="d-flex">
                    <div>
                        <p>Không có thông báo mới</p>
                    </div>
                </div>
            </li>
            <li><hr class="dropdown-divider"></li>
        `;
        return;
    }

    // Thêm các mục thông báo
    data.items.forEach(item => {
        const notificationHtml = `
            <li class="notification-item" data-id="${item.bookingID}">
                <i class="bi bi-info-circle text-primary"></i>
                <div>
                    <h4>${item.customerName}</h4>
                    <p>Đã đặt phòng mới</p>
                    <p>${item.timeAgo}</p>
                </div>
            </li>
            <li><hr class="dropdown-divider"></li>
        `;
        container.innerHTML += notificationHtml;
    });

    // Thêm sự kiện click cho mỗi thông báo
    document.querySelectorAll('#roomNotificationItems .notification-item').forEach(item => {
        item.addEventListener('click', function () {
            const bookingId = this.getAttribute('data-id');
            markBookingAsRead(bookingId);
            window.location.href = `/Admin/Bookings/View/${bookingId}`;
        });
    });
}

// Cập nhật giao diện thông báo tin nhắn liên hệ
function updateContactNotifications(data) {
    // Cập nhật số lượng thông báo
    document.querySelector('.notification-count-message').textContent = data.count > 0 ? data.count : '0';

    // Lấy container cho các mục thông báo
    const container = document.getElementById('messageNotificationItems');
    container.innerHTML = '';

    if (data.items.length === 0) {
        container.innerHTML = `
            <li class="message-item">
                <div class="d-flex">
                    <div>
                        <p>Không có tin nhắn mới</p>
                    </div>
                </div>
            </li>
            <li><hr class="dropdown-divider"></li>
        `;
        return;
    }

    // Thêm các mục thông báo
    data.items.forEach(item => {
        const notificationHtml = `
            <li class="message-item" data-id="${item.contactID}">
                <i class="bi bi-envelope text-success"></i>
                <div>
                    <h4>${item.name}</h4>
                    <p>${item.subject}</p>
                    <p>${item.timeAgo}</p>
                </div>
            </li>
            <li><hr class="dropdown-divider"></li>
        `;
        container.innerHTML += notificationHtml;
    });

    // Thêm sự kiện click cho mỗi thông báo
    document.querySelectorAll('#messageNotificationItems .message-item').forEach(item => {
        item.addEventListener('click', function () {
            const contactId = this.getAttribute('data-id');
            markContactAsRead(contactId);
            window.location.href = `/Admin/ContactMessages/View/${contactId}`;
        });
    });
}

// Đánh dấu tất cả các thông báo đặt phòng là đã đọc
function markAllBookingsAsRead() {
    fetch('/Admin/Notifications/MarkAllBookingsAsRead', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                document.querySelector('.notification-count-room').textContent = '0';
                document.getElementById('roomNotificationItems').innerHTML = `
                <li class="notification-item">
                    <div class="d-flex">
                        <div>
                            <p>Không có thông báo mới</p>
                        </div>
                    </div>
                </li>
                <li><hr class="dropdown-divider"></li>
            `;
            }
        })
        .catch(error => console.error('Lỗi khi đánh dấu đọc tất cả đặt phòng:', error));
}

// Đánh dấu tất cả các thông báo tin nhắn liên hệ là đã đọc
function markAllContactsAsRead() {
    fetch('/Admin/Notifications/MarkAllContactsAsRead', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                document.querySelector('.notification-count-message').textContent = '0';
                document.getElementById('messageNotificationItems').innerHTML = `
                <li class="message-item">
                    <div class="d-flex">
                        <div>
                            <p>Không có tin nhắn mới</p>
                        </div>
                    </div>
                </li>
                <li><hr class="dropdown-divider"></li>
            `;
            }
        })
        .catch(error => console.error('Lỗi khi đánh dấu đọc tất cả tin nhắn:', error));
}

// Đánh dấu một thông báo đặt phòng là đã đọc
function markBookingAsRead(id) {
    fetch(`/Admin/Notifications/MarkBookingAsRead?id=${id}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .catch(error => console.error('Lỗi khi đánh dấu đọc thông báo đặt phòng:', error));
}

// Đánh dấu một thông báo tin nhắn liên hệ là đã đọc
function markContactAsRead(id) {
    fetch(`/Admin/Notifications/MarkContactAsRead?id=${id}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .catch(error => console.error('Lỗi khi đánh dấu đọc thông báo tin nhắn:', error));
}