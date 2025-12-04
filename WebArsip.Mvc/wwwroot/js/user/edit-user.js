// Reusable form submission alert
function handleFormSubmit(formId, successMsg) {
    const form = document.getElementById(formId);
    form.addEventListener('submit', function (e) {
        Swal.fire({
            title: 'Processing...',
            html: '<div class="spinner-border text-info" role="status"></div>',
            showConfirmButton: false,
            allowOutsideClick: false
        });
    });
}

// Add user
handleFormSubmit("addUserForm", "User added successfully!");

// Edit user
handleFormSubmit("editUserForm", "User updated successfully!");

// Show password toggle
const showBtn = document.getElementById("showPasswordBtn");
if (showBtn) {
    showBtn.addEventListener("click", () => {
        const passInput = document.getElementById("userPassword");
        const isHidden = passInput.type === "password";
        passInput.type = isHidden ? "text" : "password";
        showBtn.innerHTML = isHidden ? '<i class="bi bi-eye-slash"></i> Hide' : '<i class="bi bi-eye"></i> Show';
    });
}