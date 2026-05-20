$(function () {
    $(document).on('click', '#btnGlobalModal', globalModalAction);
    $(document).on('click', '.aScheduletItem', hideRejectBtn);
    $(document).on('click', '.aRejectItem', hideScheduleBtn);
});

var globalModalAction = function () {
    //this is dummy function which will be invoked from specific pages;
}
function showGlobalModal(content, title, modalSizeClass, hasNoAction, actionButtonText) {
    $('#divGlobalModal div.modal-body').html(content);
    if (title)
        $('#divGlobalModal div.modal-header .modal-title').html(title);
    else
        $('#divGlobalModal div.modal-header .modal-title').html('');
    if (modalSizeClass)
        $('#divGlobalModal div.modal-dialog').attr('class', 'modal-dialog modal-dialog-centered ' + modalSizeClass);
    else
        $('#divGlobalModal div.modal-dialog').attr('class', 'modal-dialog modal-dialog-centered');

    if (hasNoAction)
        $('#divGlobalModal div.modal-footer #btnGlobalModal').addClass('d-none');
    else
        $('#divGlobalModal div.modal-footer #btnGlobalModal').removeClass('d-none');
    $('#divGlobalModal div.modal-footer #submitBtn').addClass('d-none')
    $('#divGlobalModal div.modal-footer #cancelBtn').addClass('d-none')
    $('#divGlobalModal div.modal-footer #rejectBtn').addClass('d-none')

    if (actionButtonText)
        $('#divGlobalModal div.modal-footer #btnGlobalModal').html(actionButtonText);
    else
        $('#divGlobalModal div.modal-footer #btnGlobalModal').html('Save');

    $('#divGlobalModal').modal('show');
}
function hideGlobalModal() {
    $('#divGlobalModal').modal('hide');
}


function hideRejectBtn(content, title, modalSizeClass, hasNoAction, actionButtonText) {

    $('#divGlobalModal div.modal-body').html(content);

    if (title)

        $('#divGlobalModal div.modal-header .modal-title').html(title);

    else

        $('#divGlobalModal div.modal-header .modal-title').html('');

    if (modalSizeClass)

        $('#divGlobalModal div.modal-dialog').attr('class', 'modal-dialog modal-dialog-centered ' + modalSizeClass);

    else

        $('#divGlobalModal div.modal-dialog').attr('class', 'modal-dialog modal-dialog-centered');

    if (hasNoAction)

        $('#divGlobalModal div.modal-footer #btnGlobalModal').addClass('d-none');

    else

        $('#divGlobalModal div.modal-footer #btnGlobalModal').removeClass('d-none');
    $('#divGlobalModal div.modal-footer #submitBtn').removeClass('d-none')


    if (actionButtonText) {

        $('#divGlobalModal div.modal-footer #btnGlobalModal').html(actionButtonText);
        $('#divGlobalModal div.modal-footer #rejectBtn').addClass('d-none')
        $('#divGlobalModal div.modal-footer #cancelBtn').addClass('d-none')         

        $('#divGlobalModal div.modal-footer #submitBtn').removeClass('d-none')
        $('#RemarkPartial div.modal-body #cancelBtn').addClass('d-none')


    }

    else

        $('#divGlobalModal div.modal-footer #btnGlobalModal').html('Save');

    $('#divGlobalModal').modal('show');

}


function hideScheduleBtn(content, title, modalSizeClass, hasNoAction, actionButtonText) {

    $('#divGlobalModal div.modal-body').html(content);

    if (title)

        $('#divGlobalModal div.modal-header .modal-title').html(title);

    else

        $('#divGlobalModal div.modal-header .modal-title').html('');

    if (modalSizeClass)

        $('#divGlobalModal div.modal-dialog').attr('class', 'modal-dialog modal-dialog-centered ' + modalSizeClass);

    else

        $('#divGlobalModal div.modal-dialog').attr('class', 'modal-dialog modal-dialog-centered');

    if (hasNoAction)

        $('#divGlobalModal div.modal-footer #btnGlobalModal').addClass('d-none');

    else

        $('#divGlobalModal div.modal-footer #btnGlobalModal').removeClass('d-none');

    if (actionButtonText) {

        $('#divGlobalModal div.modal-footer #btnGlobalModal').html(actionButtonText);
        $('#divGlobalModal div.modal-footer #submitBtn').addClass('d-none')
        $('#divGlobalModal div.modal-footer #cancelBtn').addClass('d-none')
        $('#divGlobalModal div.modal-footer #rejectBtn').removeClass('d-none')
        


    }

    else

        $('#divGlobalModal div.modal-footer #btnGlobalModal').html('Save');

    $('#divGlobalModal').modal('show');

}
