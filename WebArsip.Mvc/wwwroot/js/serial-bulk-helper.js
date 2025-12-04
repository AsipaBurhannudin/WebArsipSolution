// =========================
// BULK DOCUMENT SERIAL HANDLER
// =========================

document.addEventListener("DOMContentLoaded", () => {
    const dropdown = document.querySelector(".serial-format-selector[data-mode='bulk']");
    const table = document.getElementById("bulkTable");
    if (!dropdown || !table) return;

    // get all title inputs
    const getAllTitleInputs = () => [...document.querySelectorAll(".doc-title")];

    // FETCH FORMAT KEY
    async function getFormatKey(id) {
        const resp = await fetch(`/SerialNumber/GetFormatKey?id=${id}`);
        return await resp.json();
    }

    // CALL PREVIEW API
    async function previewSerial(key) {
        const resp = await fetch(`/SerialNumber/PreviewSerial`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ key })
        });

        return await resp.json();
    }

    // APPLY preview to all rows
    async function applyPreviewToAllRows() {
        const id = dropdown.value;
        if (!id) return;

        const info = await getFormatKey(id);
        if (!info.success) return;

        const key = info.key;

        const prev = await previewSerial(key);
        if (!prev.success) return;

        const generated = prev.generated;

        // apply to all titles
        getAllTitleInputs().forEach(input => {
            input.value = generated;
        });
    }

    // When selecting a format
    dropdown.addEventListener("change", async function () {
        await applyPreviewToAllRows();
    });

    // Watch new rows added dynamically
    const observer = new MutationObserver(async () => {
        if (!dropdown.value) return;
        await applyPreviewToAllRows();
    });

    observer.observe(table.querySelector("tbody"), { childList: true });

});