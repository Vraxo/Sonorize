window.updateThemeClasses = (zebra, customScroll, gridLines) => {
    // Zebra
    if (zebra) {
        document.body.classList.add('zebra-striping-enabled');
    } else {
        document.body.classList.remove('zebra-striping-enabled');
    }

    // Scrollbars
    if (customScroll) {
        document.body.classList.add('custom-scrollbars');
    } else {
        document.body.classList.remove('custom-scrollbars');
    }

    // Grid Lines
    if (gridLines) {
        document.body.classList.add('show-grid-lines');
    } else {
        document.body.classList.remove('show-grid-lines');
    }
};

window.applyCustomCss = (css) => {
    try {
        var id = 'user-custom-css';
        var el = document.getElementById(id);
        if (!el) {
            el = document.createElement('style');
            el.id = id;
            el.type = 'text/css';
            document.head.appendChild(el);
        }
        el.textContent = css || '';
    } catch (e) { }
};

window.registerZoomHandler = (dotNetRef) => {
    window.addEventListener('keydown', (e) => {
        if (e.ctrlKey) {
            if (e.key === '=' || e.key === '+') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('AdjustZoom', 1);
            } else if (e.key === '-') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('AdjustZoom', -1);
            } else if (e.key === '0') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('AdjustZoom', 0);
            }
        }
    });
};

// --- Resize Observer for LibraryGrid ---
window.observeResize = (element, dotNetRef) => {
    if (!element) return;

    // Disconnect existing if any
    if (element._resizeObserver) {
        element._resizeObserver.disconnect();
    }

    // Performance Fix: Debounce the observer to prevent Layout Thrashing during animations.
    let debounceTimer;

    const observer = new ResizeObserver(entries => {
        if (entries.length > 0) {
            const width = entries[0].contentRect.width;

            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                dotNetRef.invokeMethodAsync('OnContainerResize', width);
            }, 200);
        }
    });

    observer.observe(element);
    element._resizeObserver = observer;
};

window.unobserveResize = (element) => {
    if (element && element._resizeObserver) {
        element._resizeObserver.disconnect();
        delete element._resizeObserver;
    }
};

// --- Layout Locking (The "Zero Difference" Guarantee) ---
// Prevents heavy DOM reflows during sidebar animation by freezing content width.
window.startLayoutLock = (shouldLock) => {
    const main = document.querySelector('.main-content');
    if (!main) return;

    // We target the immediate child (the page component wrapper)
    // Note: In Blazor, this is usually the first div inside main-content
    const content = main.firstElementChild;
    if (!content) return;

    if (shouldLock) {
        // Capture current geometry
        const currentWidth = main.offsetWidth;

        // Force fixed dimensions to stop the browser from recalculating layout
        // as the parent container shrinks/grows.
        // This turns a complex O(N) reflow into a simple O(1) crop/paint operation.
        content.style.boxSizing = "border-box";
        content.style.width = currentWidth + "px";
        content.style.minWidth = currentWidth + "px";
        content.style.maxWidth = currentWidth + "px";
        content.style.overflow = "hidden"; // Ensure clean clipping
    } else {
        // Release the lock
        content.style.boxSizing = "";
        content.style.width = "";
        content.style.minWidth = "";
        content.style.maxWidth = "";
        content.style.overflow = "";
    }
};