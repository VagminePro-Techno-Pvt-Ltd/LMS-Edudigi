var oTable = null;
$(function () {
    $(document).on('change', "#LocationId", loadDepartment);
    $(document).on('change', "#DepartmentId", loadDivision);
    $(document).on('change', "#DivisionId", loadDesignation);
    $(document).on('change', "#DesignationId,#CourseCategoryId", loadTableRecords);
    $(document).on('click', "#btnSubmit", getcheckedItemlist);
});
function loadTableRecords() {
    if (!$('#DesignationId').val()) {
        $('#divtblMatrices').html('');
        $('#divFilterContainer').addClass('d-none');
        $('#CourseCategoryId').val('');
        return;
    }
    $('#divFilterContainer').removeClass('d-none');
    applicationUtil.getAjaxRequest(getTrainingMatrixURL, {
        LocationId: $('#LocationId').val(),
        DepartmentId: $('#DepartmentId').val(),
        DivisionId: $('#DivisionId').val(),
        DesignationId: $('#DesignationId').val(),
        CourseCategoryId: $('#CourseCategoryId').val(),
    }, function (response) {
        if (oTable)
            oTable.destroy();
        $('#divtblMatrices').html(response);
        oTable = $('#tblMatrices').DataTable();
    });
}
function loadDepartment() {
    var id = $(this).val();
    loadDropdownList(getDepartmentsURL, '#DepartmentId', id);
    $('#DivisionId,#DesignationId,#divtblMatrices,#divtblMatrices').html('');
    $('#divFilterContainer').addClass('d-none');
    $('#CourseCategoryId').val('');
}
function loadDivision() {
    var id = $(this).val();
    loadDropdownList(getDivisionURL, '#DivisionId', id);
    $('#DesignationId,#divtblMatrices').html('')
    $('#divFilterContainer').addClass('d-none');
    $('#CourseCategoryId').val('');
}
function loadDesignation() {
    var id = $(this).val();
    loadDropdownList(getDesignationURL, '#DesignationId', id);
    $('#DesignationId,#divtblMatrices').html('')
    $('#divFilterContainer').addClass('d-none');
    $('#CourseCategoryId').val('');
}
function getcheckedItemlist() {
    var model = [];
    $('#tblMatrices tbody tr td .selectedchk').each(function (index, element) {
        var that = this;
        var tr = $(that).closest('tr');
        var obj = {
            LocationId: $('#LocationId').val(),
            DepartmentId: $('#DepartmentId').val(),
            DivisionId: $('#DivisionId').val(),
            DesignationId: $('#DesignationId').val(),
            CoursePriority: tr.find('.priority').val(),
            CourseId: tr.attr('data-courseId'),            
            Id: tr.attr('data-id'),
            IsActive: $(that).is(':checked')
        };
        model.push(obj);
    });
    applicationUtil.postAjaxRequest(saveTrainingMatrixURL, { model: model }, function (response) {
        applicationUtil.showToast("Alert", response.message);
        loadTableRecords();
    });
}