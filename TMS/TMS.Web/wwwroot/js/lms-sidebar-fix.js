(function () {
    "use strict";

    function init() {
        const sidebar = document.querySelector(".sidebar-wrapper");
        const header = document.querySelector(".page-header");

        if (!sidebar || !header) return;

        function fixHeader() {
            const width = sidebar.getBoundingClientRect().width;

            document.documentElement.style.setProperty(
                "--lms-sidebar-width", width + "px"
            );

            header.style.setProperty("position", "fixed", "important");
            header.style.setProperty("left", width + "px", "important");
            header.style.setProperty("right", "0px", "important");
            header.style.setProperty(
                "width", "calc(100vw - " + width + "px)", "important"
            );
            header.style.setProperty("max-width", "none", "important");
            header.style.setProperty("min-width", "0", "important");
            header.style.setProperty("margin-left", "0", "important");
            header.style.setProperty("transform", "none", "important");
            header.style.setProperty("box-sizing", "border-box", "important");
        }

        fixHeader();

        const observer = new MutationObserver(fixHeader);
        observer.observe(sidebar, {
            attributes: true,
            attributeFilter: ["class", "style"]
        });

        const resizeObserver = new ResizeObserver(fixHeader);
        resizeObserver.observe(sidebar);

        window.addEventListener("resize", fixHeader);

        const tooltip = document.createElement("div");
        tooltip.className = "lms-instant-tooltip";
        tooltip.setAttribute("role", "tooltip");
        document.body.appendChild(tooltip);

        let activeItem = null;

        function hideTooltip() {
            activeItem = null;
            tooltip.classList.remove("is-visible");
        }

        function isCollapsed() {
            return sidebar.getBoundingClientRect().width <= 100;
        }

        function findMenuItem(target) {
            if (!(target instanceof Element)) return null;

            return target.closest(
                ".sidebar-wrapper a, " +
                ".sidebar-wrapper button, " +
                ".sidebar-wrapper .sidebar-link"
            );
        }

        function getLabel(item) {
            const originalTitle = item.getAttribute("title");

            if (originalTitle && originalTitle.trim()) {
                item.dataset.lmsTooltip = originalTitle.trim();
                item.removeAttribute("title");
            }

            const explicit =
                item.dataset.lmsTooltip ||
                item.getAttribute("aria-label");

            if (explicit && explicit.trim()) return explicit.trim();

            const clone = item.cloneNode(true);
            clone.querySelectorAll("i, svg, img, .icon, .badge")
                .forEach(el => el.remove());

            return (clone.textContent || "")
                .replace(/\s+/g, " ")
                .trim();
        }

        function showTooltip(item) {
            if (!isCollapsed()) {
                hideTooltip();
                return;
            }

            const label = getLabel(item);
            if (!label) {
                hideTooltip();
                return;
            }

            activeItem = item;
            tooltip.textContent = label;

            const rect = item.getBoundingClientRect();

            tooltip.style.left = Math.min(
                rect.right + 12,
                window.innerWidth - 220
            ) + "px";

            tooltip.style.top = Math.max(
                8,
                Math.min(
                    rect.top + rect.height / 2 - 17,
                    window.innerHeight - 45
                )
            ) + "px";

            tooltip.classList.add("is-visible");
        }

        sidebar.addEventListener("mouseover", function (event) {
            const item = findMenuItem(event.target);
            if (!item || item === activeItem) return;
            showTooltip(item);
        });

        sidebar.addEventListener("mouseout", function (event) {
            const item = findMenuItem(event.target);
            if (!item || item !== activeItem) return;

            const next = event.relatedTarget;
            if (next instanceof Node && item.contains(next)) return;

            hideTooltip();
        });

        sidebar.addEventListener("focusin", function (event) {
            const item = findMenuItem(event.target);
            if (item) showTooltip(item);
        });

        sidebar.addEventListener("focusout", hideTooltip);
        window.addEventListener("scroll", hideTooltip, true);

        // Remove native delayed title tooltips on sidebar items.
        sidebar.querySelectorAll("[title]").forEach(function (item) {
            item.dataset.lmsTooltip = item.getAttribute("title") || "";
            item.removeAttribute("title");
        });

        const sidebarObserver = new MutationObserver(function () {
            if (!isCollapsed()) hideTooltip();
        });

        sidebarObserver.observe(sidebar, {
            attributes: true,
            attributeFilter: ["class"]
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
