$(function () {
    $(document).on('click', '#btnSearchRolePremissions', searchRolePermissions);
    $(document).on('click', '#btnUpdateRolePermission', updateRolePermissions);
    $(document).on('change', '#RoleId', function () {
        $("#divPermissionsContainer").html('');
    });
    $(document).on('click', 'input.chkView', function () {
        if (!$(this).is(':checked')) {
            $(this).closest('tr').find('input.chkAdd,input.chkEdit').prop('checked', false);
        }
    });
    $(document).on('click', 'input.chkAdd', function () {
        if ($(this).is(':checked')) {
            $(this).closest('tr').find('input.chkView').prop('checked', true);
        }
        else {
            $(this).closest('tr').find('input.chkEdit').prop('checked', false);
        }
    });
    $(document).on('click', 'input.chkEdit', function () {
        if ($(this).is(':checked')) {
            $(this).closest('tr').find('input.chkView,input.chkAdd').prop('checked', true);
        }
    });
});

function searchRolePermissions() {
    var roleId = $('#RoleId').val();
    if (!roleId) {
        alert('Please select Role');
        return;
    }
    applicationUtil.getAjaxRequest(getRolePermissionsUrl,
        { roleId: roleId },
        function (response) {
            $("#divPermissionsContainer").html(response);
        }
    )
}

function updateRolePermissions() {
    var items = [];
    var roleId = $('#RoleId').val();
    $('#tblRolePermissions tbody tr').each(function (i, tr) {
        items.push({
            RoleId: $(tr).attr('data-roleid'),
            FormId: $(tr).attr('data-fromid'),
            View: $(tr).find('input[datatype="View"]').is(':checked'),
            Add: $(tr).find('input[datatype="Add"]').is(':checked'),
            Edit: $(tr).find('input[datatype="Edit"]').is(':checked'),
        });
    });
    applicationUtil.postAjaxRequest(updateRolePermissionsUrl,
        { model: items, roleId: roleId},
        function (response) {
            alert(response.message);
        }
    );
}