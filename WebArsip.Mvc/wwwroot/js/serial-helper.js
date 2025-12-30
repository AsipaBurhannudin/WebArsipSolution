// =========================================
// serial-helper.js (FINAL + VALIDATION)
// =========================================

document.addEventListener("DOMContentLoaded", () => {

    /* ============================================================
       1) FETCH FORMAT (KEY + PATTERN)
    ============================================================ */
    async function fetchFormatInfo(id) {
        if (!id) return null;
        try {
            const resp = await fetch(`/SerialNumber/GetFormatKey?id=${id}`);
            const j = await resp.json();
            return j?.success ? j : null;
        } catch (e) {
            console.error("fetchFormatInfo ERROR:", e);
            return null;
        }
    }

    /* ============================================================
       2) FETCH SERIAL PREVIEW (POST → API)
    ============================================================ */
    async function fetchPreviewSerial(key) {
        if (!key) return null;

        try {
            const resp = await fetch(`/SerialNumber/PreviewSerial`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ Key: key, Date: null })

            });

            const j = await resp.json();
            return j?.success ? j.generated : null;
        } catch (e) {
            console.error("fetchPreviewSerial ERROR:", e);
            return null;
        }
    }

    /* ============================================================
       3) PATTERN PREVIEW
    ============================================================ */
    function renderPatternPreview(pattern) {
        if (!pattern) return "";

        const now = new Date();

        const yyyy = now.getFullYear();
        const yy = String(yyyy).slice(-2);
        const MM = String(now.getMonth() + 1).padStart(2, "0");
        const dd = String(now.getDate()).padStart(2, "0");

        return pattern
            .replace("{DATE}", `${yyyy}${MM}${dd}`)
            .replace("{DATE:yyyy}", yyyy)
            .replace("{DATE:yy}", yy)
            .replace("{DATE:MM}", MM)
            .replace("{DATE:dd}", dd)
            .replace(/\{NUMBER:(\d+)\}/g, (_, digits) => "0".repeat(digits));
    }

    /* ============================================================
       4) CREATE PAGE HANDLER
    ============================================================ */
    const ddlCreate = document.getElementById("SerialFormatId");
    const createTitleBox = document.getElementById("AutoTitle");

    if (ddlCreate && createTitleBox) {
        ddlCreate.addEventListener("change", async () => {
            const id = ddlCreate.value;
            if (!id) {
                createTitleBox.value = "";
                return;
            }

            const format = await fetchFormatInfo(id);
            if (!format) {
                createTitleBox.value = "";
                return;
            }

            if (format.pattern) {
                createTitleBox.value = renderPatternPreview(format.pattern);
            }

            const serial = await fetchPreviewSerial(format.key);
            if (serial) {
                createTitleBox.value = serial;
            }
        });
    }

    /* ============================================================
       5) BULK CREATE PAGE HANDLER
    ============================================================ */
    const bulkSelect = document.getElementById("SerialFormatId");
    const bulkTableBody = document.querySelector("#bulkTable tbody");
    const addRowBtn = document.getElementById("addRow");

    let bulkKey = null;
    let bulkPattern = null;
    let rowIndex = window.initialRowCount ?? 0;

    async function generateBulkSerial() {
        if (!bulkKey) return null;
        return await fetchPreviewSerial(bulkKey);
    }

    // Add Row
    async function addBulkRow() {
        const serialPreview = await generateBulkSerial();

        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><input name="Items[${rowIndex}].Title" class="form-control doc-title bg-dark text-light border-secondary" readonly value="${serialPreview ?? ""}"/></td>
            <td><input name="Items[${rowIndex}].Description" class="form-control bg-dark text-light border-secondary"  /></td>
            <td>
                <select name="Items[${rowIndex}].Status" class="form-select bg-dark text-light border-secondary">
                    <option>Published</option>
                </select>
            </td>
            <td>
                <input type="file" name="Items[${rowIndex}].FileUpload"
                       class="form-control file-input bg-dark text-light border-secondary"
                       accept=".pdf,.doc,.docx,.xls,.xlsx" required />
            </td>
            <td><button type="button" class="btn btn-danger remove-row rounded-pill px-3">X</button></td>
        `;
        bulkTableBody.appendChild(tr);

        rowIndex++;
    }

    // On format change (Bulk)
    if (bulkSelect) {
        bulkSelect.addEventListener("change", async () => {
            const id = bulkSelect.value;

            if (!id) {
                bulkKey = null;
                bulkPattern = null;
                document.querySelectorAll(".doc-title").forEach(t => t.value = "");
                return;
            }

            const format = await fetchFormatInfo(id);
            if (!format) return;

            bulkKey = format.key;
            bulkPattern = format.pattern;

            const rows = document.querySelectorAll(".doc-title");

            if (bulkPattern) {
                rows.forEach(r => r.value = renderPatternPreview(bulkPattern));
            }

            for (const r of rows) {
                const real = await fetchPreviewSerial(bulkKey);
                if (real) r.value = real;
            }
        });
    }

    if (addRowBtn) addRowBtn.addEventListener("click", addBulkRow);

    // Remove row
    document.addEventListener("click", e => {
        if (e.target.classList.contains("remove-row")) {
            e.target.closest("tr")?.remove();
        }
    });

    /* ============================================================
       6) VALIDATION (BulkCreate)
    ============================================================ */
    const bulkForm = document.getElementById("bulkForm");

    if (bulkForm) {
        bulkForm.addEventListener("submit", function (e) {

            let rows = document.querySelectorAll("#bulkTable tbody tr");
            let isValid = true;
            let msg = "";

            rows.forEach((row, i) => {  

                // File validation
                const fileInput = row.querySelector(".file-input");
                if (fileInput && fileInput.value) {
                    const allowed = ["pdf", "doc", "docx", "xls", "xlsx"];
                    const ext = fileInput.value.split('.').pop().toLowerCase();
                    if (!allowed.includes(ext)) {
                        isValid = false;
                        msg = "Format file tidak valid! Hanya PDF, DOC/DOCX, XLS/XLSX yang diperbolehkan.";
                    }
                }
            });

            if (!isValid) {
                e.preventDefault();
                Swal.fire({
                    icon: "error",
                    title: "Validation Error",
                    text: msg,
                    confirmButtonColor: "#d33"
                });
            }
        });
    }

});