function escapeCourseCategoryHtml(value) {
    if (value === null || value === undefined) {
        return '';
    }

    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

getColumnDefination = function () {
    return [
        {
            title: "Sl. No.",
            data: "id",
            orderable: false,
            render: indexRenderer,
            className: "text-end"
        },
        {
            title: "Program Name",
            data: "name",
            autoWidth: true,
            render: function (data) {
                var safeName = escapeCourseCategoryHtml(data);

                return (
                    '<div style="display:flex;align-items:center;gap:10px;">' +
                        '<div style="' +
                            'width:36px;' +
                            'height:36px;' +
                            'border-radius:10px;' +
                            'background:linear-gradient(135deg,#6366f1,#8b5cf6);' +
                            'color:#fff;' +
                            'display:flex;' +
                            'align-items:center;' +
                            'justify-content:center;' +
                            'font-size:14px;' +
                            'flex-shrink:0;' +
                        '">' +
                            '<i class="fa-solid fa-graduation-cap"></i>' +
                        '</div>' +
                        '<div>' +
                            '<strong style="color:#0f172a;">' +
                                safeName +
                            '</strong>' +
                        '</div>' +
                    '</div>'
                );
            }
        },
        {
            title: "Semesters",
            data: "noOfSemester",
            autoWidth: true,
            className: "text-center",
            render: function (data) {
                if (data === null || data === undefined || data === '') {
                    return '<span style="color:#94a3b8;">—</span>';
                }

                var safeSemester = escapeCourseCategoryHtml(data);

                return (
                    '<span style="' +
                        'padding:4px 14px;' +
                        'border-radius:8px;' +
                        'background:linear-gradient(135deg,#ede9fe,#e0e7ff);' +
                        'color:#4f46e5;' +
                        'font-weight:700;' +
                        'font-size:0.82rem;' +
                    '">' +
                        safeSemester +
                        ' Sem' +
                    '</span>'
                );
            }
        },
        {
            title: "Description",
            data: "description",
            autoWidth: true,
            className: "text-wrap",
            render: function (data) {
                if (data === null || data === undefined || data === '') {
                    return '<span style="color:#cbd5e1;">No description</span>';
                }

                var description = String(data);

                var truncated =
                    description.length > 60
                        ? description.substring(0, 60) + '...'
                        : description;

                var safeDescription =
                    escapeCourseCategoryHtml(description);

                var safeTruncated =
                    escapeCourseCategoryHtml(truncated);

                return (
                    '<span style="color:#64748b;font-size:0.88rem;" ' +
                    'title="' + safeDescription + '">' +
                        safeTruncated +
                    '</span>'
                );
            }
        },
        {
            title: "Status",
            data: "isActive",
            autoWidth: true,
            className: "text-center",
            render: function (data) {
                if (data) {
                    return (
                        '<span style="' +
                            'padding:4px 12px;' +
                            'border-radius:20px;' +
                            'background:#dcfce7;' +
                            'color:#16a34a;' +
                            'font-weight:700;' +
                            'font-size:0.78rem;' +
                            'text-transform:uppercase;' +
                        '">' +
                            'Active' +
                        '</span>'
                    );
                }

                return (
                    '<span style="' +
                        'padding:4px 12px;' +
                        'border-radius:20px;' +
                        'background:#fee2e2;' +
                        'color:#dc2626;' +
                        'font-weight:700;' +
                        'font-size:0.78rem;' +
                        'text-transform:uppercase;' +
                    '">' +
                        'Inactive' +
                    '</span>'
                );
            }
        },
        {
            title: "Last Action By",
            data: "lastActionBy",
            autoWidth: true
        },
        {
            title: "Last Action On",
            data: "lastActionOnStr",
            autoWidth: true
        },
        {
            data: "idEnc",
            orderable: false,
            autoWidth: true,
            render: viewActionRenderer
        }
    ];
};