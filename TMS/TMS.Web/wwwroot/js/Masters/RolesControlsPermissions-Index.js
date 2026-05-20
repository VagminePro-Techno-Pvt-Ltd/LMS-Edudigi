$(function () {
    $(document).on('click', '#btnSearchRolePremissions', searchRolePermissions);
    $(document).on('click', '#btnUpdateRolePermission', updateRolePermissions);
    $(document).on('change', '#RoleId,#FormId', function () {
        $("#divPermissionsContainer").html('');
    });
});

function searchRolePermissions() {
    var roleId = $('#RoleId').val();
    if (!roleId) {
        applicationUtil.showToast('Alert', 'Please select Role');
        return;
    }
    var formId = $('#FormId').val();
    if (!formId) {
        applicationUtil.showToast('Alert', 'Please select Form');
        return;
    }
    applicationUtil.getAjaxRequest(getRoleControlsPermissionsUrl,
        { roleId: roleId, formId: formId },
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
            ControlId: $(tr).attr('data-controlid'),
            FormId: $(tr).attr('data-fromid'),
            IsVisible: $(tr).find('input[datatype="Visible"]').is(':checked')
        });
    });
    applicationUtil.postAjaxRequest(updateRoleControlsPermissionsUrl,
        { model: items, roleId: roleId },
        function (response) {
            applicationUtil.showToast('Alert', response.message);
        }
    );
}