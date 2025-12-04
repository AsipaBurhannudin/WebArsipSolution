document.addEventListener("DOMContentLoaded", () => {
    const dashboardType = document.body.dataset.dashboardType || "admin";
    const ctxAudit = document.getElementById("auditLogChart");
    const ctxPie = document.getElementById("pieChart");

    const refreshInterval = 60000; // 1 menit
    let auditChartInstance = null;
    let pieChartInstance = null;

    const colors = ["#007bff", "#28a745", "#ffc107", "#17a2b8", "#dc3545"];

    // === 🧩 UTILITIES ===
    const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    const showToast = (icon, title) => {
        Swal.fire({
            toast: true,
            icon,
            title,
            position: "top-end",
            showConfirmButton: false,
            timer: 2000,
            timerProgressBar: true
        });
    };

    const animateCount = (el, target) => {
        const duration = 800;
        const start = parseInt(el.textContent.replace(/\D/g, "")) || 0;
        const range = target - start;
        const startTime = performance.now();

        const update = (currentTime) => {
            const progress = Math.min((currentTime - startTime) / duration, 1);
            el.textContent = Math.floor(start + range * progress);
            if (progress < 1) requestAnimationFrame(update);
        };

        requestAnimationFrame(update);
    };

    const fetchDashboardData = async () => {
        try {
            const response = await fetch("/Dashboard/GetDashboardData");
            if (!response.ok) throw new Error("Gagal memuat data dashboard.");
            return await response.json();
        } catch (err) {
            console.error(err);
            showToast("error", "Tidak dapat memuat data dashboard");
            return null;
        }
    };

    const updateCounters = (data) => {
        document.querySelectorAll("[data-count]").forEach((el) => {
            const key = el.dataset.count;
            if (data[key] !== undefined) {
                animateCount(el, data[key]);
            }
        });
    };

    // === 📊 RENDER CHARTS ===
    const renderAuditChart = (stats) => {
        if (!ctxAudit) return;

        const grouped = stats.reduce((acc, item) => {
            const date = new Date(item.date).toLocaleDateString("id-ID");
            if (!acc[date]) acc[date] = 0;
            acc[date] += item.count;
            return acc;
        }, {});

        const labels = Object.keys(grouped);
        const values = Object.values(grouped);

        if (auditChartInstance) auditChartInstance.destroy();

        auditChartInstance = new Chart(ctxAudit, {
            type: "bar",
            data: {
                labels,
                datasets: [{
                    label: "Aktivitas Audit Log",
                    data: values,
                    backgroundColor: "rgba(75,192,192,0.4)",
                    borderColor: "rgba(75,192,192,1)",
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                animation: {
                    duration: 1000,
                    easing: "easeOutQuart"
                },
                scales: {
                    y: { beginAtZero: true }
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: (context) => ` ${context.parsed.y} aktivitas`
                        }
                    }
                }
            }
        });
    };

    const renderPieChart = (data) => {
        if (!ctxPie) return;

        const { documents, users, roles, permissions, logs } = data;
        const labels = ["Documents", "Users", "Roles", "Permissions", "Audit Logs"];
        const values = [documents, users, roles, permissions, logs];

        if (pieChartInstance) pieChartInstance.destroy();

        pieChartInstance = new Chart(ctxPie, {
            type: "doughnut",
            data: {
                labels,
                datasets: [{
                    data: values,
                    backgroundColor: colors,
                    hoverOffset: 10
                }]
            },
            options: {
                animation: {
                    duration: 1000,
                    easing: "easeOutBounce"
                },
                plugins: {
                    legend: { position: "bottom" }
                }
            }
        });
    };

    // === 🔁 REFRESH DASHBOARD ===
    const refreshDashboard = async () => {
        const data = await fetchDashboardData();
        if (!data) return;

        updateCounters(data);
        renderAuditChart(data.stats || []);
        renderPieChart(data);
        showToast("success", "Dashboard diperbarui ✨");
    };

    // === 🚀 INISIALISASI ===
    const init = async () => {
        if (dashboardType === "admin") {
            await refreshDashboard();
            setInterval(refreshDashboard, refreshInterval);
        } else if (dashboardType === "user") {
            Swal.fire({
                title: "Selamat datang!",
                text: "Anda sedang melihat dashboard pengguna.",
                timer: 2000,
                showConfirmButton: false,
                icon: "info"
            });
        }

        // Cegah scroll tembus sidebar
        document.body.style.overflowX = "hidden";
    };

    init();
});

document.addEventListener("DOMContentLoaded", () => {
    const stats = window.dashboardStats || [];
    if (stats.length === 0) return;

    const grouped = stats.reduce((acc, item) => {
        const date = new Date(item.date).toLocaleDateString('id-ID');
        acc[date] = (acc[date] || 0) + item.count;
        return acc;
    }, {});

    const ctx = document.getElementById('auditLogChart');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: Object.keys(grouped),
            datasets: [{
                label: 'Aktivitas Audit',
                data: Object.values(grouped),
                backgroundColor: [
                    'rgba(115,103,240,0.6)',
                    'rgba(52,172,224,0.6)',
                    'rgba(173,110,255,0.6)',
                    'rgba(120,220,255,0.6)',
                    'rgba(180,130,255,0.6)',
                    'rgba(110,200,255,0.6)',
                    'rgba(160,120,255,0.6)'
                ],
                borderColor: 'rgba(255,255,255,0.2)',
                borderWidth: 1
            }]
        },
        options: {
            scales: {
                y: { beginAtZero: true }
            },
            plugins: {
                legend: { labels: { color: "#fff" } }
            }
        }
    });
});
