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

    const observer = new ResizeObserver(entries => {
        if (entries.length > 0) {
            const width = entries[0].contentRect.width;
            // Use requestAnimationFrame to throttle slightly and prevent loop errors
            requestAnimationFrame(() => {
                dotNetRef.invokeMethodAsync('OnContainerResize', width);
            });
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