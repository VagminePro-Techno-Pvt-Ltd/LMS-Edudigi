$(function () {
    $(document).on("change", "input[type=date]", function () {
        var date = moment(this.value, "YYYY/MM/DD").format("DD/MM/YYYY");
        if (date.indexOf('Invalid') === -1)
            this.setAttribute("data-date", date);
        else
            this.setAttribute("data-date", 'dd/mm/yyyy');
    });
    $("input[type=date]").trigger("change");
});