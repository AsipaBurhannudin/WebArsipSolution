document.addEventListener("DOMContentLoaded", () => {
    window.API_BASE_URL = `${window.location.origin}/api`;
    const form = document.getElementById("addUserForm");
    const btnSave = document.getElementById("btnSave");

    const setLoading = (state) => {
        btnSave.disabled = state;
        btnSave.innerHTML = state ? "⏳ Saving..." : "💾 Save";
    };

    form.addEventListener("submit", async (e) => {
        e.preventDefault();

        const name = document.getElementById("Name").value.trim();
        const email = document.getElementById("Email").value.trim();
        const password = document.getElementById("Password").value.trim();

        // 🔹 Validasi Nama
        if (name.length < 4) {
            Swal.fire("Nama Terlalu Pendek", "Nama minimal 4 karakter.", "warning");
            return;
        }

        // 🔹 Validasi Email
        if (!email.includes("@") || (!email.endsWith(".com") && !email.endsWith(".co.id"))) {
            Swal.fire("Format Email Salah", "Email harus mengandung '@' dan diakhiri .com atau .co.id", "error");
            return;
        }

        // 🔹 Validasi Password Kosong
        if (!password) {
            Swal.fire("Password Kosong", "Password tidak boleh kosong.", "warning");
            return;
        }

        // 🔹 Validasi Password Lemah
        const strongRegex = /^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).{6,}$/;
        if (!strongRegex.test(password)) {
            Swal.fire(
                "Password Lemah",
                "Password minimal 6 karakter dan harus memiliki 1 huruf kapital, 1 angka, dan 1 simbol khusus.",
                "warning"
            );
            return;
        }

        setLoading(true);

        try {
            // 🔹 Kirim form data
            const formData = new FormData(form);
            const response = await fetch(form.action, { method: "POST", body: formData });

            if (response.ok) {
                Swal.fire({
                    icon: "success",
                    title: "User Berhasil Ditambahkan!",
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    window.location.href = "/User/Index";
                });
            } else {
                Swal.fire("Gagal", "Terjadi kesalahan saat menyimpan data.", "error");
            }
        } catch (err) {
            Swal.fire("Error", "Tidak dapat menghubungi server. Pastikan API berjalan.", "error");
        } finally {
            setLoading(false);
        }
    });

    // 🔹 Tampilkan notifikasi dari TempData
    const successMsg = document.body.dataset.success;
    const errorMsg = document.body.dataset.error;

    if (successMsg)
        Swal.fire({ icon: "success", title: successMsg, timer: 2000, showConfirmButton: false });

    if (errorMsg)
        Swal.fire({ icon: "error", title: "Gagal", text: errorMsg });
});