
var getColumnDefination = function () {

    //do not delete, this is to be used in implementation.

};


var getFilters = function (data) {

    //do not delete, this is to be used in implementation.

    return data;

}

var setChildRenderer = function (table) {

    //do not delete, this is to be used in implementation.

}

var setInitComplete = function (settings, json) {

    var api = this.api();

    var textBox = $('#tblItemList_filter label input');

    textBox.unbind().bind('keyup input', function (e) {

        if (e.keyCode == 13) {

            api.search(this.value).draw();

        }

    });

}

var getColumnOrder = function () {

    return [[1, 'asc']];

}

loadRecords = function () {

    if (oTableObject)

        oTableObject.search('').draw();

}

$(function () {

    $(document).on('click', '#updateStatusBtn', loadTableData);

    loadTableData();

    setChildRenderer(oTableObject);

});

//$(function () {

//    $(document).on('click', '#updateStatusBtn', loadTableRecords);

//    loadTableRecords();

//    setChildRenderer(oTableObject);

//});


function loadTableData() {

    oTableObject = $('#tblItemList').DataTable({

        processing: true,

        serverSide: true,

        ordering: true,

        paging: true,

        searchDelay: 1000,


        "ajax": {

            "url": getListPageUrl,

            "type": "POST",

            "datatype": "json",

            "data": function (data) {

                var point = getFilters(data);


            }

        },

        columns: getColumnDefination(),

        order: getColumnOrder(),


        initComplete: setInitComplete

    });

}

function indexRenderer(data, type, row, meta) {

    return meta.row + meta.settings._iDisplayStart + 1;

}

function viewActionRenderer(data, type, row, meta) {

    var editText = $('#tblItemList').attr('data-et');

    var editIcon = $('#tblItemList').attr('data-ei');
    return '<a class="aEditItem " data-id="' + row.idEnc + '" data-toggle="tooltip" title="' + editText + '"><i class="' + editIcon + '"></i></a>';


}






