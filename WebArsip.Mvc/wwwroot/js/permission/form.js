document.addEventListener("DOMContentLoaded", function () {
    // Animate checkbox highlight
    document.querySelectorAll(".permission-toggle").forEach(chk => {
        chk.addEventListener("change", e => {
            const label = e.target.nextElementSibling;
            if (e.target.checked) {
                label.classList.add("text-success", "fw-bold");
            } else {
                label.classList.remove("text-success", "fw-bold");
            }
        });
    });

    // SweetAlert success/error
    const success = '@TempData["Success"]';
    const error = '@TempData["Error"]';
    if (success) {
        Swal.fire({ icon: "success", title: "Berhasil!", text: success, timer: 2000, showConfirmButton: false });
    } else if (error) {
        Swal.fire({ icon: "error", title: "Gagal!", text: error, timer: 2500, showConfirmButton: false });
    }
});