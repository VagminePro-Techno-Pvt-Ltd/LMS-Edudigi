$(function () {
    $(document).on('click', '.aNominationApprovalBtn  ', ApproveNominationModel);
    $(document).on('click', '#ApproveBtnGlobalModal', ApproveNomination);
    $(document).on('click', '.aNominationRejectBtn', ApproveNominationModel);
    $(document).on('click', '#RejectBtnGlobalModal', RejectNomination);
});



getColumnDefination = function () {

    return [

        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },
        {
            title: "Action", "data": "idEnc", orderable: false, "autoWidth": true, "render": function (data, type, row, meta) {

                var html = '';

                html += NominationApprovalRenderer(data, type, row, meta);

                html += NominationRejectRenderer(data, type, row, meta);

                return html;
            }
        },

        { title: "Name", "data": "candidate.name", "autoWidth": true },

        { title: "Email", "data": "candidate.email", "autoWidth": true },

        { title: "Training Title", "data": "training.trainingTitle", "autoWidth": true },

        { title: "Training Date", "data": "training.dateRangeStr" },

        { title: "Training Time", "data": "training.timeRangeStr", "autoWidth": true, className: "nowrap" },

        {
            title: "Status", "data": "status", render: function (data, type, row) {

                switch (data) {
                                        case 0:

                        return "Requested";

                    case 1:

                        return "Approved";

                    case 2:

                        return "Rejected";
                    case 3:

                        return "Withdraw Requested";

                    case 4:

                        return "Withdraw Approved";
                    case 5:

                        return "Withdraw Rejected";

                    default:

                        return "";

                }

            }, "autoWidth": true

        },

        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },

        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true }

    ]

}

getColumnOrder = function () {
    return [[9, 'desc']];
}
function NominationApprovalRenderer(data, type, row, meta) {
    var buttonHTML = '';
    if (row.status == 1 || row.status == 2 || row.status == 4 || row.status==5) {
        buttonHTML = '  ' + '<a class="aNominationApprovalBtn d-none" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Approve"><i class="far fa-check-square text-success"></i></a>';
    }
    else {
        buttonHTML = '  ' + '<a class="aNominationApprovalBtn" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Approve"><i class="far fa-check-square text-success"></i></a>';
    }
    return buttonHTML;
}


//function NominationApprovalRenderer(data,type,row,meta) {
//    //var nominationApprovalIcon = $('#tblItemList').attr('data-nai');
//    //var nominationApprovalText = $('#tblItemList').attr('data-nat');
//    if (row.status == 1 || row.status == 2) {
//        return '<a class="aNominationApprovalBtn d-none" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Approve"><i class="far fa-check-square text-success"></i></a>';

//    }

//    else {
//        return '<a class="aNominationApprovalBtn" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Approve"><i class="far fa-check-square text-success"></i></a>';
//    }


//}
function NominationRejectRenderer(data, type, row, meta) {
    var buttonHTML = '';
    if (row.status == 1 || row.status == 2 || row.status == 4 || row.status==5) {
        buttonHTML = '  ' + '<a class="aNominationRejectBtn d-none" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Reject"><i class="fa fa-ban text-danger"></i></a>';
    }
    else {
        buttonHTML = '  ' + '<a class="aNominationRejectBtn" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Reject"><i class="fa fa-ban text-danger"></i></a>';
    }
    return buttonHTML;
}
//function NominationRejectRenderer(data, type, row, meta) {
//    var nominationRejectIcon = $('#tblItemList').attr('data-nri');
//    var nominationRejectText = $('#tblItemList').attr('data-nrt');
//    if (row.status == 1 || row.status == 2) {
//        return '<a class="aNominationRejectBtn d-none" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Reject"><i class="fa fa-ban text-danger"></i></a>';

//    }
//    else {
//        return '<a class="aNominationRejectBtn" data-id="' + row.idEnc + '" data-toggle="tooltip" title="Reject"><i class="fa fa-ban text-danger"></i></a>';
//    }
//}



//for admins Approval pop-up
function ApproveNominationModel() {
    var id = null;
    if ($(this).hasClass('aNominationApprovalBtn')) {
        id = $(this).attr('data-id');
        $('#btnGlobalModal').hide();
        $('#ApproveBtnGlobalModal').show();
        $('#RejectBtnGlobalModal').hide();
        $('#submitNominationBtn').hide();
        $('#WithdrawlBtn').hide();
    }
    if ($(this).hasClass('aNominationRejectBtn')) {
        id = $(this).attr('data-id');
        $('#btnGlobalModal').hide();
        $('#RejectBtnGlobalModal').show();
        $('#ApproveBtnGlobalModal').hide();
        $('#submitNominationBtn').hide();
        $('#WithdrawlBtn').hide();
    }
    applicationUtil.postAjaxRequest(getNominationRemarkUrl, { iId: id }, function (response) {
        $('#divGlobalModal div.modal-header .modal-title').html("Nomination Details");
        $('#divGlobalModal .modal-body').html(response);
        $('#hdn_Id').val(id);
        $('#divGlobalModal').modal('show');
        $('#divGlobalModal div.modal-footer #rejectBtn').addClass('d-none')
        $('#divGlobalModal div.modal-footer #submitBtn').addClass('d-none')
        $('#divGlobalModal div.modal-footer #cancelBtn').addClass('d-none');
    });
} 

function ApproveNomination() {
    var id = null;
    var id = $('#hdn_Id').val();
    var remarks = $('#divGlobalModal #AdminNominationRemarks').val();
    applicationUtil.postAjaxRequest(getApproveNominationUrl, { Id: id, Remarks: remarks }, onActionForNomination);
}

function RejectNomination() {
    var id = null;
    var id = $('#hdn_Id').val();
   
    var remarks = $('#divGlobalModal #AdminNominationRemarks').val();
    $('#remarks').prop('required', true);
    if (!remarks) {
        applicationUtil.showToast('Alert', 'Remarks Field Required!');
        return;
    }
    applicationUtil.postAjaxRequest(getRejectNominationUrl, { Id: id, Remarks: remarks }, onActionForNomination);
}
function onActionForNomination(response) {
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
//function NominationActionRenderer(data, type, row, meta) {

//    var nominationIcon = $('#tblItemList').attr('data-ni');
//    var nominationText = $('#tblItemList').attr('data-nt');


//    return " " + '<a class="aNominateModal" data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + nominationText + '"><i class="' + nominationIcon + '"></i></a>';

//}

