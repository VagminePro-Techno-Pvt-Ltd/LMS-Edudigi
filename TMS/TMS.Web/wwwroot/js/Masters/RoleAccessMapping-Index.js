$(function () {
    $(document).on('click', '#btnUpdateRoleAccessMapping', updateRoleAccessMapping);
});

function updateRoleAccessMapping() {
    var checkedItems = $("#tblRoleAccessMappings tbody input:checked");
    var items = [];
    $(checkedItems).each(function (i, chk) {
        items.push({
            RoleId: $(chk).attr('data-roleid'),
            TypeId: $(chk).attr('data-typeid'),
            SubTypeId: $(chk).attr('data-subtypeid')
        });
    });

    fcmlApp.postAjaxRequest(updateRoleAccessMappingUrl,
        { model: items },
        function (response) {
            alert(response.message);
        }
    );
}