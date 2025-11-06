document.addEventListener("DOMContentLoaded", async () => {
    const token = sessionStorage.getItem("JWToken") || localStorage.getItem("JWToken") || window.JWT_TOKEN;
    const baseUrl = "http://localhost:5287/api";

    if (!token) {
        Swal.fire("Unauthorized", "Silakan login kembali.", "warning");
        return;
    }

    // 🪄 Helper: Fetch JSON
    async function fetchJson(url) {
        console.log("Fetching:", `${baseUrl}/${url}`);
        const res = await fetch(`${baseUrl}/${url}`, {
            headers: { "Authorization": `Bearer ${token}` }
        });
        if (!res.ok) throw new Error(`${url} gagal dimuat (${res.status})`);
        return await res.json();
    }

    // 🧮 Animasi angka naik
    function animateCount(el, target) {
        if (!el) return;
        let current = 0;
        const increment = Math.max(1, Math.ceil(target / 80));
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

    // 💫 Fade-in halus
    function fadeIn(el) {
        el.style.opacity = 0;
        el.style.transform = "translateY(10px)";
        el.style.transition = "opacity 0.6s ease, transform 0.6s ease";
        requestAnimationFrame(() => {
            el.style.opacity = 1;
            el.style.transform = "translateY(0)";
        });
    }

    // ✨ Shimmer loading (sementara sebelum data muncul)
    function showShimmer(el) {
        el.innerHTML = `<div class="shimmer"></div>`;
    }

    try {
        // Set shimmer di semua elemen count
        document.querySelectorAll(".count-value").forEach(showShimmer);

        const [counts, activity] = await Promise.all([
            fetchJson("Dashboard/counts"),
            fetchJson("Dashboard/user-activity?days=7")
        ]);

        // 🧾 Update angka count
        const mapping = {
            "countDocuments": counts.Documents || counts.documents || 0,
            "countUsers": counts.Users || counts.users || 0,
            "countRoles": counts.Roles || counts.roles || 0,
            "countPermissions": counts.Permissions || counts.permissions || 0,
            "countUserPermissions": counts.UserPermissions || counts.userPermissions || 0, // ✅ baru
            "countAuditLogs": counts.AuditLogs || counts.auditLogs || 0
        };

        for (const id in mapping) {
            const el = document.getElementById(id);
            if (el) {
                el.innerHTML = ""; // hapus shimmer
                animateCount(el, mapping[id]);
                fadeIn(el);
            }
        }

        // ---------- PIE CHART ----------
        const pieCtx = document.getElementById("countPieChart");
        fadeIn(pieCtx);
        new Chart(pieCtx, {
            type: "pie",
            data: {
                labels: ["Documents", "Users", "Roles", "Permissions", "User Permissions", "Audit Logs"],
                datasets: [{
                    data: Object.values(mapping),
                    backgroundColor: [
                        "rgba(54, 162, 235, 0.75)",   // Documents
                        "rgba(75, 192, 192, 0.75)",   // Users
                        "rgba(255, 206, 86, 0.75)",   // Roles
                        "rgba(255, 99, 132, 0.75)",   // Permissions
                        "rgba(55, 112, 219, 0.33)",  // User Permissions 💜
                        "rgba(153, 102, 255, 0.75)"   // Audit Logs
                    ],
                    borderColor: "#fff",
                    borderWidth: 2
                }]
            },
            options: {
                animation: { duration: 1200, easing: "easeOutQuart" },
                plugins: { legend: { position: "bottom", labels: { color: "#fff" } } }
            }
        });

        // ---------- LINE CHART ----------
        const labels = [...new Set(activity.map(s => s.date))];
        const grouped = activity.reduce((acc, s) => {
            if (!acc[s.action]) acc[s.action] = [];
            acc[s.action].push(s.count);
            return acc;
        }, {});
        const datasets = Object.keys(grouped).map((action, i) => ({
            label: action,
            data: grouped[action],
            borderWidth: 2,
            tension: 0.35,
            fill: true,
            borderColor: `hsl(${i * 70}, 80%, 60%)`,
            backgroundColor: `hsla(${i * 70}, 80%, 60%, 0.25)`
        }));

        const lineCtx = document.getElementById("activityChart");
        fadeIn(lineCtx);
        new Chart(lineCtx, {
            type: "line",
            data: { labels, datasets },
            options: {
                animation: { duration: 1300, easing: "easeOutCubic" },
                plugins: { legend: { position: "bottom", labels: { color: "#fff" } } },
                scales: {
                    y: { beginAtZero: true, ticks: { color: "#fff" } },
                    x: { ticks: { color: "#fff" } }
                }
            }
        });
    } catch (err) {
        console.error("Gagal memuat dashboard:", err);
        Swal.fire("Gagal Memuat", err.message, "error");
    }
});