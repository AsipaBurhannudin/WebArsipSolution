$(document).ready(function () {
    const table = $('#userTable').DataTable({
        pageLength: 5,
        responsive: true,
        autoWidth: false,
        language: {
            search: "🔍 Search:",
            lengthMenu: "Show _MENU_ entries",
            zeroRecords: "No matching users found",
            info: "Showing _START_ to _END_ of _TOTAL_ users",
            paginate: { next: "›", previous: "‹" }
        }
    });

    table.on('order.dt search.dt', function () {
        table.column(0, { search: 'applied', order: 'applied' })
            .nodes()
            .each((cell, i) => { cell.innerHTML = i + 1; });
    }).draw();

    // 🗑 Delete User
    $('#userTable').on('click', '.delete-btn', function () {
        const id = $(this).data("id");
        const name = $(this).data("name");

        Swal.fire({
            title: "Yakin ingin hapus?",
            text: `User "${name}" akan dihapus permanen.`,
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Ya, hapus!",
            cancelButtonText: "Batal",
            reverseButtons: true
        }).then(result => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: 'Menghapus...',
                    html: '<div class="spinner-border text-danger" role="status"></div>',
                    showConfirmButton: false,
                    allowOutsideClick: false
                });

                $.post(`/User/Delete/${id}`, res => {
                    Swal.close();
                    if (res.success) {
                        Swal.fire({
                            icon: "success",
                            title: res.message,
                            timer: 1500,
                            showConfirmButton: false
                        });
                        setTimeout(() => location.reload(), 1600);
                    } else {
                        Swal.fire("Error", res.message, "error");
                    }
                }).fail(() => Swal.fire("Error", "Gagal menghapus user.", "error"));
            }
        });
    });
});