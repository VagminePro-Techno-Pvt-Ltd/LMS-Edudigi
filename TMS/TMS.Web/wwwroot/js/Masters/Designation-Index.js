$(function () {
    $(document).on('change', "#Division_Department_LocationId", loadDepartment);
    $(document).on('change', "#Division_DepartmentId", loadDivision);
});
getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },
        { title: "Location", "data": "division.department.location.nameStatusHtml", "autoWidth": true },
        { title: "Designation Code", "data": "designationCode", "autoWidth": true },

        { title: "Department", "data": "division.department.nameStatusHtml", "autoWidth": true },
        { title: "Division", "data": "division.nameStatusHtml", "autoWidth": true },
        { title: "Name", "data": "name", "autoWidth": true },
        { title: "Description", "data": "description", "autoWidth": true, className: "text-wrap" },
        { title: "Status", "data": "isActiveStrHtml", "autoWidth": true },
        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },
        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}

getColumnOrder = function () {
    return [[1, 'asc'], [2, 'asc'], [3, 'asc'], [4, 'asc']];
}
function loadDepartment() {
    var id = $(this).val();
    $('#DivisionId').html();
    loadDropdownList(getDepartmentOptionUrl, '#Division_DepartmentId', id);
}
function loadDivision() {
    var id = $(this).val();
    loadDropdownList(getDivisionOptionUrl, '#DivisionId', id);
}