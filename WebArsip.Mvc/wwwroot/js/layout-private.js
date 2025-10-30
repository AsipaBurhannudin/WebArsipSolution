document.addEventListener("DOMContentLoaded", () => {
    const wrapper = document.getElementById("wrapper");
    const toggleBtn = document.getElementById("menu-toggle");
    const sidebar = document.getElementById("sidebar-wrapper");
    const content = document.getElementById("page-content-wrapper");
    const profileIcon = document.getElementById("profile-icon");
    const profileMenu = document.getElementById("profile-menu");

    // Sidebar toggle + icon morph
    toggleBtn.addEventListener("click", () => {
        wrapper.classList.toggle("toggled");
        toggleBtn.classList.toggle("active");
        sidebar.style.transition = "all 0.4s ease";
        content.classList.add("content-anim");
        setTimeout(() => content.classList.remove("content-anim"), 400);
    });

    // Profile dropdown
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

    // Active link highlight
    const currentUrl = window.location.pathname.toLowerCase();
    document.querySelectorAll("#sidebar-wrapper a").forEach(link => {
        const href = link.getAttribute("href")?.toLowerCase();
        if (href && currentUrl.includes(href)) {
            link.classList.add("active");
        }
    });

    // Responsive reset
    window.addEventListener("resize", () => {
        if (window.innerWidth > 992 && wrapper.classList.contains("toggled")) {
            wrapper.classList.remove("toggled");
            toggleBtn.classList.remove("active");
        }
    });
});