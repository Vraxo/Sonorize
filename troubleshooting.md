# Troubleshooting Guide

This document serves as a reference for debugging critical or recurring issues, ensuring they are not reintroduced in future updates.

---

## 1. Drag & Drop Failures

**Problem:** Drag-and-drop reordering for tracks within a manual playlist was non-functional.
**Observed Behavior:** When a user attempted to drag a track, the cursor would immediately change to the "forbidden" icon. No drag preview was generated, and dropping the item had no effect.

### Symptoms

- **UI:** Cursor displayed "forbidden" icon immediately.
- **Logs:** Complete absence of console logs from drag handlers (`dragstart`, etc.) and no backend RPC calls.

### Root Cause

1. **Race Condition:** `player-interaction.js` was loading *after* the `photino-bridge-ready` event had already fired, so its initialization listener never triggered.
2. **Fragile `dragover`:** The logic only called `preventDefault()` when hovering specific elements, causing "dead zones" between rows where dropping was forbidden.

### Solution

1. **Robust Initialization:** Implemented a "check-then-listen" pattern. If `window.photino.isBridgeReady` is true, initialize immediately; otherwise, wait for the event.
2. **Broad Event Handling:** Ensure `dragover` calls `preventDefault()` on the container level, not just specific children.

---

## 2. Visual Artifacts: Scroll Glitching & Color Fringing

**Problem:**
When scrolling rapidly through the track list, "glitchy" purple, green, or red faint colors appear behind text or borders. This is often described as color fringing, ghosting, or "rainbow" artifacts on text edges.

### Symptoms

- **UI:** Faint colored outlines appear around text during movement.
- **Performance:** Scrolling feels slightly heavy or jittery.
- **Video:** These artifacts may not appear in screen recordings (OBS) because they are often artifacts of the monitor's response to subpixel rendering, or specific to the browser's compositor layer which might be bypassed in capture.

### Root Cause

This is a browser rendering engine artifact caused by the interaction between **Subpixel Antialiasing (ClearType)** and **Dark Backgrounds**.

1. **Subpixel Rendering:** Browsers use individual R/G/B subpixels to smooth text edges. On dark backgrounds with high-contrast white text, this often results in visible color fringing.
2. **Alpha Blending Cost:** Using `rgba()` values for borders or backgrounds (e.g., `rgba(255, 255, 255, 0.1)`) forces the browser's compositor to recalculate alpha blending for every row on every frame of the scroll.
3. **CSS Transitions:** Having `transition: all` or background transitions on table rows causes the browser to constantly re-evaluate styles during scrolling if the mouse pointer "hovers" over moving rows, leading to layout thrashing.

### Solution & Prevention

To prevent this on dark themes, the following CSS rules **must** be applied to dense lists (like Track Tables):

1. **Enforce Grayscale Antialiasing:**
   Disable subpixel rendering for the text container.
   
   ```css
   -webkit-font-smoothing: antialiased;
   -moz-osx-font-smoothing: grayscale;
   ```

2. **Use Solid Colors (No RGBA):**
   Replace semi-transparent borders/backgrounds with their opaque Hex equivalents.
   
   * Bad: `border-bottom: 1px solid rgba(255, 255, 255, 0.1);`
   * Good: `border-bottom: 1px solid #282828;`

3. **Promote to GPU Layer:**
   Force rows to be treated as separate layers to prevent repaint artifacts.
   
   ```css
   transform: translateZ(0);
   backface-visibility: hidden;
   ```

4. **Disable Transitions on Rows:**
   Do not use CSS transitions on elements that move rapidly (like table rows during scroll).

---

## 3. Resize Freeze & Layout Thrashing (Album Grid)

**Problem:**
When resizing the application window while in Album Grid view, the UI would freeze for ~500ms specifically when the number of columns changed (e.g., expanding from 4 to 5 columns).

### Symptoms

- **UI:** The window resize handle becomes unresponsive or "jumps".
- **FPS:** Noticeable drop in frame rate during layout changes.
- **GC:** High memory allocation spikes visible in diagnostic tools.

### Root Cause

1. **Event Flooding:** The JS `ResizeObserver` fires for every pixel of change. Sending all these events to C# overwhelms the Blazor Interop bridge.
2. **LINQ Allocations:** Using `GroupBy().Select().ToList()` to reshape the grid inside the resize handler allocates thousands of new objects and closures instantly, triggering Garbage Collection.
3. **DOM Thrashing:** Re-creating the outer `List<List<T>>` for rows changes the object references. Blazor's diffing engine interprets this as "delete all old rows, create all new rows" rather than updating existing ones, causing massive DOM layout recalculations.

### Solution

1. **Throttle JS Events:** Limit `ResizeObserver` interop calls to ~50fps (every 20ms) using a timer.

2. **Object Pooling:** Use a `List<List<T>>` pool for the rows. Reuse existing list instances instead of creating new ones so Blazor detects reference equality and only updates content, not the container.

3. **Loops over LINQ:** Replace heavy LINQ projections with explicit `for` loops during hot-path layout calculations.

---

