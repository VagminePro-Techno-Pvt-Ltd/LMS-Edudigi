getColumnDefination = function () {
    return [
        { title: "Sl. No.", "data": "Id", orderable: false, render: indexRenderer, className: "text-end" },
        { title: "Venue Code", "data": "venueId" },
        { title: "Name", "data": "name", "autoWidth": true },
        { title: "Description", "data": "description", "autoWidth": true, className: "text-wrap" },
        { title: "Status", "data": "isActiveStrHtml", "autoWidth": true },
        { title: "Last Action By", "data": "lastActionBy", "autoWidth": true },
        { title: "Last Action On", "data": "lastActionOnStr", "autoWidth": true },
        { title: "Venue Seats", "data": "venueSeat", "autoWidth": true },
        { "data": "idEnc", orderable: false, "autoWidth": true, render: viewActionRenderer }
    ]
}