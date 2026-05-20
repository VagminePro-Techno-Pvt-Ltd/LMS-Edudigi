var myElement = document.getElementById('simple-bar');
if (myElement) {
    new SimpleBar(myElement, { autoHide: true });
}
var primary = localStorage.getItem("primary") || '#7366ff';
var secondary = localStorage.getItem("secondary") || '#f73164';

window.CubaAdminConfig = {
    // Theme Primary Color
    primary: primary,
    // theme secondary color
    secondary: secondary,
};

$(document).ready(function () {
    $('.loader-wrapper').fadeOut('slow', function () {
        if ($(this).hasClass('topLoader'))
            $(this).remove();
    });
});

$(window).on('pageshow', function (event) {
    $('.loader-wrapper').fadeOut('slow', function () {
        if ($(this).hasClass('topLoader'))
            $(this).remove();
    });
});

$(window).on('scroll', function () {
    if ($(this).scrollTop() > 600) {
        $('.tap-top').fadeIn();
    } else {
        $('.tap-top').fadeOut();
    }
});

$('.tap-top').click(function () {
    $("html, body").animate({
        scrollTop: 0
    }, 600);
    return false;
});

