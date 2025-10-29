document.addEventListener("DOMContentLoaded", () => {
    const wrapper = document.getElementById("wrapper");
    const toggleBtn = document.getElementById("menu-toggle");
    const profileIcon = document.getElementById("profile-icon");
    const profileMenu = document.getElementById("profile-menu");

    // === Sidebar Toggle (Morph + Smooth Transition) ===
    toggleBtn.addEventListener("click", () => {
        wrapper.classList.toggle("toggled");
        toggleBtn.classList.toggle("active");

        const sidebar = document.getElementById("sidebar-wrapper");
        sidebar.style.transition = "all 0.4s ease";

        // Optional smooth opacity fade for content
        const content = document.getElementById("page-content-wrapper");
        content.style.opacity = wrapper.classList.contains("toggled") ? "0.95" : "1";
    });

    // === Highlight Active Sidebar Menu ===
    const currentUrl = window.location.pathname.toLowerCase();
    document.querySelectorAll("#sidebar-wrapper a").forEach(link => {
        const href = link.getAttribute("href")?.toLowerCase();
        if (href && currentUrl.includes(href)) {
            link.classList.add("active");
        }
    });

    // === Profile Dropdown Toggle ===
    if (profileIcon && profileMenu) {
        profileIcon.addEventListener("click", (e) => {
            e.stopPropagation();
            profileMenu.classList.toggle("show");
        });

        document.addEventListener("click", (e) => {
            if (!profileMenu.contains(e.target) && e.target !== profileIcon) {
                profileMenu.classList.remove("show");
            }
        });
    }

    // === Responsive Reset (auto close sidebar on resize) ===
    window.addEventListener("resize", () => {
        if (window.innerWidth > 992 && wrapper.classList.contains("toggled")) {
            wrapper.classList.remove("toggled");
            toggleBtn.classList.remove("active");
        }
    });
});