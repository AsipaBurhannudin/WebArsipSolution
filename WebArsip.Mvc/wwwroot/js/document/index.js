$(document).ready(function () {

    // ============================================================
    // DATATABLES INIT
    // ============================================================
    const table = $('#documentTable').DataTable({
        pageLength: 5,
        responsive: {
            details: {
                display: $.fn.dataTable.Responsive.display.childRowImmediate,
                type: 'none'
            }
        },
        autoWidth: false,
    });

    // Re-render numbering after sort/search
    table.on('order.dt search.dt draw.dt', function () {
        table.column(0, { search: 'applied', order: 'applied' })
            .nodes()
            .each((cell, i) => { cell.innerHTML = i + 1; });
    }).draw();


    // ============================================================
    // 🔴  DELETE DOCUMENT
    // ============================================================
    $(document).on('click', '.delete-btn', function (e) {
        e.preventDefault();

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
            if (!result.isConfirmed) return;

            Swal.fire({
                title: 'Menghapus...',
                html: `<div class="spinner-border text-danger" role="status"></div>`,
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

                    // FIX: handle row deletion even if inside responsive detail
                    const row = table.row(btn.parents('tr'));
                    row.remove().draw(false);

                } else {
                    Swal.fire("Gagal", res.message, "error");
                }
            }).fail(() => {
                Swal.fire("Error", "Terjadi kesalahan server.", "error");
            });
        });
    });


    // ============================================================
    // ⬇ DOWNLOAD DOCUMENT
    // ============================================================
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
            if (!result.isConfirmed) return;

            Swal.fire({
                title: 'Downloading...',
                html: `<div class="spinner-border text-primary" role="status"></div>`,
                showConfirmButton: false,
                allowOutsideClick: false
            });

            setTimeout(() => {
                window.location.href = `/Document/Download/${id}`;
                Swal.close();
            }, 800);
        });
    });


    // ============================================================
    // 👁 PREVIEW DOCUMENT
    // ============================================================
    $(document).on('click', '.btn-preview', function () {
        const docId = $(this).data("id");
        const filePath = $(this).data("filepath");
        const ext = filePath.split('.').pop().toLowerCase();
        let srcUrl = "";

        if (ext === "pdf") {
            srcUrl = `/Document/Preview/${docId}`;
        }
        else if (["doc", "docx", "xls", "xlsx"].includes(ext)) {
            const encoded = encodeURIComponent(`/Document/Preview/${docId}`);
            srcUrl = `https://view.officeapps.live.com/op/embed.aspx?src=${encoded}`;
        }
        else {
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


    // ============================================================
    // 🔐 PERMISSION AUTO-HIDE ACTION BUTTONS
    // ============================================================
    async function refreshUserPermissions() {
        try {
            const email = $('body').data('user-email');
            const role = $('body').data('user-role');

            if (!email || !role) return;

            const res = await fetch(`/api/permission/check?email=${email}&role=${role}`);
            if (!res.ok) return;

            const p = await res.json();

            if (!p.CanEdit) $(".btn-warning").hide();
            if (!p.CanDelete) $(".btn-danger").hide();
            if (!p.CanView) $(".btn-info").hide();
            if (!p.CanDownload) $(".btn-secondary").hide();
            if (!p.CanUpload) $("a[href$='Create']").hide();

        } catch (err) {
            console.error("Permission refresh failed:", err);
        }
    }

    setTimeout(refreshUserPermissions, 1500);
});