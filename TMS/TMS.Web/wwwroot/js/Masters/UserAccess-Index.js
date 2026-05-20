$(function () {
    $(document).on('click', '#btnSearchLocations', searchLocations);
    $(document).on('click', '#btnUpdateMapping', updateLocationMapping);
    $(document).on('change', '#UserId', function () {
        $("#divLocationContainer").html('');
    });
    $(document).on('change', 'input.chkSelectAll', updateSelectAllItems);
    $(document).on('change', 'input.chkSelectAllDefault', updateSelectAllDefaultItems);
    $(document).on('change', 'input.chkShow', function () { updateSelectAllChk($(this)); });
    $(document).on('change', 'input.chkDefault', function () { updateSelectAllDefaultChk($(this)); });
});

function searchLocations() {
    var userId = $('#UserId').val();
    if (!userId) {
        alert('Please select User');
        return;
    }
    fcmlApp.postAjaxRequest(userAccessGetUrl,
        { userId: userId },
        function (response) {
            $("#divLocationContainer").html(response);
            initCheckAll();
        }
    );
}

function updateLocationMapping() {
    var model = {
        Locations: [],
        SalesTeams: [],
        Salesmans: [],
        Architects: [],
        Divisions: []
    };
    var userId = $('#tblLocations').attr('data-user');
    $('#tblLocations tbody tr input.chkShow:checked').each(function (i, chk) {
        model.Locations.push({
            UserId: $(chk).attr('data-userid'),
            CompanyId: $(chk).attr('data-company')
        });
    });
    $('#tblSalesTeams tbody tr input.chkShow:checked').each(function (i, chk) {
        model.SalesTeams.push({
            UserId: $(chk).attr('data-userid'),
            SalesTeamId: $(chk).attr('data-salesteam')
        });
    });
    $('#tblSalesmans tbody tr input.chkShow:checked').each(function (i, chk) {
        model.Salesmans.push({
            UserId: $(chk).attr('data-userid'),
            SalesmanId: $(chk).attr('data-salesman')
        });
    });
    $('#tblArchitects tbody tr input.chkShow:checked').each(function (i, chk) {
        model.Architects.push({
            UserId: $(chk).attr('data-userid'),
            ArchitectId: $(chk).attr('data-architect')
        });
    });
    $('#tblDivisions tbody tr input.chkShow:checked').each(function (i, chk) {
        model.Divisions.push({
            UserId: $(chk).attr('data-userid'),
            DivisionId: $(chk).attr('data-division'),
            IsDefault: $(chk).closest('tr').find('input.chkDefault').is(':checked')
        });
    });
    fcmlApp.postAjaxRequest(userAccessUpdateUrl,
        { access: model, userId: userId },
        function (response) {
            if (response) {
                fcmlApp.showToast('Alert', 'Mapping updated successfully');
            }
            else {
                fcmlApp.showToast('Alert', 'Something went wrong, please try again!');
            }
        }
    );
}

function updateSelectAllItems() {
    var checked = $(this).is(':checked');
    $(this).closest('table').find('tbody input.chkShow').prop('checked', checked);
}
function updateSelectAllDefaultItems() {
    var checked = $(this).is(':checked');
    $(this).closest('table').find('tbody input.chkDefault').prop('checked', checked);
}

function updateSelectAllChk(chk) {
    if ($(chk).closest('tbody').find('input.chkShow:not(:checked)').length > 0) {
        $(chk).closest('table').find('thead input.chkSelectAll').prop('checked', false);
    }
    else {
        $(chk).closest('table').find('thead input.chkSelectAll').prop('checked', true);
    }
}

function updateSelectAllDefaultChk(chk) {
    if ($(chk).closest('tbody').find('input.chkDefault:not(:checked)').length > 0) {
        $(chk).closest('table').find('thead input.chkSelectAllDefault').prop('checked', false);
    }
    else {
        $(chk).closest('table').find('thead input.chkSelectAllDefault').prop('checked', true);
    }
}

function initCheckAll() {
    $('input.chkSelectAll').each(function () {
        if ($(this).closest('table').find('tbody input.chkShow').length == $(this).closest('table').find('tbody input.chkShow:checked').length)
            $(this).prop('checked', true);
    });
    $('input.chkSelectAllDefault').each(function () {
        if ($(this).closest('table').find('tbody input.chkDefault').length == $(this).closest('table').find('tbody input.chkDefault:checked').length)
            $(this).prop('checked', true);
    });
}