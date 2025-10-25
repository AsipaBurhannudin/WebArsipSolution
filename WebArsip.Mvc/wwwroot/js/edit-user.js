document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("editUserForm");
    const btnSave = document.getElementById("btnSave");

    // ===== VALIDASI DAN SUBMIT EDIT =====
    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const name = document.getElementById("Name").value.trim();
        const email = document.getElementById("Email").value.trim();

        if (name.length < 4) {
            Swal.fire("Nama Terlalu Pendek", "Nama minimal 4 karakter.", "warning");
            return;
        }
        if (!email.includes("@") || (!email.endsWith(".com") && !email.endsWith(".co.id"))) {
            Swal.fire("Format Email Salah", "Email harus mengandung '@' dan diakhiri .com atau .co.id", "error");
            return;
        }

        Swal.fire({
            title: "Menyimpan perubahan...",
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });

        const formData = new FormData(form);
        const response = await fetch(form.action, { method: "POST", body: formData });

        Swal.close();

        if (response.ok) {
            Swal.fire({
                icon: "success",
                title: "User berhasil diperbarui!",
                timer: 2000,
                showConfirmButton: false
            }).then(() => window.location.href = "/User/Index");
        } else {
            Swal.fire({ icon: "error", title: "Gagal", text: "Terjadi kesalahan saat menyimpan." });
        }
    });

    // ===== VERIFIKASI ADMIN PASSWORD =====
    const showBtn = document.getElementById("showPasswordBtn");
    const pwdField = document.getElementById("userPassword");

    if (showBtn && pwdField) {
        showBtn.addEventListener("click", async () => {
            const { value: adminPass, isConfirmed } = await Swal.fire({
                title: "Verifikasi Admin",
                input: "password",
                inputLabel: "Masukkan password admin:",
                inputPlaceholder: "Password admin",
                showCancelButton: true,
                confirmButtonText: "Verifikasi",
                cancelButtonText: "Batal",
                preConfirm: (value) => {
                    if (!value || value.trim().length < 6) {
                        Swal.showValidationMessage("Password admin minimal 6 karakter");
                        return false;
                    }
                    return value;
                }
            });

            if (!isConfirmed) return;

            Swal.fire({
                title: "Memverifikasi...",
                text: "Mohon tunggu sebentar",
                allowOutsideClick: false,
                didOpen: () => Swal.showLoading()
            });

            try {
                const token = window.JWT_TOKEN;
                if (!token) throw new Error("Token JWT tidak ditemukan. Silakan login ulang.");

                const apiUrl = `${window.API_BASE_URL}auth/verify-admin-password`;
                const response = await fetch(apiUrl, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": `Bearer ${token}`
                    },
                    body: JSON.stringify({ password: adminPass })
                });

                Swal.close();

                if (!response.ok) {
                    const errJson = await response.json().catch(() => null);
                    throw new Error(errJson?.message || `Error ${response.status}`);
                }

                const result = await response.json();
                if (result.success) {
                    Swal.fire({
                        icon: "success",
                        title: "Verifikasi berhasil!",
                        timer: 1200,
                        showConfirmButton: false
                    });
                    pwdField.type = pwdField.type === "password" ? "text" : "password";
                    showBtn.innerHTML = pwdField.type === "text" ? "🙈 Hide" : "👁 Show";
                } else {
                    Swal.fire({
                        icon: "error",
                        title: "Gagal",
                        text: result.message || "Password admin salah."
                    });
                }
            } catch (err) {
                Swal.close();
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: err.message.includes("fetch")
                        ? "Tidak dapat menghubungi server. Pastikan API berjalan."
                        : err.message
                });
            }
        });
    }

    // ===== TOAST FEEDBACK =====
    const successMsg = document.body.dataset.success;
    const errorMsg = document.body.dataset.error;

    if (successMsg)
        Swal.fire({ icon: "success", title: successMsg, timer: 2000, showConfirmButton: false });
    if (errorMsg)
        Swal.fire({ icon: "error", title: "Gagal", text: errorMsg });
});