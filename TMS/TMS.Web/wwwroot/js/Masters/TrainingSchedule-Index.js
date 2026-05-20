

$(function () {

    $(document).on('change', '#VenueId', setMaxSeats);

    $(document).on('change', '#CourseId', setCourseDetails);

    $(document).on('change', '#toggle', editInoutFiled);

    $(document).on('click', '.aScheduletItem', ScheduleItemModal); 

    $(document).on('click', '.aRejectItem', ShowRejectItemModal);
    $(document).on('click', '.aReScheduleItem', ShowReScheduleItemModal);


    $(document).on('click', '#submitBtn', ScheduleItem);

    $(document).on('click', '#rejectBtn', RejectItem);
    $(document).on('click', '#cancelBtn', CancelItem);

    $(document).on('click', '.aNominateModal', showNominationRequestModal);

    $(document).on('click', '.aNominateWithdrawl', showNominationRequestModal);//for user 

    $(document).on('click', '#submitNominationBtn', NominationItem) //for user

    $(document).on('click', '.aNominationItem', ShowNominationModal);

    $(document).on('click', '#WithdrawlBtn', WithdrawlItem);// for Admins
    $(document).on('click', '.aCancelTraininglItem', ShowCancellationModal);

});


getColumnDefination = function () {

    var buttonIdCounter = 0;

    return [

        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },

        //{ "data": "idEnc", orderable: false, render: viewActionRenderer },

        {

            "data": "idEnc",

            "orderable": false,

            "autoWidth": true,

            "render": function (data, type, row, meta) {

                var html = '';
                html += viewActionRenderer(data, type, row, meta);

                html += NominationActionRenderer(data, type, row, meta);

                html += ScheduleActionRenderer(data, type, row, meta);

                html += RejectActionRenderer(data, type, row, meta);

                html += ApprovalActionRenderer(data, type, row, meta);

                html += NominationActionAdmin(data, type, row, meta);
                html += CancelTrainingActionRenderer(data, type, row, meta);

                return html;

            }

        },


        { title: "Status", "data": "statusDescription", "autoWidth": true },

        { title: "Training Title", "data": "trainingTitle", "autoWidth": true },

        { title: "Training Date", "data": "dateRangeStr", "autoWidth": true, className: "nowrap" },

        { title: "Training Time", "data": "timeRangeStr", "autoWidth": true, className: "nowrap" },

        { title: "Training Mode", "data": "modeStr", "autoWidth": true },

        { title: "Training Venue", "data": "venueName", "autoWidth": true },

        // { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },

        { title: "Last Action By", "data": "updatedByUserName", "autoWidth": true },

        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },

        { title: "Min-Max Seats", "data": "sittingRangeStr", "autoWidth": true },

        { title: "Nomination Left", "data": "nominationLeft", "autoWidth": true },

    ];

}

getColumnOrder = function () {

    return [[8, 'desc']];

}

function ShowNominationModal() {

    var id = null;

    var Showdata = null;

    var isEdit = false;

    if ($(this).hasClass('aNominationItem')) {

        id = $(this).attr('data-id');

        isEdit = true;

        $('#btnGlobalModal').hide();

        $('#RejectBtnGlobalModal').hide();

        $('#ApproveBtnGlobalModal').hide();
        $('#submitNominationBtn').hide();

        $('#WithdrawlBtn').hide();

    }

    var title = isEdit ? "Nomination Details" : "";

    var actionText = isEdit ? "" : "Nominate";

    if ($('#hdTitle').length > 0) {

        title += ' ';

        actionText += ' ';

    }

    applicationUtil.postAjaxRequest(getNominationItemUrl, { iId: id }, function (response) {

        //var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'false'

        showGlobalModal(response, title, 'modal-lg', actionText);

        $('#hdn_Id').val(id);

        resetFormValidation('#frmItem');

        performPostFormLoad();

    });

}


//for admins pop up ends

