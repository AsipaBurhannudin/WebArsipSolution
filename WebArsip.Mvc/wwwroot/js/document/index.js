$(document).ready(function () {
    const table = $('#documentTable').DataTable({
        pageLength: 5,
        responsive: true,
        autoWidth: false
    });

    table.on('order.dt search.dt', function () {
        table.column(0, { search: 'applied', order: 'applied' })
            .nodes()
            .each((cell, i) => { cell.innerHTML = i + 1; });
    }).draw();

    // 🗑 DELETE
    $(document).on('click', '.delete-btn', function () {
        const btn = $(this);
        const id = btn.data('id');
        const title = btn.data('title');

        Swal.fire({
            title: "Yakin ingin hapus?",
            text: `Dokumen "${title}" akan dihapus permanen.`,
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Ya, hapus!",
            cancelButtonText: "Batal",
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: 'Menghapus...',
                    html: '<div class="spinner-border text-danger" role="status"></div>',
                    showConfirmButton: false,
                    allowOutsideClick: false
                });

                $.post(`/Document/Delete/${id}`, function (res) {
                    Swal.close();
                    if (res.success) {
                        Swal.fire({
                            icon: "success",
                            title: res.message,
                            timer: 1500,
                            showConfirmButton: false
                        });
                        table.row(btn.closest('tr')).remove().draw(false);
                    } else {
                        Swal.fire("Gagal", res.message, "error");
                    }
                }).fail(() => Swal.fire("Error", "Terjadi kesalahan server.", "error"));
            }
        });
    });

    // ⬇ DOWNLOAD
    $(document).on('click', '.btn-download', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        const name = $(this).data('name');

        Swal.fire({
            title: 'Download File?',
            text: `Ingin download dokumen "${name}"?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Ya, download',
            cancelButtonText: 'Batal'
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: 'Downloading...',
                    html: '<div class="spinner-border text-primary" role="status"></div>',
                    showConfirmButton: false,
                    allowOutsideClick: false
                });

                setTimeout(() => {
                    window.location.href = `/Document/Download/${id}`;
                    Swal.close();
                    Swal.fire({
                        icon: 'success',
                        title: 'Download Sukses!',
                        timer: 1800,
                        showConfirmButton: false
                    });
                }, 1000);
            }
        });
    });

    // 👁 PREVIEW
    $(document).on('click', '.btn-preview', function () {
        const docId = $(this).data("id");
        const filePath = $(this).data("filepath");
        const ext = filePath.split('.').pop().toLowerCase();
        let srcUrl = "";

        if (ext === "pdf") {
            srcUrl = `/Document/Preview/${docId}`;
        } else if (["doc", "docx", "xls", "xlsx"].includes(ext)) {
            const encoded = encodeURIComponent(`/Document/Preview/${docId}`);
            srcUrl = `https://view.officeapps.live.com/op/embed.aspx?src=${encoded}`;
        } else {
            Swal.fire({
                icon: "warning",
                title: "File tidak bisa dipreview",
                text: "Tipe file ini tidak mendukung preview."
            });
            return;
        }

        $("#previewFrame").attr("src", srcUrl);
        $("#previewModal").modal("show");
    });
});