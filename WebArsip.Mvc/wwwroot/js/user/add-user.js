// ===========================
// PASSWORD VALIDATION SYSTEM
// ===========================

const passInput = document.getElementById("Password");
const confirmInput = document.getElementById("ConfirmPassword");
const btnSave = document.getElementById("btnSave");

const chkLength = document.getElementById("chkLength");
const chkNumber = document.getElementById("chkNumber");
const chkUpper = document.getElementById("chkUpper");

const strengthBar = document.getElementById("StrengthBar");
const confirmMessage = document.getElementById("confirmMessage");

function validatePassword() {
    const pwd = passInput.value;

    // Condition checks
    const hasLength = pwd.length >= 6;
    const hasNumber = /\d/.test(pwd);
    const hasUpper = /[A-Z]/.test(pwd);

    updateCheck(chkLength, hasLength);
    updateCheck(chkNumber, hasNumber);
    updateCheck(chkUpper, hasUpper);

    // Calculate strength %
    const validCount = [hasLength, hasNumber, hasUpper].filter(x => x).length;
    const percent = (validCount / 3) * 100;

    strengthBar.style.width = percent + "%";
    strengthBar.style.background =
        percent < 40 ? "red" :
            percent < 80 ? "orange" : "green";

    validateSubmit();
}

function updateCheck(element, isValid) {
    const label = element.getAttribute("data-label");

    element.classList.toggle("valid", isValid);
    element.classList.toggle("invalid", !isValid);

    element.innerHTML = isValid
        ? `<i class="bi bi-check-circle"></i> ${label}`
        : `<i class="bi bi-x-circle"></i> ${label}`;
}

function validateConfirm() {
    const pwd = passInput.value;
    const conf = confirmInput.value;

    if (!conf) {
        confirmMessage.textContent = "Confirm password cannot be empty";
    }
    else if (conf !== pwd) {
        confirmMessage.textContent = "Password doesn't match";
    }
    else {
        confirmMessage.textContent = "";
    }

    validateSubmit();
}

function validateSubmit() {
    const hasInvalid = document.querySelector(".password-checklist .invalid");
    const confFilled = confirmInput.value !== "";
    const confValid = confirmMessage.textContent === "";

    const allValid = !hasInvalid && confFilled && confValid;

    btnSave.disabled = !allValid;
}

// ===============================
// SHOW/HIDE PASSWORD BUTTONS
// ===============================

// Main password toggle
const pwdInput = document.getElementById("Password");
const pwdToggle = document.getElementById("togglePwd");

if (pwdToggle && pwdInput) {
    pwdToggle.addEventListener("click", () => {
        const hidden = pwdInput.type === "password";
        pwdInput.type = hidden ? "text" : "password";
        pwdToggle.innerHTML = hidden
            ? '<i class="bi bi-eye-slash"></i>'
            : '<i class="bi bi-eye"></i>';
    });
}

// Confirm password toggle
const confInput = document.getElementById("ConfirmPassword");
const confToggle = document.getElementById("toggleConfirmPwd");

if (confToggle && confInput) {
    confToggle.addEventListener("click", () => {
        const hidden = confInput.type === "password";
        confInput.type = hidden ? "text" : "password";
        confToggle.innerHTML = hidden
            ? '<i class="bi bi-eye-slash"></i>'
            : '<i class="bi bi-eye"></i>';
    });
}

// Input events
passInput.addEventListener("input", validatePassword);
confirmInput.addEventListener("input", validateConfirm);

// ===========================
// SWEETALERT EXISTING
// ===========================
function handleFormSubmit(formId) {
    const form = document.getElementById(formId);
    form.addEventListener("submit", function () {
        Swal.fire({
            title: "Processing...",
            html: '<div class="spinner-border text-info" role="status"></div>',
            showConfirmButton: false,
            allowOutsideClick: false
        });
    });
}

handleFormSubmit("addUserForm");