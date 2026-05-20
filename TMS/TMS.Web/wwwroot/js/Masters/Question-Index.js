$(function () {
    $(document).on('change', "#QuestionType", loadOptions);
    $(document).on('click', '.rdoAnswer', selectRadio);
});
getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "dt-control text-end" },
        { title: "Question", "data": "question", "autoWidth": true },
        { title: "Question Type", "data": "questionTypeStr", "autoWidth": true },
        { title: "Status", "data": "isActiveStrHtml", "autoWidth": true },
        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },
        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}
function loadOptions() {
    if ($('#QuestionType').val() == '') {
        $('#divQuestionOptions').html('');
        return;
    }
    applicationUtil.getAjaxRequest(getQuestionOptionsUrl, {
        questionType: $('#QuestionType').val(),
    }, function (response) {
        $('#divQuestionOptions').html(response);
        resetFormValidation('#frmItem');
    });
}

validateForm = function () {
    if ($('#frmItem input.rdoAnswer:checked').length == 0) {
        applicationUtil.showToast('Alert', 'Please select any correct Answer!');
        return false;
    }
    return true;
}
function selectRadio() {
    $('.rdoAnswer').prop('checked', false);
    $(this).prop('checked', true);
}

setChildRenderer = function (oTable) {
    $('#tblItemList tbody').on('click', 'td.dt-control', function (e) {
        if (e.target !== this)
            return;
        var tr = $(this).closest('tr');
        var row = oTable.row(tr);
        if (row.child.isShown()) {
            // This row is already open - close it
            row.child.hide();
            tr.removeClass('parent');
        } else {
            var html = formatChildrow(row.data());
            row.child(html).show();
            tr.addClass('parent');
        }
    });
}

function formatChildrow(data) {
    if (!data || !data.questionOptions)
        return '';
    var html = '<table class="table table-morecondensed"><thead><tr><th style="width:50px;">Option Number</th><th>Option</th><th style="width:50px;">Correct Answer</th></tr></thead><tbody>'
    for (var i = 0; i < data.questionOptions.length; i++) {
        html += '<tr>';
        html += '<td valign="top" align="center">' + (i + 1) + '</td>';
        html += '<td valign="top">' + data.questionOptions[i].option + '</td>';
        html += '<td valign="top">' + (data.questionOptions[i].isCorrectAnswer ? 'Yes' : 'No') + '</td>';
        html += '</tr>';
    }
    html += '</tbody></table>';
    return html;
}