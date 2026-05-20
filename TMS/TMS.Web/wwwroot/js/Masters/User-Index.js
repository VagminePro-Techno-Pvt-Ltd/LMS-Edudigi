$(function () {
    $(document).on('change', "#LocationId", loadDepartment);
    $(document).on('change', "#DepartmentId", loadDivision);
    $(document).on('change', "#DivisionId", loadDesignation);
});

// ── Role filter: read from hidden field set by server ──
var _roleFilter = '';
$(function () {
    _roleFilter = $('#hdRoleFilter').val() || '';
});

getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },
        { title: "Role", "data": "role.nameStatusHtml", "autoWidth": true },
        { title: "Name", "data": "name", "autoWidth": true },
        { title: "Email", "data": "email", "autoWidth": true },
        { title: "Phone", "data": "contactNo", "autoWidth": true },
        { title: "Status", "data": "isActiveStrHtml", "autoWidth": true },
        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },
        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}

getColumnOrder = function () {
    return [[1, 'asc'], [2, 'asc']];
}

// ── Pass roleFilter to server with every DataTable AJAX request ──
getFilters = function (data) {
    data.roleFilter = _roleFilter;
    return data;
}

function loadDepartment() {
    var id = $(this).val();
    loadDropdownList(getDepartmentsURL, '#DepartmentId', id);
    $('#DivisionId,#DesignationId').html('');
}
function loadDivision() {
    var id = $(this).val();
    loadDropdownList(getDivisionURL, '#DivisionId', id);
    $('#DesignationId').html('')
}
function loadDesignation() {
    var id = $(this).val();
    loadDropdownList(getDesignationURL, '#DesignationId', id);
}