function showNominationRequestModal() {

    var id = null;

    var isEdit = false;

    if ($(this).hasClass('aNominateModal')) {

        id = $(this).attr('data-id');

        isEdit = true;

        $('#btnGlobalModal').hide();

        $('#RejectBtnGlobalModal').hide();

        $('#ApproveBtnGlobalModal').hide();

        $('#WithdrawlBtn').hide();

    }

    if ($(this).hasClass('aNominateWithdrawl')) {

        id = $(this).attr('data-id');

        isEdit = true;

        $('#btnGlobalModal').hide();

        $('#RejectBtnGlobalModal').hide();

        $('#ApproveBtnGlobalModal').hide();

        $('#submitNominationBtn').hide();

        $('#WithdrawlBtn').show();

    }

    var title = isEdit ? "Nomination" : "";

    var actionText = isEdit ? "" : "Nominate";

    if ($('#hdTitle').length > 0) {

        title += ' ';

        actionText += ' ';

    }

    applicationUtil.postAjaxRequest(getNominationRemarkItemUrl, { iId: id }, function (response) {

        var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'false'

        showGlobalModal(response, title, 'modal-lg', hasNoAction, actionText);

        resetFormValidation('#frmItem');

        performPostFormLoad();

    });

}
function ShowCancellationModal() {

    var id = null;

    var Showdata = null;

    var isEdit = false;

    if ($(this).hasClass('aCancelTraininglItem')) {

        id = $(this).attr('data-id');

        isEdit = true;

        $('#btnGlobalModal').hide();

        $('#RejectBtnGlobalModal').hide();

        $('#ApproveBtnGlobalModal').hide();
        $('#submitNominationBtn').hide();

        $('#WithdrawlBtn').hide();
     

    }

    var title = isEdit ? "Cancellation Details" : "";

    var actionText = isEdit ? "" : "Nominate";

    if ($('#hdTitle').length > 0) {

        title += ' ';

        actionText += ' ';

    }

    applicationUtil.postAjaxRequest(getRemarkItemUrl, { iId: id }, function (response) {

        //var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'false'

        showGlobalModal(response, title, 'modal-lg', actionText);
        $('#divGlobalModal div.modal-footer #rejectBtn').addClass('d-none')
        $('#divGlobalModal div.modal-footer #submitBtn').addClass('d-none') 
        $('#divGlobalModal div.modal-footer #cancelBtn').removeClass('d-none', true);


        $('#hdn_Id').val(id);



    });

}


function setMaxSeats() {

    //debugger

    $('#MaxSeats').val('');



    var selectedOption = $('#VenueId option:selected');

    var seats = selectedOption.data('seats');

    $('#MaxSeats').val(seats);

    //if (selectedOption.length > 0) {

    //    var seats = selectedOption.data('seats');

    //    $('#MaxSeats').val(seats);

    //}

}

function setCourseDetails() {

    //   debugger

    $('#CourseCategory,#CourseName,#DurationDays,#DurationHours').html(' ');

    if (!$(this).val())

        return;

    var option = $(this).find('option:selected');

    $('#CourseCategory').html(option.attr('data-category'));

    $('#CourseName').html(option.attr('data-name'));

    $('#DurationDays').html(option.attr('data-day'));

    $('#DurationHours').html(option.attr('data-hours'));

}

//function editInoutFiled() {

//    if ($(this).is(':checked')) {

//        $('#Link-field').show();

//    } else {

//        $('#Link-field').hide();

//    }

//}

function editInoutFiled() {

    if ($(this).is(':checked')) {

        $('#Link-field').removeClass('d-none');

        $('#Link-input').val('')

    } else {

        $('#Link-field').addClass('d-none');

        $('#Link-input').val('')

    }

}


// add from addedit start---


function ScheduleItemModal() {
    var id = null;
    var isEdit = false;
    if ($(this).hasClass('aScheduletItem')) {
        id = $(this).attr('data-id');
        $('#frmItem').prop('disabled', true);
        $('frmItem #rejectBtn').prop('disabled', true);
        $('#btnGlobalModal').hide();
        $('#submitNominationBtn').hide();
        $('#WithdrawlBtn').hide();
        isEdit = true;
    }
    $('#RejectBtnGlobalModal').hide();
    $('#ApproveBtnGlobalModal').hide();
    $('#rejectModal #rejectBtn').hide();
    var title = isEdit ? "Approved" : "Schedule";
    var actionText = isEdit ? "Update" : "Save";
    var title = isEdit ? "Schedule" : "Reject";
    if ($('#hdTitle').length > 0) {
        title += ' ' + $('#hdTitle').val();
        actionText += ' ' + $('#hdTitle').val
    }
    applicationUtil.postAjaxRequest(getRemarkItemUrl, { iId: id }, function (response) {
        var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'true'
        hideRejectBtn(response, title, 'modal-lg', hasNoAction, actionText);
        resetFormValidation('#frmItem');
        performPostFormLoad();
    });
}

