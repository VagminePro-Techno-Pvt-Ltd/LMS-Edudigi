var oTable = null;
$(function () {
    $(document).on('change', "#QuestionType", loadOptions);
    $(document).on('click', "#btnSubmit", SaveData);
    $(document).on('click', '.rdoAnswer', selectRadio);
    oTable = $('#tblQuestions').DataTable();
});
function loadOptions() {
    if ($('#QuestionType').val()=='') {
        $('#divQuestionOptions').html('');
        return;
    }
    applicationUtil.getAjaxRequest(getQuestionOptionsUrl,{
        questionType: $('#QuestionType').val(),
    }, function (response) {
        $('#divQuestionOptions').html(response);
        resetFormValidation('#frmQuestion')
    });
}

function SaveData() {
    if (!$('#frmQuestion').valid()) {
        applicationUtil.showToast('Alert', 'Please validate the data!');
        return;
    }
    if ($('#frmQuestion input.rdoAnswer:checked').length == 0) {
        applicationUtil.showToast('Alert', 'Please select any correct Answer!');
        return;
    }
    var model = applicationUtil.serializeObject('#frmQuestion');
    applicationUtil.postAjaxRequest(saveQuestionsUrl, model, function (response) {
        applicationUtil.showToast("Alert", response.message);
        resetform();
        loadtable();
    });
}

function selectRadio() {
    $('.rdoAnswer').prop('checked', false);
    $(this).prop('checked', true);
}
function resetform()
{
    $('#QuestionType').val('');
    $('#Question').val('');
    $('#divQuestionOptions').html('');
    
}
function loadtable() {
    applicationUtil.getAjaxRequest(getQuestionListUrl, null, function (response) {
        if (oTable)
            oTable.destroy();
        $('#divQuestions').html(response);
        oTable = $('#tblQuestions').DataTable();

    });
}
























