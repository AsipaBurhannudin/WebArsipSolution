document.addEventListener("DOMContentLoaded", () => {
    const forms = document.querySelectorAll("form");
    forms.forEach(form => {
        form.addEventListener("submit", () => {
            Swal.fire({
                title: "Processing...",
                html: '<div class="spinner-border text-info" role="status"></div>',
                showConfirmButton: false,
                allowOutsideClick: false
            });
        });
    });
});