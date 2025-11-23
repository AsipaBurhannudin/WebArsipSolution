document.addEventListener("DOMContentLoaded", function () {

    const passwordInput = document.getElementById("NewPassword");
    const confirmInput = document.getElementById("ConfirmPassword");
    const saveButton = document.getElementById("ResetBtn");
    const strengthBar = document.getElementById("StrengthBar");

    const ruleLength = document.getElementById("rule-length");
    const ruleNumber = document.getElementById("rule-number");
    const ruleUpper = document.getElementById("rule-upper");

    const confirmMessage = document.getElementById("ConfirmMessage");
    const showPassChk = document.getElementById("ShowPassword");

    let passwordScore = 0;

    if (!passwordInput) return;

    // Show / hide password
    showPassChk.addEventListener("change", () => {
        const type = showPassChk.checked ? "text" : "password";
        passwordInput.type = type;
        confirmInput.type = type;
    });

    // Validate password rules
    const validatePassword = () => {
        const val = passwordInput.value;
        let score = 0;

        // 1. Minimal 6 karakter
        if (val.length >= 6) { ruleLength.classList.add("valid"); score++; }
        else ruleLength.classList.remove("valid");

        // 2. Mengandung angka
        if (/[0-9]/.test(val)) { ruleNumber.classList.add("valid"); score++; }
        else ruleNumber.classList.remove("valid");

        // 3. Mengandung huruf kapital
        if (/[A-Z]/.test(val)) { ruleUpper.classList.add("valid"); score++; }
        else ruleUpper.classList.remove("valid");

        passwordScore = score;

        // Update strength bar
        strengthBar.className = "strength-level";
        strengthBar.style.width = (score * 33.3) + "%";

        if (score === 1) strengthBar.classList.add("strength-weak");
        if (score === 2) strengthBar.classList.add("strength-medium");
        if (score === 3) strengthBar.classList.add("strength-strong");

        validateConfirm();
    };

    // Validate confirm password
    const validateConfirm = () => {
        if (confirmInput.value.length === 0) {
            confirmMessage.textContent = "";
            toggleSubmit();
            return;
        }

        if (passwordInput.value === confirmInput.value) {
            confirmMessage.textContent = "✔ Password cocok!";
            confirmMessage.classList.remove("text-danger");
            confirmMessage.classList.add("text-success");
        } else {
            confirmMessage.textContent = "✖ Password tidak sama.";
            confirmMessage.classList.add("text-danger");
            confirmMessage.classList.remove("text-success");
        }

        toggleSubmit();
    };

    // Show / Hide New Password
    document.getElementById("ShowPassword").addEventListener("change", function () {
        const pwd = document.getElementById("NewPassword");
        pwd.type = this.checked ? "text" : "password";
    });

    // Show / Hide Confirm Password
    document.getElementById("ShowConfirmPassword").addEventListener("change", function () {
        const pwd = document.getElementById("ConfirmPassword");
        pwd.type = this.checked ? "text" : "password";
    });

    // Enable/disable submit button
    const toggleSubmit = () => {
        const confirmOk = passwordInput.value === confirmInput.value;
        saveButton.disabled = !(passwordScore === 3 && confirmOk);
    };

    passwordInput.addEventListener("input", validatePassword);
    confirmInput.addEventListener("input", validateConfirm);

});