function ShowRejectItemModal() {
    var id = null;
    var isEdit = false;
    if ($(this).hasClass('aRejectItem')) {
        id = $(this).attr('data-id');
        $('#frmItem').prop('disabled', true);
        $('#btnGlobalModal').hide();
        $('#submitNominationBtn').hide();
        $('#WithdrawlBtn').hide();
        isEdit = true;
        $('#rejectModal #rejectBtn').hide();
        var title = isEdit ? "Approved" : "Schedule";
        var actionText = isEdit ? "Update" : "Save";
        var title = isEdit ? "Reject" : "Schedule";
        if ($('#hdTitle').length > 0) {
            title += ' ' + $('#hdTitle').val();
            actionText += ' ' + $('#hdTitle').val();
        }
        $('#RejectBtnGlobalModal').hide();
        $('#ApproveBtnGlobalModal').hide();
    }
 
    applicationUtil.postAjaxRequest(getRemarkItemUrl, { iId: id }, function (response) {
        var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'true'
        hideScheduleBtn(response, title, 'modal-lg', hasNoAction, actionText);
        resetFormValidation('#frmItem');
        performPostFormLoad();
    });
}
function ShowReScheduleItemModal() {
    var id = null;

    var isEdit = false;

    if ($(this).hasClass('aReScheduleItem')) {
        id = $(this).attr('data-id');
        $('#frmItem').prop('disabled', true);
        $('#btnGlobalModal').hide();
        $('#submitNominationBtn').hide();
        $('#WithdrawlBtn').hide();
        isEdit = true;
        $('#rejectModal #rejectBtn').hide();
        var title = isEdit ? "Approved" : "Schedule";
        var actionText = isEdit ? "Update" : "Save";
        var title = isEdit ? "ReSchedule" : "Schedule";
        if ($('#hdTitle').length > 0) {
            title += ' ' + $('#hdTitle').val();
            actionText += ' ' + $('#hdTitle').val();
        }
        $('#RejectBtnGlobalModal').hide();
        $('#ApproveBtnGlobalModal').hide();
        $("#rejectBtn").text('ReSchedule')

    }
    applicationUtil.postAjaxRequest(getReScheduleUrl, { iId: id }, function (response) {
        var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'true'
        hideScheduleBtn(response, title, 'modal-lg', hasNoAction, actionText);
        resetFormValidation('#frmItem');
        performPostFormLoad();
    });
}

function ScheduleItem() {

    var id = null;

    var id = $('#getId').val();

    var remarks = $('#rejectModal #remarks').val();

    applicationUtil.postAjaxRequest(getScheduleUrl, { Id: id, Remarks: remarks }, onSubmit);

}

function NominationItem() {

    var id = null;

    var id = $('#getId').val();


    var remarks = $('#rejectModal #remarks').val();
    

    applicationUtil.postAjaxRequest(getNominationRequestUrl, { Id: id, Remarks: remarks }, onSubmit

        , hideGlobalModal(),

        loadRecords()

    );

}

function WithdrawlItem() {

    var id = null;

    var id = $('#getId').val();
    
    var remarks = $('#rejectModal #remarks').val();
    $('#remarks').prop('required', true);

    if (!remarks) {
        
        applicationUtil.showToast('Alert', 'Remarks Field Required!');
        return;
    }


    applicationUtil.postAjaxRequest(getWithdrawlRequestUrl, { Id: id, Remarks: remarks }, onSubmit

        , hideGlobalModal()

    );

    loadRecords();

}

function NominationRequest() {

    var id = null;

    if ($(this).hasClass('aNominateModal')) {

        id = $(this).attr('data-id');

    }

    applicationUtil.postAjaxRequest(getNominationRequestUrl, { Id: id });

}


