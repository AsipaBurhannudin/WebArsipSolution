document.addEventListener("DOMContentLoaded", function () {
    const alertData = document.getElementById("alert-data");
    const success = alertData?.dataset.success;
    const error = alertData?.dataset.error;

    if (success) {
        Swal.fire({
            icon: "success",
            title: "Berhasil!",
            text: success,
            timer: 2000,
            showConfirmButton: false
        });
    } else if (error) {
        Swal.fire({
            icon: "error",
            title: "Gagal!",
            text: error,
            timer: 2500,
            showConfirmButton: false
        });
    }

    // ✅ Confirm before save changes
    const form = document.getElementById("editPermissionForm");
    if (form) {
        form.addEventListener("submit", function (e) {
            e.preventDefault();
            Swal.fire({
                title: "Simpan perubahan?",
                text: "Perubahan permission akan segera diterapkan.",
                icon: "question",
                showCancelButton: true,
                confirmButtonText: "Ya, simpan!",
                cancelButtonText: "Batal",
                reverseButtons: true
            }).then(result => {
                if (result.isConfirmed) {
                    form.submit();
                }
            });
        });
    }
});