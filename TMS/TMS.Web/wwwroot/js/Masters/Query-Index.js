getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },
        { title: "Course", "data": "course.nameStatusHtml", "autoWidth": true },
        { title: "Description", "data": "description", "autoWidth": true, className: "text-wrap" },
        { title: "Status", "data": "isActiveStrHtml", "autoWidth": true },
        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },
        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}
getColumnOrder = function () {
    return [[1, 'asc'], [2, 'asc']];
}
$(function () {
    bindImagePreview('#File', '#imgFile');
});