function RejectItem() {

    var id = $('#getId').val();

    var remarks = $('#rejectModal #remarks').val();

    $('#remarks').prop('required', true);

    if (!remarks) {

        applicationUtil.showToast('Alert', 'Remarks Field Required!');

        return;

    }

    applicationUtil.postAjaxRequest(getRejectUrl, { Id: id, Remarks: remarks }, onReject);

}
function CancelItem() {

    var id = $('#getId').val();

    var remarks = $('#rejectModal #remarks').val();

    $('#remarks').prop('required', true);
  

    if (!remarks) {

        applicationUtil.showToast('Alert', 'Remarks Field Required!');

        return;

    }

    applicationUtil.postAjaxRequest(getCancelUrl, { Id: id, Remarks: remarks }, onReject);

}
//function ShowCancellationModal() {

//    var id = null;

//    var isEdit = false;

//    if ($(this).hasClass('aCancelTraininglItem')) {

//        id = $(this).attr('data-id');

//        isEdit = true;

//        $('#btnGlobalModal').hide();

//        $('#RejectBtnGlobalModal').hide();

//        $('#ApproveBtnGlobalModal').hide();
//        $('#submitNominationBtn').hide();

//        $('#WithdrawlBtn').hide();

//    }

//    var title = isEdit ? "Cancellation Details" : "";

//    var actionText = isEdit ? "" : "Nominate";

//    if ($('#hdTitle').length > 0) {

//        title += ' ';

//        actionText += ' ';

//    }

//    applicationUtil.postAjaxRequest(getRemarkItemUrl, { iId: id }, function (response) {

//        var hasNoAction = $(response).find('#hdAddEditActionAllowed').val() == 'true'

//        hideRejectBtn(response, title, 'modal-lg', hasNoAction, actionText);
//        hideScheduleBtn(response, title, 'modal-lg', hasNoAction, actionText);

//        resetFormValidation('#frmItem');

//        performPostFormLoad();

//    });

//}



function hideScheduleModal() {

    $('#RemarkPartial').modal('hide');

}

function hideRejectModal() {

    $('#RemarkPartial').modal('hide');

}

function onSubmit(response) {

    if (!response) {

        applicationUtil.showToast('Alert', 'Something went wrong, please contact support!');

        return;

    }

    if (response.message) {

        applicationUtil.showToast(response.status ? 'Message' : 'Alert', response.message);

    }

    if (response.status) {

        hideScheduleModal();

        hideGlobalModal();


        loadRecords();

    }

}

function onReject(response) {

    if (!response) {

        applicationUtil.showToast('Alert', 'Something went wrong, please contact support!');

        return;

    }

    if (response.message) {

        applicationUtil.showToast(response.status ? 'Message' : 'Alert', response.message);

    }

    hideRejectModal();

    hideGlobalModal();

    loadRecords();

}

performPostFormLoad = function () {

    if (!$('#CourseId').val())

        return;

    $('#CourseId').change();

}


loadTableRecords = function () {

}

changeTableRecords = function () { }


$(function () {

    $(document).on('change', "#LocationId", loadNomDepartment);

    $(document).on('change', "#DepartmentId", loadNomDivision);

    $(document).on('change', "#DivisionId", loadNomDesignation);

    $(document).on('change', "#DesignationId,#nCourseCategoryId", loadNomTableRecords);

    $(document).on('click', "#SaveNomUserByAdmin", getUsercheckedItemlist);


});

var oTable = null;

function loadNomTableRecords() {

    if (!$('#DesignationId').val()) {

        $('#divtblNomination').html('');

        $('#divFilterContainer').addClass('d-none');

        $('#nCourseCategoryId').val('');

        return;

    }

    $('#divFilterContainer').removeClass('d-none');

    applicationUtil.getAjaxRequest(getTrainingNominationURL, {

        LocationId: $('#LocationId').val(),

        DepartmentId: $('#DepartmentId').val(),

        DivisionId: $('#DivisionId').val(),

        DesignationId: $('#DesignationId').val(),

        CourseCategoryId: $('#nCourseCategoryId').val(),

    }, function (response) {

        if (oTable)

            oTable.destroy();

        $('#divtblNomination').html(response);

        oTable = $('#tblNomination').DataTable();

    });

}

