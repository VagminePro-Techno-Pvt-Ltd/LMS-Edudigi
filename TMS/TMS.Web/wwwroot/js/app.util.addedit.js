


var oTableObject = null;

var loadRecords = null;

$(function () {

    $(document).on('click', '.aAddNewItem', showAddItemModal);

    $(document).on('click', '.aEditItem', showAddItemModal);

    $(document).on('click', '.aApprovalItem', ApprovalStatusItemModal);
  
});

loadRecords = function () {

    //do not delete

    if (oTableObject) {

        oTableObject.destroy();

    }

    applicationUtil.getAjaxRequest(getListItemUrl, null, function (response) {

        $('#divIndexContainer .card-body').html(response);

        oTableObject = $('#tblItemList').DataTable({ sWrapper: 'dataTables_wrapper dt-bootstrap' });

    });

}

var reloadRecords = function () {

    //do not delete

}

var performPostFormLoad = function () {

    //do not delete

}
function setMaxSeats() {
    //debugger
    $('#MaxSeats').val('');
    var selectedOption = $('#VenueId option:selected');
    var seats = selectedOption.data('seats');
    $('#MaxSeats').val(seats);
}

function showAddItemModal() {

    var id = null;

    var isEdit = false;

    if ($(this).hasClass('aEditItem')) {

        id = $(this).attr('data-id');

        isEdit = true;
        $('#submitNominationBtn').hide();

        $('#WithdrawlBtn').hide();
        $('#divGlobalModal div.modal-body #EditRejectRemarkModal').addClass('d-none', true)

    }
 
   
    var title = isEdit ? "Edit" : "Add";

    var actionText = isEdit ? "Update" : "Save";

    if ($('#hdTitle').length > 0) {

        title += ' ' + $('#hdTitle').val();

        actionText += ' ' + $('#hdTitle').val();
    }
    $('#RejectBtnGlobalModal').hide();
    $('#ApproveBtnGlobalModal').hide();
    $('#submitNominationBtn').hide();

    $('#WithdrawlBtn').hide();

    // Build URL with roleFilter if present
    var ajaxUrl = getItemUrl;
    var roleFilterVal = $('#hdRoleFilter').val();
    if (roleFilterVal) {
        ajaxUrl += (ajaxUrl.indexOf('?') > -1 ? '&' : '?') + 'roleFilter=' + encodeURIComponent(roleFilterVal);
    }

    applicationUtil.postAjaxRequest(ajaxUrl, { iId: id }, function (response) {

        var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'false'

        showGlobalModal(response, title, 'modal-lg', hasNoAction, actionText);

        resetFormValidation('#frmItem');

        performPostFormLoad();



    });

}



function ApprovalStatusItemModal() {

    var id = null;
    var isEdit = false;
    $('#divGlobalModal div.modal-footer #rejectBtn').addClass('d-none')

    if ($(this).hasClass('aApprovalItem')) {

        id = $(this).attr('data-id');
        var Sid = $('#approvalValue').val();
        $('#frmItem').prop('disabled', true);
        $('#btnGlobalModal').hide();
        isEdit = true;
    }

    $('#rejectModal #rejectBtn').hide();
    $('#divGlobalModal #RejectBtnGlobalModal').hide();
    $('#divGlobalModal #ApproveBtnGlobalModal').hide();
    $('#submitNominationBtn').hide();

    $('#WithdrawlBtn').hide();

  

    var actionText = isEdit ? "Update" : "Save";

    var title = isEdit ? "Approval History" : "Schedule";

    if ($('#hdTitle').length > 0) {

        title += ' ';

        actionText += ' ' + $('#hdTitle').val();

    }

    applicationUtil.postAjaxRequest(getSApprovalStatusUrl, { iId: id }, function (response) {

        var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'true'

        hideScheduleBtn(response, title, 'modal-lg', hasNoAction, actionText);

        resetFormValidation('#frmItem');

        performPostFormLoad();

    });

}


var validateForm = function () {

    return true;

}

globalModalAction = function () {

    if (!$('#frmItem').valid()) {

        applicationUtil.showToast('Alert', 'Please validate the data!');

        return;

    }

    if (!validateForm())

        return;

    // Build save URL with roleFilter if present
    var saveUrl = saveItemUrl;
    var roleFilterSave = $('#hdRoleFilter').val();
    if (roleFilterSave) {
        saveUrl += (saveUrl.indexOf('?') > -1 ? '&' : '?') + 'roleFilter=' + encodeURIComponent(roleFilterSave);
    }

    if ($('#frmItem input[type="file"]').length > 0) {

        model = applicationUtil.serializeFormObject('#frmItem')

        applicationUtil.postAjaxRequestForm(saveUrl, model, onSuccess);

    }

    else {

        var model = applicationUtil.serializeObject($('#frmItem'));

        applicationUtil.postAjaxRequest(saveUrl, model, onSuccess);

    }

}

function onSuccess(response) {

    if (!response) {

        applicationUtil.showToast('Alert', 'Something went wrong, please contact support!');

        return;

    }

    if (response.message) {

        applicationUtil.showToast(response.status ? 'Message' : 'Alert', response.message);

    }

    if (response.status) {

        hideGlobalModal();

        loadRecords();

    }

}

function loadDropdownList(url, controlSelector, id) {

    applicationUtil.getAjaxRequest(url, { id: id }, function (response) {

        $(controlSelector).html(response);

    });

}






