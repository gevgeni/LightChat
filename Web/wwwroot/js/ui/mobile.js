// js/ui/mobile.js

export function toggleMobileSidebar(open) {
    const sidebar = document.getElementById("chatsSidebar");
    const overlay = document.getElementById("mobileSidebarOverlay");
    if (!sidebar || !overlay) return;

    if (open) {
        sidebar.classList.remove("left-[-100%]");
        sidebar.classList.add("left-0");
        overlay.classList.remove("hidden");
    } else {
        sidebar.classList.remove("left-0");
        sidebar.classList.add("left-[-100%]");
        overlay.classList.add("hidden");
    }
}

export function toggleMobileMembers(open) {
    const sidebar = document.getElementById("chatMembersSidebar");
    const overlay = document.getElementById("mobileMembersOverlay");
    if (!sidebar || !overlay) return;

    if (open) {
        sidebar.classList.remove("right-[-100%]");
        sidebar.classList.add("right-0");
        overlay.classList.remove("hidden");
    } else {
        sidebar.classList.remove("right-0");
        sidebar.classList.add("right-[-100%]");
        overlay.classList.add("hidden");
    }
}

/**
 * Инициализация обработчиков overlay-ев (закрытие по клику)
 */
export function initializeMobileHandlers() {
    const sidebarOverlay = document.getElementById("mobileSidebarOverlay");
    const membersOverlay = document.getElementById("mobileMembersOverlay");

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener("click", () => toggleMobileSidebar(false));
    }
    if (membersOverlay) {
        membersOverlay.addEventListener("click", () => toggleMobileMembers(false));
    }
}