function loadNomDepartment() {

    var id = $(this).val();

    loadDropdownList(getNomDepartmentsURL, '#DepartmentId', id);

    $('#DivisionId,#DesignationId,#divtblNomination,#divtblNomination').html('');

    $('#divFilterContainer').addClass('d-none');

    $('#nCourseCategoryId').val('');

}

function loadNomDivision() {

    var id = $(this).val();

    loadDropdownList(getNomDivisionURL, '#DivisionId', id);

    $('#DesignationId,#divtblNomination').html('')

    $('#divFilterContainer').addClass('d-none');

    $('#nCourseCategoryId').val('');

}

function loadNomDesignation() {

    var id = $(this).val();

    var res = loadDropdownList(getNomDesignationURL, '#DesignationId', id);


    loadDropdownList(getNomDesignationURL, '#DesignationId', id);

    $('#DesignationId,#divtblNomination').html('')

    $('#divFilterContainer').addClass('d-none');

    $('#nCourseCategoryId').val('');

}

function getUsercheckedItemlist() {

    var id = null;

    var id = $('#hdn_Id').val();

    var selectedUsers = [];

    $('.selectedchk:checked').each(function () {

        selectedUsers.push($(this).data('userid'));

    });


    var remarks = $('#remarks').val();

    applicationUtil.postAjaxRequest(saveTrainingNominationURL, { selectedUsers: selectedUsers, remarks: remarks, Id: id }, onMultiUserSelection);

}

function onMultiUserSelection(response) {

    if (!response) {

        applicationUtil.showToast('Alert', 'Something went wrong, please contact support!');

        return;

    }

    if (response.message) {

        applicationUtil.showToast(response.status ? 'Message' : 'Alert', response.message);

        hideGlobalModal();

        loadRecords();

    }

    if (response.status) {

    }

}


function ScheduleActionRenderer(data, type, row, meta) {
         var ScheduleViewIconClass = $('#tblItemList').attr('data-si');

    var buttonHTML = '';

    //if (row.statusId == 2 || row.statusId == 5 || row.statusId == 6) {

    //    var ScheduleViewIconClass = $('#tblItemList').attr('data-si');

    //    buttonHTML =  " "+'<a class="aScheduletItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="send for approval"><i class="' + ScheduleViewIconClass + '"></i></a>';

    //}
    if (row.statusId == 3 || row.statusId == 1) {

        //return '<a class="Text-Centre btn btn-primary disabled btn-sm ">Scheduled</a>';

        buttonHTML = " " + '<a class="aScheduletItem d-none" data-id="' + row.idEnc + '" data-toggle="tooltip" title=""><i class="' + ScheduleViewIconClass + '"></i></a>';

    }

    if (row.statusId == 2 ||row.statusId==8) {

        //return '<a class="aScheduletItem Text-Center btn btn-success btn-sm" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + ScheduleIcon + '">Schedule</a>';

        buttonHTML = " " + '<a class="aScheduletItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Send For Approval"><i class="' + ScheduleViewIconClass + '"></i></a>';

    }

    if (row.statusId == 5) {

        //return '<a class="aScheduletItem Text-Center btn btn-success btn-sm" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + ScheduleIcon + '">Schedule</a>';

        buttonHTML = " " + '<a class="aScheduletItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Approve"><i class="' + ScheduleViewIconClass + '"></i></a>';

    }

    if (row.statusId == 6) {

        //return '<a class="aScheduletItem Text-Center btn btn-success btn-sm" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + ScheduleIcon + '">Schedule</a>';

        buttonHTML = " " + '<a class="aScheduletItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Plan"><i class="' + ScheduleViewIconClass + '"></i></a>';

    }

    return buttonHTML;

}

