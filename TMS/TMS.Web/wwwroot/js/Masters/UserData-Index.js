
getColumnDefination = function () {

    var buttonIdCounter = 0;

    return [

        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },

        { title: "Status", "data": "status.nameStatusHtml", "autoWidth": true },

        { title: "Training Title", "data": "trainingTitle", "autoWidth": true },

        { title: "Training Date", "data": "dateRangeStr", "autoWidth": true, className: "nowrap" },

        { title: "Training Time", "data": "timeRangeStr", "autoWidth": true, className: "nowrap" },

        { title: "Training Mode", "data": "modeStr", "autoWidth": true },

        { title: "Training Venue", "data": "venue.name", "autoWidth": true },

        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },

        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },

        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer },

        { "data": "idEnc", orderable: false, "autoWidth": true, render: ScheduleActionRenderer },

        { "data": "idEnc", orderable: false, "autoWidth": true, render: RejectActionRenderer },
    ];
}

getColumnOrder = function () {
    return [[2, 'asc']];
}

function setCourseDetails() {
    $('#CourseCategory,#CourseName,#DurationDays,#DurationHours').html(' ');
    if (!$(this).val())
        return;
    var option = $(this).find('option:selected');
    $('#CourseCategory').html(option.attr('data-category'));
    $('#CourseName').html(option.attr('data-name'));
    $('#DurationDays').html(option.attr('data-day'));
    $('#DurationHours').html(option.attr('data-hours'));
}







performPostFormLoad = function () {
    if (!$('#CourseId').val())
        return;
    $('#CourseId').change();
}

