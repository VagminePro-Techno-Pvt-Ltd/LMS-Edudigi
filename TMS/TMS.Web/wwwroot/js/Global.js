$(window).on('load', function () {

});

$(function () {
    //loadUserNotifications();
    //loadUserNotificationsFollowup();
});

function loadUserNotifications() {
    applicationUtil.getAjaxRequest(userNotificationGetUrl, null, function (response) {
        $('#liDropDownNotification').append(response);
    });
}
function loadUserNotificationsFollowup() {
    applicationUtil.getAjaxRequest(userFollowupNotificationGetUrl, null, function (response) {
        $('#ulTopBarDropdownArea').prepend(response);
    });
}

function userFavoritiesUpdate(model, showAlert) {
    if (showAlert == undefined)
        showAlert = true;
    applicationUtil.postAjaxRequest(userFavoritiesUpdateUrl, { model: model }
        , function (response) {
            if (response == 1 && showAlert)
                applicationUtil.showToast('Alert', 'Status updated successfully!');
        });
}