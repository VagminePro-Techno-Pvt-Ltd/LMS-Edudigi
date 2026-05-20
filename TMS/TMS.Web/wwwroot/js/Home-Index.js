$(function () {
    loadDashboardData();
  /*  loadMasterList();*/
});

function loadDashboardData() {
    applicationUtil.getAjaxRequest(dashboardUserUrl, null,
        function (response) {
            $('#divDashboardData').html(response);
        }, null, true);
}

//function loadMasterList() {
//    applicationUtil.getAjaxRequest(getAllMastersUrl, null,
//        function (response) {
//            $('#divMasterListView').html(response);
//            $('[data-toggle="tooltip"]').tooltip();
//        }, null, true);
//}
