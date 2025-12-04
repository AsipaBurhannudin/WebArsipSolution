// =========================================
// serial-helper.js (FINAL FIXED VERSION)
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
                body: JSON.stringify({ key: key, date: null })
            });

            const j = await resp.json();
            return j?.success ? j.generated : null;
        } catch (e) {
            console.error("fetchPreviewSerial ERROR:", e);
            return null;
        }
    }

    /* ============================================================
       3) PATTERN PREVIEW RENDERER
          Supports new syntax:
          {DATE}, {DATE:yyyy}, {DATE:yy}, {DATE:MM}, {DATE:dd}
          {NUMBER:n}
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
            .replace(/\{NUMBER:(\d+)\}/g, (_, digits) => "0".repeat(digits)); // placeholder for live preview
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

            // Show pattern preview first
            if (format.pattern) {
                createTitleBox.value = renderPatternPreview(format.pattern);
            }

            // Replace with real serial preview
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

    async function addBulkRow() {
        const serialPreview = await generateBulkSerial();

        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><input name="Items[${rowIndex}].Title" class="form-control doc-title" value="${serialPreview ?? ""}" /></td>
            <td><input name="Items[${rowIndex}].Description" class="form-control" /></td>
            <td>
                <select name="Items[${rowIndex}].Status" class="form-select">
                    <option>Published</option>
                </select>
            </td>
            <td><input type="file" name="Items[${rowIndex}].FileUpload" class="form-control" /></td>
            <td><button type="button" class="btn btn-danger remove-row">X</button></td>
        `;
        bulkTableBody.appendChild(tr);

        rowIndex++;
    }

    // Format selection in Bulk Create
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

            // 1. Show pattern preview
            if (bulkPattern) {
                rows.forEach(r => r.value = renderPatternPreview(bulkPattern));
            }

            // 2. Replace with real number (POST to Preview API)
            for (const r of rows) {
                const real = await fetchPreviewSerial(bulkKey);
                if (real) r.value = real;
            }
        });
    }

    if (addRowBtn) addRowBtn.addEventListener("click", addBulkRow);

    // Remove row handler
    document.addEventListener("click", e => {
        if (e.target.classList.contains("remove-row")) {
            e.target.closest("tr")?.remove();
        }
    });
});