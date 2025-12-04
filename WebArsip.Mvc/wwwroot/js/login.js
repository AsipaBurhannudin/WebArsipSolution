document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("loginForm");
    const emailInput = document.getElementById("email");
    const passwordInput = document.getElementById("password");
    const loginBtn = document.getElementById("loginBtn");

    const Toast = Swal.mixin({
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 2500,
        timerProgressBar: true,
    });

    const setLoading = (isLoading) => {
        if (isLoading) {
            loginBtn.classList.add("loading");
            loginBtn.disabled = true;
        } else {
            loginBtn.classList.remove("loading");
            loginBtn.disabled = false;
        }
    };

    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        const email = emailInput.value.trim();
        const password = passwordInput.value.trim();

        // 🔹 Validasi input
        if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            Toast.fire({ icon: "error", title: "Format email tidak valid" });
            return;
        }
        if (password.length < 6) {
            Toast.fire({ icon: "error", title: "Password minimal 6 karakter" });
            return;
        }

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
                const card = document.querySelector(".login-card");
                card.style.animation = "shake 0.5s";
                setTimeout(() => (card.style.animation = ""), 500);
            }
        } catch (err) {
            Toast.fire({ icon: "error", title: "Terjadi kesalahan koneksi" });
        } finally {
            setLoading(false);
        }
    });

    const shakeForm = () => {
        const card = document.querySelector(".login-card");
        if (card) {
            card.style.animation = "shake 0.5s";
            setTimeout(() => (card.style.animation = ""), 500);
        }
    };

    // 🔹 Toggle Password Visibility
    const pwdToggle = document.getElementById("passwordToggle");
    if (pwdToggle) {
        pwdToggle.addEventListener("click", () => {
            const isHidden = password.type === "password";
            password.type = isHidden ? "text" : "password";
            pwdToggle.classList.toggle("active", isHidden);
        });
    }

    // 🔹 Fade-in Animasi Halus
    const loginCard = document.querySelector(".login-card");
    if (loginCard) {
        loginCard.style.opacity = 0;
        loginCard.style.transform = "translateY(15px)";
        setTimeout(() => {
            loginCard.style.transition = "all 0.6s ease";
            loginCard.style.opacity = 1;
            loginCard.style.transform = "translateY(0)";
        }, 100);
    }
});