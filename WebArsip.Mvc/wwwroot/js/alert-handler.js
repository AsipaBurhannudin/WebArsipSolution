document.addEventListener("DOMContentLoaded", () => {
    const successMsg = document.querySelector("meta[name='TempData-Success']")?.content;
    const errorMsg = document.querySelector("meta[name='TempData-Error']")?.content;

    if (successMsg) {
        Swal.fire({
            icon: "success",
            title: "Berhasil!",
            text: successMsg,
            showConfirmButton: false,
            timer: 2000
        });
    }

    if (errorMsg) {
        Swal.fire({
            icon: "error",
            title: "Gagal!",
            text: errorMsg,
            showConfirmButton: true
        });
    }
});