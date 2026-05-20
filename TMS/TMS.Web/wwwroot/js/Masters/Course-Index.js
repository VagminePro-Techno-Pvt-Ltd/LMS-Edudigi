getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },
        {
            title: "Course Code", "data": "courseCode", "autoWidth": true,
            render: function (data) {
                return '<span style="padding:4px 12px;border-radius:8px;background:#f1f5f9;color:#475569;font-weight:700;font-size:0.82rem;font-family:monospace;">' + (data || '—') + '</span>';
            }
        },
        {
            title: "Program", "data": "courseCategory", "autoWidth": true,
            render: function (data) {
                if (!data || !data.name) return '<span style="color:#94a3b8;">—</span>';
                return '<div style="display:flex;align-items:center;gap:8px;">' +
                    '<div style="width:28px;height:28px;border-radius:8px;background:linear-gradient(135deg,#6366f1,#8b5cf6);color:#fff;display:flex;align-items:center;justify-content:center;font-size:11px;flex-shrink:0;">🎓</div>' +
                    '<span style="font-weight:600;color:#1e293b;font-size:0.88rem;">' + data.name + '</span></div>';
            }
        },
        {
            title: "Semester", "data": "semester", "autoWidth": true, className: "text-center",
            render: function (data) {
                if (!data || !data.name) return '<span style="color:#94a3b8;">—</span>';
                return '<span style="padding:4px 14px;border-radius:8px;background:linear-gradient(135deg,#ede9fe,#e0e7ff);color:#4f46e5;font-weight:700;font-size:0.82rem;">' + data.name + '</span>';
            }
        },
        {
            title: "Course Name", "data": "name", "autoWidth": true,
            render: function (data) {
                return '<div style="display:flex;align-items:center;gap:8px;">' +
                    '<i class="fa-solid fa-book" style="color:#8b5cf6;font-size:12px;"></i>' +
                    '<strong style="color:#0f172a;">' + (data || '') + '</strong></div>';
            }
        },
        {
            title: "Credits", "data": "noofCredit", "autoWidth": true, className: "text-center",
            render: function (data) {
                if (!data) return '<span style="color:#94a3b8;">—</span>';
                return '<span style="padding:3px 10px;border-radius:6px;background:#fef3c7;color:#b45309;font-weight:700;font-size:0.82rem;">⭐ ' + data + '</span>';
            }
        },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}
$(function () {
    $(document).on('click', 'input[type=checkbox].chkAll', function () {
        $('input[type=checkbox].chkItem').prop('checked', false);
    });
    $(document).on('click', 'input[type=checkbox].chkItem', function () {
        $('input[type=checkbox].chkAll').prop('checked', false);
    });
});

validateForm = function () {
    
    return true;
}