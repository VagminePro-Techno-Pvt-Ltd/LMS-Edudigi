$(function () {
    $(document).on('change', "#LocationId", loadDepartment);
    $(document).on('change', "#DepartmentId", loadDivision);
    $(document).on('change', "#DivisionId", loadDesignation);
});

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
            title: "Role",
            data: "role.nameStatusHtml",
            autoWidth: true
        },
        {
            title: "Name",
            data: "name",
            autoWidth: true
        },
        {
            title: "Email",
            data: "email",
            autoWidth: true
        },
        {
            title: "Phone",
            data: "contactNo",
            autoWidth: true
        },
        {
            title: "Status",
            data: "isActiveStrHtml",
            autoWidth: true
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

getColumnOrder = function () {
    return [[1, 'asc'], [2, 'asc']];
};

/*
 * Read the role filter directly from the server-rendered hidden field
 * every time DataTables prepares an AJAX request.
 *
 * Do not cache this value inside a document-ready callback because the
 * shared DataTable initialization can run before that callback and send
 * the first request with an empty role filter.
 */
getFilters = function (data) {
    var roleFilter = $('#hdRoleFilter').val();

    if (roleFilter !== undefined && roleFilter !== null) {
        data.roleFilter = roleFilter.toString().trim();
    } else {
        data.roleFilter = '';
    }

    return data;
};

function loadDepartment() {
    var id = $(this).val();
    loadDropdownList(getDepartmentsURL, '#DepartmentId', id);
    $('#DivisionId,#DesignationId').html('');
}

function loadDivision() {
    var id = $(this).val();
    loadDropdownList(getDivisionURL, '#DivisionId', id);
    $('#DivisionId').html('');
}

function loadDesignation() {
    var id = $(this).val();
    loadDropdownList(getDesignationURL, '#DesignationId', id);
}