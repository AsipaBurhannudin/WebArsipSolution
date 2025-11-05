// 🌈 SweetAlert modular helper
function showAlert(type, title, text) {
    Swal.fire({
        icon: type,
        title: title,
        text: text,
        showConfirmButton: false,
        timer: 1800,
        background: "rgba(30,30,30,0.9)",
        color: "#f8f8f8",
        backdrop: "rgba(0,0,0,0.4)",
        customClass: {
            popup: "rounded-4 shadow-lg"
        }
    });
}