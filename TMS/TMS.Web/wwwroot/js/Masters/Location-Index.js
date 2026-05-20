getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },
        { title: "Name", "data": "name", "autoWidth": true },
        { title: "Description", "data": "description", "autoWidth": true, className: "text-wrap" },
        { title: "Location Code", "data": "locationCode", "autoWidth": true, className: "text-wrap" },

        { title: "Status", "data": "isActiveStrHtml", "autoWidth": true },
        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },
        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}