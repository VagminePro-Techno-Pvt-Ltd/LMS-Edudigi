$(function () {
    $(document).on('change', "#Department_LocationId", loadDepartment);
});
getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },
        { title: "Location", "data": "department.location.nameStatusHtml", "autoWidth": true },
        { title: "Division Code", "data": "divisionCode", "autoWidth": true },
        { title: "Department", "data": "department.nameStatusHtml", "autoWidth": true },
        { title: "Name", "data": "name", "autoWidth": true },
        { title: "Description", "data": "description", "autoWidth": true, className: "text-wrap" },
        { title: "Status", "data": "isActiveStrHtml", "autoWidth": true },
        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },
        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}

getColumnOrder = function () {
    return [[1, 'asc'], [2, 'asc'], [3, 'asc']];
}
function loadDepartment() {
    var id = $(this).val();
    loadDropdownList(getDepartmentOptionUrl, '#DepartmentId', id);
}