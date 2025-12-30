document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("loginForm");
    const emailInput = document.getElementById("email");
    const passwordInput = document.getElementById("password");
    const loginBtn = document.getElementById("loginBtn");
    const pwdToggle = document.getElementById("passwordToggle");
    const eyeIcon = document.querySelector(".eye-icon");

    // SweetAlert Toast
    const Toast = Swal.mixin({
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 2200,
        timerProgressBar: true,
    });

    // Loading button
    const setLoading = (isLoading) => {
        if (isLoading) {
            loginBtn.classList.add("loading");
            loginBtn.disabled = true;
        } else {
            loginBtn.classList.remove("loading");
            loginBtn.disabled = false;
        }
    };

    // ============ 1. VALIDASI KOSONG & EMAIL FORMAT ============
    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        const email = emailInput.value.trim();
        const password = passwordInput.value.trim();

        if (!email) {
            Toast.fire({ icon: "warning", title: "Email harus diisi!" });
            return;
        }

        if (!password) {
            Toast.fire({ icon: "warning", title: "Password harus diisi!" });
            return;
        }

        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            Toast.fire({ icon: "error", title: "Format email tidak valid" });
            return;
        }

        if (password.length < 6) {
            Toast.fire({ icon: "error", title: "Password minimal 6 karakter" });
            return;
        }

        // START LOGIN REQUEST
        setLoading(true);

        try {
            const response = await fetch("/Auth/Login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ Email: email, Password: password })
            });

            const result = await response.json();

            if (result.success) {
                Toast.fire({ icon: "success", title: result.message });
                setTimeout(() => {
                    window.location.href = result.redirectUrl;
                }, 800);
            } else {
                Toast.fire({ icon: "error", title: result.message || "Login gagal" });
                shakeForm();
            }
        } catch {
            Toast.fire({ icon: "error", title: "Terjadi kesalahan koneksi" });
        } finally {
            setLoading(false);
        }
    });

    // Shake animation
    const shakeForm = () => {
        const card = document.querySelector(".login-card");
        card.style.animation = "shake 0.5s";
        setTimeout(() => (card.style.animation = ""), 500);
    };

    // ============ 2. CAPSLOCK DETECTION ============
    passwordInput.addEventListener("keyup", (e) => {
        if (e.getModifierState("CapsLock")) {
            Toast.fire({
                icon: "info",
                title: "CapsLock sedang aktif!"
            });
        }
    });

    // ============ 3. TOGGLE PASSWORD VISIBILITY (eye icon) ============
    if (pwdToggle) {
        pwdToggle.addEventListener("click", () => {
            const isHidden = passwordInput.type === "password";
            passwordInput.type = isHidden ? "text" : "password";

            // Ganti class icon agar berubah bentuk
            if (isHidden) {
                eyeIcon.classList.add("open");     // icon mata terbuka
            } else {
                eyeIcon.classList.remove("open");  // icon mata tertutup
            }
        });
    }

    // ============ 4. Fade-in Animation ============
    const loginCard = document.querySelector(".login-card");
    loginCard.style.opacity = 0;
    loginCard.style.transform = "translateY(15px)";
    setTimeout(() => {
        loginCard.style.transition = "all 0.6s ease";
        loginCard.style.opacity = 1;
        loginCard.style.transform = "translateY(0)";
    }, 100);
});