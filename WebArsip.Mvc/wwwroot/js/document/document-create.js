document.addEventListener("DOMContentLoaded", function () {

    // SweetAlert TempData
    const success = document.body.dataset.success;
    const error = document.body.dataset.error;

    if (success) {
        Swal.fire({
            icon: "success",
            title: "Success!",
            text: success,
            timer: 2000,
            showConfirmButton: false
        });
    } else if (error) {
        Swal.fire({
            icon: "error",
            title: "Failed!",
            text: error,
            timer: 2500,
            showConfirmButton: false
        });
    }

    // ===============================
    // SERIAL FORMAT AUTO TITLE PREVIEW (NO INCREMENT)
    // ===============================
    const ddl = document.getElementById("SerialFormatId");
    const titleBox = document.getElementById("AutoTitle");

    ddl.addEventListener("change", function () {

        const formatId = ddl.value;
        if (!formatId) return;

        // Step 1: ambil key format
        fetch(`/SerialNumber/GetFormatKey?id=${formatId}`)
            .then(r => r.json())
            .then(f => {

                // Step 2: request preview ke MVC (bukan direct API)
                fetch(`/SerialNumber/PreviewSerial?key=${f.key}`)
                    .then(res => res.json())
                    .then(result => {
                        if (result?.generated) {
                            titleBox.value = result.generated;
                        } else {
                            console.warn("Preview returned no generated value:", result);
                        }
                    })
                    .catch(err => console.error("Preview error:", err));
            });
    });

    // ===============================
    // BOOTSTRAP VALIDATION
    // ===============================
    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();

                Swal.fire({
                    icon: 'warning',
                    title: 'Invalid!',
                    text: 'Please check the form and try again.',
                    timer: 2000,
                    showConfirmButton: false
                });
            }
            form.classList.add('was-validated');
        }, false);
    });

});