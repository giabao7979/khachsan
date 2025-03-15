$(document).ready(function () {
    // Xử lý ngày nhận/trả phòng
    $("#searchForm input[name='checkIn'], #searchForm input[name='checkOut']").change(function () {
        var checkIn = new Date($("#searchForm input[name='checkIn']").val());
        var checkOut = new Date($("#searchForm input[name='checkOut']").val());
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        // Xử lý ngày nhận phòng không được trong quá khứ
        if (checkIn < today) {
            $("#searchForm input[name='checkIn']").val(today.toISOString().split('T')[0]);
            checkIn = today;
        }

        // Xử lý ngày trả phòng phải sau ngày nhận
        if (checkOut <= checkIn) {
            var newCheckOut = new Date(checkIn);
            newCheckOut.setDate(newCheckOut.getDate() + 1);
            $("#searchForm input[name='checkOut']").val(newCheckOut.toISOString().split('T')[0]);
        }
    });

    // Validation form trước khi submit
    $("#searchForm").submit(function (e) {
        var checkIn = new Date($("#searchForm input[name='checkIn']").val());
        var checkOut = new Date($("#searchForm input[name='checkOut']").val());
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        if (checkIn < today) {
            alert("Ngày nhận phòng không thể trong quá khứ!");
            e.preventDefault();
            return false;
        }

        if (checkOut <= checkIn) {
            alert("Ngày trả phòng phải sau ngày nhận phòng!");
            e.preventDefault();
            return false;
        }

        // Kiểm tra khoảng cách giữa ngày nhận và trả phòng
        var daysDiff = Math.floor((checkOut - checkIn) / (1000 * 60 * 60 * 24));
        if (daysDiff > 30) {
            if (!confirm("Bạn đang tìm phòng cho thời gian dài (" + daysDiff + " ngày). Bạn có chắc chắn muốn tiếp tục?")) {
                e.preventDefault();
                return false;
            }
        }

        return true;
    });
});