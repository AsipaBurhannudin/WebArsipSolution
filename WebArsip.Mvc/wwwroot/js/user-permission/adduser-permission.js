// 🧩 Add User Permission Script

document.addEventListener("DOMContentLoaded", () => {
    // Animate permission toggles
    document.querySelectorAll(".form-check-input").forEach(chk => {
        const badge = chk.nextElementSibling;
        chk.addEventListener("change", () => {
            if (chk.checked) {
                badge.classList.add("shadow", "opacity-100");
                badge.style.transform = "scale(1.05)";
            } else {
                badge.classList.remove("shadow", "opacity-100");
                badge.style.transform = "scale(1)";
            }
        });
    });

    // Bootstrap validation
    const form = document.getElementById("userPermissionForm");
    form.addEventListener("submit", e => {
        if (!form.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
        }
        form.classList.add("was-validated");
    });
});