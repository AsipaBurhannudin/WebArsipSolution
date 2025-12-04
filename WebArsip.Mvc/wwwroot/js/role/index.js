$(document).ready(function () {
    const table = $('#roleTable').DataTable({
        pageLength: 10,
        responsive: true,
        autoWidth: false
    });

    // DELETE ACTION
    $(document).on('click', '.delete-btn', function () {
        const id = $(this).data('id');
        const name = $(this).data('name');

        Swal.fire({
            title: 'Yakin ingin hapus?',
            text: `Role "${name}" akan dihapus permanen.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Ya, hapus!',
            cancelButtonText: 'Batal',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: 'Menghapus...',
                    html: '<div class="spinner-border text-danger" role="status"></div>',
                    showConfirmButton: false,
                    allowOutsideClick: false
                });

                $.post(`/Role/Delete/${id}`, function (res) {
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
                        Swal.fire("Gagal", res.message, "error");
                    }
                }).fail(() => Swal.fire("Error", "Terjadi kesalahan server.", "error"));
            }
        });
    });
});
