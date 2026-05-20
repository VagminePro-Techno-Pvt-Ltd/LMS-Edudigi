$(function () {
    jQuery.extend(jQuery.validator.messages, {
        maxlength: "Please enter no more than {0} characters.",
        minlength: "Please enter at least {0} characters."
    });
});