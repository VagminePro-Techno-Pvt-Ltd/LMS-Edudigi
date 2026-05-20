var oTable = null;
$(function () {
    $(document).on('change', "#CourseId", loadMappings);
    $(document).on('click', "#btnSubmit", SaveData);
});

function loadMappings() {
    var courseId = getCourseId();
    if (!courseId) {
        if (oTable)
            oTable.destroy();
        $('#divtblQuizMapping').html('');
        return;
    }
    applicationUtil.getAjaxRequest(getQuizMappingListUrl, {
        CourseId: courseId
    }, function (response) {
        if (oTable)
            oTable.destroy();
        $('#divtblQuizMapping').html(response);
        oTable = $('#tblQuizMapping').DataTable();
    });
}
function SaveData() {
    var model = [];
    $('#tblQuizMapping tbody tr td .selectedchk').each(function (index, element) {
        var that = this;
        var tr = $(that).closest('tr');
        var obj = {
            QuestionId: tr.attr('data-questionid'),
            CourseId: tr.attr('data-courseid'),
            Id: tr.attr('data-id'),
            IsActive: $(that).is(':checked')
        };
        model.push(obj);
    });
    applicationUtil.postAjaxRequest(saveQuizMappingUrl, { model: model }, function (response) {
        applicationUtil.showToast("Alert", response.message);
        if (response.status) {
            loadMappings();
        }
    });
}
function getCourseId() {
    return $('#listCourse').find('option[value="' + $("#CourseId").val() + '"]').attr('data-id');
}





