## 4. UI Stale State on Auto-Advance (Audio Thread Lock)

**Problem:**
When the player automatically advanced to the next track (e.g., playlist continue or repeat), the UI (Song Title, Album Art, Focus Mode) would remain displaying the previous song. The UI would only update after a user interaction (mouse click, window resize).

### Symptoms

- **UI:** Text and images are stale; audio is correct and playing the next song.
- **Interaction:** Clicking anywhere or resizing the window forces a sudden update to the correct state.
- **Logs:** Events appeared to fire in logs, but the DOM did not reflect changes.

### Root Cause

1. **Unmanaged Thread Context:** The BASS audio engine fires the `Sync` callback on an unmanaged background thread. Calling Blazor's `InvokeAsync` directly from this context can sometimes fail to properly acquire the synchronization context or trigger a render, resulting in a "pending" render state that only flushes on the next UI event.
2. **Missing Event Signal:** The logic for auto-advancing updated the `CurrentQueueIndex` but failed to fire the specific `QueueChanged` event that some components were listening to.

### Solution

1. **Thread Decoupling:** Wrap the callback logic in `Task.Run` to immediately transition execution to the managed ThreadPool.
   
   ```csharp
   // In PlayerService.cs
   _ = Task.Run(async () => { /* Logic */ });
   ```

2. **Explicit Event Firing:** Use a centralized method (e.g., `SetQueueIndex`) to ensure `QueueChanged` is invoked whenever the internal index changes.

3. **Async UI Marshaling:** Update UI event handlers to use `async void` with `await InvokeAsync(...)` to ensure the render request is awaited and processed by the Dispatcher.
   
   ```csharp
   private async void OnStateChanged()
   {
       await InvokeAsync(() =>
       {
           UpdateState();
           StateHasChanged();
       });
   }
   ```

---

## 5. Sticky Hover & Scroll Hit-Testing Lag

**Problem:**
When scrolling through virtualized lists (Track List or Album Grid), the hover highlight would "stick" to the row under the mouse even as it moved away, or take ~500ms to appear after scrolling stopped.

### Symptoms

- **Sticky Hover:** Highlighted row moves with the scroll instead of the highlight staying under the cursor.
- **Laggy Hover:** After stopping a fast scroll, the item under the cursor doesn't highlight immediately.
- **UX:** Feels unresponsive or "heavy".

### Root Cause

1. **Browser Optimization:** Chromium browsers (WebView2) disable expensive hit-testing during scroll to maintain 60FPS.
2. **Pointer Events:** The standard fix for #1 is disabling pointer events during scroll, but this breaks the ability to detect what is under the cursor when scroll *stops*, leading to the delay.

### Solution

A hybrid approach using **CSS Pointer Events** and **JavaScript Manual Hit-Testing**:

1. **Scroll Start:** Add a `disable-hover` class to the body. CSS sets `pointer-events: none` on rows. This instantly kills "sticky" highlights.
2. **Scroll End:**
   - Remove `disable-hover`.
   - Immediately run `document.elementFromPoint(x, y)` in JavaScript.
   - Manually apply a `.hover-forced` class to the found element.
3. **Mouse Move:** Remove `.hover-forced` and let native CSS `:hover` resume control.

This bypasses the browser's optimization delay by doing the math ourselves.

---

## 6. Unstyled Form Elements (Default White Background / Browser Defaults)

**Problem:**
Certain HTML elements like `<textarea>` or custom-styled buttons appear with default browser styling (white background, black text, etc.) instead of the application's themed colors.

### Symptoms

- **UI:** The element looks out of place, often white or light gray while the rest of the app uses dark theme colors.
- **Persistence:** Even with custom CSS classes and `!important`, the browser's default styling (`user-agent stylesheet`) overrides them.

### Root Cause

1. **User-Agent Styles:** Browsers apply built-in styles to form elements. For example, `<textarea>` defaults to `background-color: field` and `color: fieldtext`. These selectors are very broad (`textarea { ... }`) and can have higher effective specificity than a simple class selector, especially when scoped CSS is involved.
2. **Missing Global Reset:** The application lacked a global CSS reset that explicitly overrides browser defaults for all instances of problematic elements.

### Solution

Add a **global reset** for the problematic element in the main stylesheet (e.g., `base.css`). This ensures the theme variables are applied universally with high specificity.

**Example for textareas:**

```css
/* Global Textarea Reset */
textarea {
    background-color: var(--bg-secondary) !important;
    color: var(--text-primary) !important;
    border-color: var(--border-color) !important;
}
```

**For action buttons that are not icons, create a dedicated utility class:**

```css
.header-action-text-btn {
    background: transparent;
    border: none;
    color: var(--text-secondary);
    /* ... appropriate padding, font-size, etc. ... */
    white-space: nowrap;
}
```

### Prevention

- Always consider browser defaults when styling form controls or interactive elements.
- Prefer global resets for elements that appear in multiple places with consistent theming requirements.
- When a component's scoped styles aren't enough, check the browser's **Computed** styles tab to identify the overriding rule and craft a more specific selector or add a global rule.