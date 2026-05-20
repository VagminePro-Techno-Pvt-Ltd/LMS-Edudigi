getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "id", orderable: false, render: indexRenderer, className: "text-end" },
        { title: "Name", "data": "name"},
        { title: "Initial", "data": "initial"},
        { title: "Phone", "data": "phone", "autoWidth": true, className: "text-wrap" },
        { title: "Fax", "data": "fax" },
        { title: "Email", "data": "email" },
        { title: "Address 1", "data": "address1" },
        { title: "Status", "data": "isActiveStrHtml", "autoWidth": true },
        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },
        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}