function RejectActionRenderer(data, type, row, meta) {

    var buttonHTML = '';
    var RejectText = $('#tblItemList').attr('data-rt');

    var RejectIcon = $('#tblItemList').attr('data-ri');

    if (row.statusId != 3 || row.statusId != 1) {
        buttonHTML = " "+ '<a class="aRejectItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + RejectText + '"><i class="' + RejectIcon + '"></i></a>';
    }
    if (row.statusId == 7) {
        buttonHTML = " " + '<a class="aRejectItem d-none" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + RejectText + '"></a>';

    }
    if (row.statusId == 3) {
        buttonHTML = " " + '<a class="aReScheduleItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Reschedule"><i class="fa-solid fa-arrow-rotate-right"></i></a>';

    }

    return buttonHTML;

}

function NominationActionAdmin(data, type, row, meta) {

    var buttonHTML = '';
    var AdminNominationText = $('#tblItemList').attr('data-nnt');

    var AdminNominationIcon = $('#tblItemList').attr('data-nni');

    if (row.statusId == 1) {

        buttonHTML = " " + '<a class="aNominationItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + AdminNominationText + '"><i class="' + AdminNominationIcon +'"></i></a>';


    }
    else {
        buttonHTML = " " + '<a class="d-none"  data-id="' + row.idEnc + '" data-toggle="tooltip" title="Admin Nominate"><i class="fa fa-reply-all"></i></a>';

    }

    return buttonHTML;

}

function NominationActionRenderer(data, type, row, meta) {
    var buttonHTML = '';

    var nominationIcon = $('#tblItemList').attr('data-ni');

    var nominationText = $('#tblItemList').attr('data-nt');

    if (row.nrRequest == 0 || row.nrRequest == 3 || row.nrRequest==1) {

        buttonHTML = " " + '<a class="aNominateModal " disabled data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + nominationText + '"></a>';

    }

    if (row.nrRequest == 1 ) {

        buttonHTML = " " + '<a class="aNominateWithdrawl" data-id="' + row.idEnc + '" data-toggle="tooltip" title="withdraw nomination"><i class="fas fa-times"></i></a>';

    }
    if (row.nrRequest == 3 || row.nrRequest == 5 || row.nrRequest == 4) {

        buttonHTML = " " + '<a class="aNominateWithdrawl" disabled data-id="' + row.idEnc + '" data-toggle="tooltip" title="withdraw nomination"></a>';

    }
    else if (row.nrRequest == 2 || row.nrRequest == null) {

        buttonHTML = ' '+'<a class="aNominateModal" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + nominationText + '"><i class="' + nominationIcon + '"></i></a>';

    }

    return buttonHTML;

}

function ApprovalActionRenderer(data, type, row, meta) {

    var buttonHTML = '';

    if (row.statusId != 2) {

        var ApprovalIcon = $('#tblItemList').attr('data-ai');

        var approvalText = $('#tblItemList').attr('data-at');

        buttonHTML = ' '+'<a class="aApprovalItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + approvalText + '"><i class="' + ApprovalIcon + '"></i></a>';

    }

    return buttonHTML;

}
function viewActionRenderer(data, type, row, meta) {
    var buttonHTML = '';
    var editText = $('#tblItemList').attr('data-et');

    var editIcon = $('#tblItemList').attr('data-ei');
    if (row.statusId == 2) {
       

        buttonHTML = '<a class="aEditItem " data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + editText + '"><i class="' + editIcon + '"></i></a>';
    }
    else {
        buttonHTML = '<a class="aEditItem d-none" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + editText + '"></a>';

    }
    return buttonHTML;


}
function CancelTrainingActionRenderer(data, type, row, meta) {

    var buttonHTML = '';
    var TrainingCancelText = $('#tblItemList').attr('data-tct');

    var TrainingCancelIcon = $('#tblItemList').attr('data-tci');
   
   
    //if (row.Role.UserTypeId==3) {
    //    buttonHTML = ' ' + '<a class="aCancelTraininglItem d-none" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Cancel Training"><i class="fas fa-times"></i> </a>';


    //}
    if (row.statusId !=7) {
        buttonHTML = " " + '<a class="aCancelTraininglItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + TrainingCancelText + '"><i class="' + TrainingCancelIcon + '"></i></a>';


    }
    else {
        buttonHTML = ' ' + '<a class="aCancelTraininglItem" data-id="' + row.idEnc + '" data-toggle="tooltip" title=""></a>';

    }

    return buttonHTML;

}
