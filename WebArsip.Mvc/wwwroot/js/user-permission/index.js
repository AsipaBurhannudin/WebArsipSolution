// ⚙️ DataTables initialization
$(document).ready(function () {
    const table = $('#userPermissionTable').DataTable({
        pageLength: 10,
        order: [[0, 'asc']], // ✅ urut berdasarkan nomor (kolom pertama)
        language: {
            search: "Search:",
            lengthMenu: "Show _MENU_ entries",
            zeroRecords: "No matching records found",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            infoEmpty: "No entries available",
            paginate: { next: "›", previous: "‹" }
        }
    });

    // ✅ SweetAlert notification setelah tambah/edit permission
    const successMsg = $('#alert-data').data('success');
    const errorMsg = $('#alert-data').data('error');

    if (successMsg) {
        Swal.fire({
            icon: "success",
            title: "Berhasil!",
            text: successMsg,
            timer: 2000,
            showConfirmButton: false
        });
    } else if (errorMsg) {
        Swal.fire({
            icon: "error",
            title: "Gagal!",
            text: errorMsg,
            timer: 2500,
            showConfirmButton: false
        });
    }
});

// 🧨 Delete User Permission
function deletePermission(id) {
    Swal.fire({
        title: "Yakin ingin menghapus permission ini?",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Ya, hapus!",
        cancelButtonText: "Batal",
        reverseButtons: true
    }).then(result => {
        if (result.isConfirmed) {
            fetch(`/UserPermission/Delete/${id}`, { method: "POST" })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire({
                            icon: "success",
                            title: "Berhasil!",
                            text: data.message,
                            timer: 1500,
                            showConfirmButton: false
                        }).then(() => location.reload());
                    } else {
                        Swal.fire({
                            icon: "error",
                            title: "Gagal!",
                            text: data.message
                        });
                    }
                })
                .catch(() => {
                    Swal.fire({
                        icon: "error",
                        title: "Oops...",
                        text: "Terjadi kesalahan saat menghapus data."
                    });
                });
        }
    });
}