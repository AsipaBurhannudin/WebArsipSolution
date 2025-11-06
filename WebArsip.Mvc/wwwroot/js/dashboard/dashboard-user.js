document.addEventListener("DOMContentLoaded", async () => {
    const token = sessionStorage.getItem("JWToken") || localStorage.getItem("JWToken") || window.JWT_TOKEN;
    const baseUrl = "http://localhost:5287/api";

    if (!token) {
        Swal.fire("Unauthorized", "Silakan login kembali.", "warning");
        return;
    }

    async function fetchJson(url) {
        console.log("Fetching:", `${baseUrl}/${url}`);
        const res = await fetch(`${baseUrl}/${url}`, {
            headers: { "Authorization": `Bearer ${token}` }
        });
        if (!res.ok) throw new Error(`${url} gagal dimuat (${res.status})`);
        return await res.json();
    }

    function animateCount(el, target) {
        if (!el) return;
        let current = 0;
        const increment = Math.max(1, Math.ceil(target / 60));
        const interval = setInterval(() => {
            current += increment;
            if (current >= target) {
                el.textContent = target.toLocaleString();
                clearInterval(interval);
            } else {
                el.textContent = current.toLocaleString();
            }
        }, 16);
    }

    function fadeIn(el) {
        el.style.opacity = 0;
        el.style.transform = "translateY(10px)";
        el.style.transition = "opacity 0.6s ease, transform 0.6s ease";
        requestAnimationFrame(() => {
            el.style.opacity = 1;
            el.style.transform = "translateY(0)";
        });
    }

    function showShimmer(el) {
        el.innerHTML = `<div class="shimmer"></div>`;
    }

    try {
        document.querySelectorAll(".count-value").forEach(showShimmer);

        const [counts, activity] = await Promise.all([
            fetchJson("Dashboard/counts"),
            fetchJson("Dashboard/user-activity?days=7")
        ]);

        animateCount(document.getElementById("userDocCount"), counts.documents);
        animateCount(document.getElementById("userLogCount"), counts.auditLogs);

        document.querySelectorAll(".count-value").forEach(fadeIn);

        const grouped = activity.reduce((acc, s) => {
            acc[s.date] = (acc[s.date] || 0) + s.count;
            return acc;
        }, {});
        const labels = Object.keys(grouped);
        const data = Object.values(grouped);

        const ctx = document.getElementById("userActivityChart");
        fadeIn(ctx);

        new Chart(ctx, {
            type: "line",
            data: {
                labels,
                datasets: [{
                    label: "Aktivitas Harian",
                    data,
                    borderColor: "#4fc3f7",
                    backgroundColor: "rgba(79,195,247,0.25)",
                    borderWidth: 2,
                    fill: true,
                    tension: 0.35
                }]
            },
            options: {
                animation: { duration: 1200, easing: "easeOutQuart" },
                plugins: { legend: { position: "bottom", labels: { color: "#fff" } } },
                scales: {
                    y: { beginAtZero: true, ticks: { color: "#fff" } },
                    x: { ticks: { color: "#fff" } }
                }
            }
        });
    } catch (err) {
        console.error("Gagal memuat dashboard user:", err);
        Swal.fire("Gagal Memuat", err.message, "error");
    }
});