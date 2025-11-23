document.addEventListener("DOMContentLoaded", () => {
    const wrapper = document.getElementById("wrapper");
    const toggleBtn = document.getElementById("menu-toggle");
    const sidebar = document.getElementById("sidebar-wrapper");
    const profileIcon = document.getElementById("profile-icon");
    const profileMenu = document.getElementById("profile-menu");

    /* ==========================================================
       MENU TOGGLE
    ========================================================== */
    const toggleSidebar = () => {
        wrapper.classList.toggle("toggled");
        toggleBtn.classList.toggle("active");
    };

    toggleBtn.addEventListener("click", (e) => {
        e.stopPropagation();
        toggleSidebar();
    });

    /* ==========================================================
       AUTO CLOSE WHEN CLICK OUTSIDE (MOBILE)
    ========================================================== */
    document.addEventListener("click", (e) => {
        if (
            window.innerWidth <= 992 &&
            wrapper.classList.contains("toggled") &&
            !sidebar.contains(e.target) &&
            e.target !== toggleBtn
        ) {
            wrapper.classList.remove("toggled");
            toggleBtn.classList.remove("active");
        }
    });

    /* ==========================================================
       AUTO CLOSE AFTER CLICK MENU ITEM (MOBILE)
    ========================================================== */
    document.querySelectorAll("#sidebar-wrapper a").forEach(link => {
        link.addEventListener("click", () => {
            if (window.innerWidth <= 992) {
                wrapper.classList.remove("toggled");
                toggleBtn.classList.remove("active");
            }
        });
    });

    /* ==========================================================
       PROFILE MENU
    ========================================================== */
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

    /* ==========================================================
       ACTIVE SIDEBAR HIGHLIGHT
    ========================================================== */
    const currentUrl = window.location.pathname.toLowerCase();
    document.querySelectorAll("#sidebar-wrapper a").forEach(link => {
        const href = link.getAttribute("href")?.toLowerCase();
        if (href && currentUrl.includes(href)) {
            link.classList.add("active");
        }
    });

    /* ==========================================================
       RESPONSIVE RESET (WITH DEBOUNCE)
    ========================================================== */
    let resizeTimer;
    window.addEventListener("resize", () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {

            // If expanded but user returns to desktop size
            if (window.innerWidth > 992) {
                wrapper.classList.remove("toggled");
                toggleBtn.classList.remove("active");
            }

        }, 150);
    });

    /* ==========================================================
       (OPTIONAL) SWIPE TO CLOSE - MOBILE UX BOOST
    ========================================================== */
    let touchXStart = 0;

    document.addEventListener("touchstart", (e) => {
        touchXStart = e.touches[0].clientX;
    });

    document.addEventListener("touchmove", (e) => {
        const diff = e.touches[0].clientX - touchXStart;

        // Swipe → left = close sidebar
        if (diff < -50 && window.innerWidth <= 992) {
            wrapper.classList.remove("toggled");
            toggleBtn.classList.remove("active");
        }
    });

});