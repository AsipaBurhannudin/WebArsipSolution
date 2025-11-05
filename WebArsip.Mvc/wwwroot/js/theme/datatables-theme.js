$(document).ready(function () {
    // 🔧 Default konfigurasi global DataTables
    $.extend(true, $.fn.dataTable.defaults, {
        responsive: true,
        pageLength: 5,
        autoWidth: false,
        language: {
            search: "🔍 Search:",
            lengthMenu: "Show _MENU_ entries",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            paginate: {
                previous: "‹ Prev",
                next: "Next ›"
            },
            zeroRecords: "No data found",
            infoEmpty: "No entries available"
        },
        initComplete: function () {
            $(this).closest('.dataTables_wrapper').hide().fadeIn(600);
        }
    });

    // 💡 Efek smooth untuk hover tombol paging
    $(document).on('mouseenter', '.paginate_button', function () {
        $(this).css('transform', 'scale(1.05)');
    }).on('mouseleave', '.paginate_button', function () {
        $(this).css('transform', 'scale(1)');
    });
});