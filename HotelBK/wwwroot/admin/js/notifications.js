
$(document).ready(function () {
    // Khởi tạo thông báo từ localStorage (nếu có)
    initNotifications();

    // Cập nhật số lượng thông báo
    updateNotificationCounts();

    // Xử lý sự kiện đánh dấu đã đọc
    $("#markAllRoomRead").click(function (e) {
        e.preventDefault();
        markAllAsRead('room');
        updateNotificationCounts();
    });

    $("#markAllMessageRead").click(function (e) {
        e.preventDefault();
        markAllAsRead('message');
        updateNotificationCounts();
    });

    // Xử lý sự kiện khi nhấp vào một thông báo
    $(document).on('click', '.notification-item', function () {
        const type = $(this).data('type');
        const id = $(this).data('id');
        markAsRead(type, id);
        updateNotificationCounts();
    });
});

// Khởi tạo thông báo
function initNotifications() {
    // Lấy thông báo từ localStorage
    let roomNotifications = JSON.parse(localStorage.getItem('roomNotifications')) || [];
    let messageNotifications = JSON.parse(localStorage.getItem('messageNotifications')) || [];

    // Hiển thị thông báo phòng
    renderNotifications('room', roomNotifications);

    // Hiển thị thông báo tin nhắn
    renderNotifications('message', messageNotifications);
}

// Thêm một thông báo mới
function addNotification(type, data) {
    // Lấy danh sách thông báo hiện tại
    const storageKey = type === 'room' ? 'roomNotifications' : 'messageNotifications';
    let notifications = JSON.parse(localStorage.getItem(storageKey)) || [];

    // Thêm thông báo mới vào đầu danh sách
    notifications.unshift({
        id: Date.now(),
        title: data.title,
        message: data.message,
        read: false,
        time: new Date().toISOString(),
        data: data.data || {}
    });

    // Giới hạn số lượng thông báo (giữ lại 20 thông báo mới nhất)
    if (notifications.length > 20) {
        notifications.pop();
    }

    // Lưu vào localStorage
    localStorage.setItem(storageKey, JSON.stringify(notifications));

    // Cập nhật giao diện
    renderNotifications(type, notifications);
    updateNotificationCounts();
}

// Đánh dấu một thông báo đã đọc
function markAsRead(type, id) {
    const storageKey = type === 'room' ? 'roomNotifications' : 'messageNotifications';
    let notifications = JSON.parse(localStorage.getItem(storageKey)) || [];

    // Tìm và cập nhật trạng thái đã đọc
    const index = notifications.findIndex(item => item.id === id);
    if (index !== -1) {
        notifications[index].read = true;
        localStorage.setItem(storageKey, JSON.stringify(notifications));

        // Cập nhật giao diện
        renderNotifications(type, notifications);
    }
}

// Đánh dấu tất cả thông báo đã đọc
function markAllAsRead(type) {
    const storageKey = type === 'room' ? 'roomNotifications' : 'messageNotifications';
    let notifications = JSON.parse(localStorage.getItem(storageKey)) || [];

    // Đánh dấu tất cả đã đọc
    notifications = notifications.map(item => ({ ...item, read: true }));
    localStorage.setItem(storageKey, JSON.stringify(notifications));

    // Cập nhật giao diện
    renderNotifications(type, notifications);
}

// Hiển thị thông báo lên giao diện
function renderNotifications(type, notifications) {
    const containerId = type === 'room' ? 'roomNotificationItems' : 'messageNotificationItems';
    const container = $(`#${containerId}`);

    // Xóa các thông báo cũ
    container.empty();

    // Nếu không có thông báo
    if (notifications.length === 0) {
        container.html('<li class="dropdown-item"><div class="text-center">Không có thông báo mới</div></li>');
        return;
    }

    // Hiển thị tối đa 5 thông báo mới nhất
    const displayCount = Math.min(notifications.length, 5);
    for (let i = 0; i < displayCount; i++) {
        const notification = notifications[i];
        const time = new Date(notification.time);
        const timeStr = `${time.getHours()}:${String(time.getMinutes()).padStart(2, '0')} ${time.getDate()}/${time.getMonth() + 1}/${time.getFullYear()}`;

        const iconClass = type === 'room'
            ? 'bi bi-exclamation-circle text-warning'
            : 'bi bi-envelope text-primary';

        const html = `
            <li class="notification-item" data-type="${type}" data-id="${notification.id}">
                <i class="${iconClass}"></i>
                <div>
                    <h4>${notification.title}</h4>
                    <p>${notification.message}</p>
                    <p>${timeStr}</p>
                </div>
            </li>
            <li><hr class="dropdown-divider"></li>
        `;

        container.append(html);
    }
}

// Cập nhật số lượng thông báo chưa đọc
function updateNotificationCounts() {
    const roomNotifications = JSON.parse(localStorage.getItem('roomNotifications')) || [];
    const messageNotifications = JSON.parse(localStorage.getItem('messageNotifications')) || [];

    const unreadRoomCount = roomNotifications.filter(item => !item.read).length;
    const unreadMessageCount = messageNotifications.filter(item => !item.read).length;

    // Cập nhật số lượng hiển thị
    $('.notification-count-room').text(unreadRoomCount > 0 ? unreadRoomCount : '0');
    $('.notification-count-message').text(unreadMessageCount > 0 ? unreadMessageCount : '0');

    // Ẩn/hiện badge nếu không có thông báo
    if (unreadRoomCount === 0) {
        $('.notification-count-room').hide();
    } else {
        $('.notification-count-room').show();
    }

    if (unreadMessageCount === 0) {
        $('.notification-count-message').hide();
    } else {
        $('.notification-count-message').show();
    }
}

// Thêm thông báo phòng mới (hàm này được gọi khi tạo phòng mới)
function addRoomNotification(roomName, roomId) {
    addNotification('room', {
        title: 'Phòng mới',
        message: `Phòng "${roomName}" đã được thêm vào hệ thống`,
        data: { roomId }
    });
}

// Thêm thông báo tin nhắn mới (hàm này được gọi khi có tin nhắn liên hệ mới)
function addContactNotification(name, contactId) {
    addNotification('message', {
        title: 'Tin nhắn liên hệ mới',
        message: `Có tin nhắn mới từ "${name}"`,
        data: { contactId }
    });
}