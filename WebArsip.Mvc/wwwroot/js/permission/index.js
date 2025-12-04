$(document).ready(function () {
    window.deletePermission = function (id) {
        Swal.fire({
            title: "Yakin ingin menghapus permission ini?",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Ya, hapus!",
            cancelButtonText: "Batal",
            reverseButtons: true
        }).then(result => {
            if (result.isConfirmed) {
                fetch(`/Permission/Delete/${id}`, { method: "POST" })
                    .then(res => res.json())
                    .then(data => {
                        if (data.success) {
                            Swal.fire({
                                iconHtml: '<div class="swal2-success-circular"><div class="checkmark"></div></div>',
                                customClass: { icon: 'no-border' },
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
                            text: "Terjadi kesalahan pada server."
                        });
                    });
            }
        });
    };
});