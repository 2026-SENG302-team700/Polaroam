<script lang="ts">
    import CloseButton from "../buttons/close-button.svelte";
    let {
        shown,
        scrapboardImage
    } : {
        shown : boolean
        scrapboardImage : string
    } = $props();
    
    let scale = $state(1);
    let panX = $state(0);
    let panY = $state(0);
    let isDragging = $state(false);
    let startX = $state(0);
    let startY = $state(0);

    const minScale = 1;
    const maxScale = 5;

    // Track active pointers for touch (supports multi-touch pinch)
    let activePointers = new Map<number, { x: number; y: number }>();
    let pinchStartDist = 0;
    let pinchStartScale = 1;
    let pinchStartMidX = 0;
    let pinchStartMidY = 0;

    function getDistance(p1: { x: number; y: number }, p2: { x: number; y: number }) {
        return Math.hypot(p2.x - p1.x, p2.y - p1.y);
    }

    function getMidpoint(p1: { x: number; y: number }, p2: { x: number; y: number }) {
        return { x: (p1.x + p2.x) / 2, y: (p1.y + p2.y) / 2 };
    }

    function handleMouseDown(e: MouseEvent) {
        isDragging = true;
        startX = e.clientX - panX;
        startY = e.clientY - panY;
    }

    function handleMouseMove(e: MouseEvent) {
        if (!isDragging) return;
        panX = e.clientX - startX;
        panY = e.clientY - startY;
    }

    function handleMouseUp() {
        isDragging = false;
    }

    function handleWheel(e: WheelEvent) {
        e.preventDefault();
        const zoomFactor = 0.1;
        if (e.deltaY < 0) {
            scale = Math.min(scale + zoomFactor, maxScale);
        } else {
            scale = Math.max(scale - zoomFactor, minScale);
            if (scale === minScale) {
                panX = 0;
                panY = 0;
            }
        }
    }

    // --- Touch / Pointer support ---
    function handlePointerDown(e: PointerEvent) {
        (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
        activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

        if (activePointers.size === 1) {
            isDragging = true;
            startX = e.clientX - panX;
            startY = e.clientY - panY;
        } else if (activePointers.size === 2) {
            isDragging = false;
            const pts = Array.from(activePointers.values());
            pinchStartDist = getDistance(pts[0], pts[1]);
            pinchStartScale = scale;
            const mid = getMidpoint(pts[0], pts[1]);
            pinchStartMidX = mid.x;
            pinchStartMidY = mid.y;
        }
    }

    function handlePointerMove(e: PointerEvent) {
        if (!activePointers.has(e.pointerId)) return;
        activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

        if (activePointers.size === 2) {
            e.preventDefault();
            const pts = Array.from(activePointers.values());
            const newDist = getDistance(pts[0], pts[1]);
            if (pinchStartDist > 0) {
                const newScale = Math.min(
                    maxScale,
                    Math.max(minScale, pinchStartScale * (newDist / pinchStartDist))
                );
                scale = newScale;
                if (scale === minScale) {
                    panX = 0;
                    panY = 0;
                }
            }
        } else if (activePointers.size === 1 && isDragging) {
            panX = e.clientX - startX;
            panY = e.clientY - startY;
        }
    }

    function handlePointerUp(e: PointerEvent) {
        activePointers.delete(e.pointerId);

        if (activePointers.size === 1) {
            // Transitioning from pinch back to single-finger drag
            const [remaining] = Array.from(activePointers.values());
            isDragging = true;
            startX = remaining.x - panX;
            startY = remaining.y - panY;
        } else if (activePointers.size === 0) {
            isDragging = false;
            pinchStartDist = 0;
        }
    }
</script>

{#if shown}
    <div class="fullscreen-image-wrapper">
        <a class="click-to-return" onclick={()=>shown = false}/>
        <div class="image-container" >
            <div class="image-content">
                <div
                        class="viewer-container"
                        onmousedown={handleMouseDown}
                        onmousemove={handleMouseMove}
                        onmouseup={handleMouseUp}
                        onmouseleave={handleMouseUp}
                        onwheel={handleWheel}
                        onpointerdown={handlePointerDown}
                        onpointermove={handlePointerMove}
                        onpointerup={handlePointerUp}
                        onpointercancel={handlePointerUp}
                >
                    <img
                            src={scrapboardImage}
                            alt="Zoomable and pannable"
                            style="transform: translate({panX}px, {panY}px) scale({scale}); cursor: {isDragging ? 'grabbing' : 'grab'};"
                            draggable="false"
                    />
                    <div class="close-control">
                        <CloseButton bind:close={shown} class="btn-sm" />
                    </div>
                </div>
            </div>
        </div>
    </div>
    
{/if}

<style>
    .viewer-container {
        position: relative;
        width: 100%;
        flex: 1;
        overflow: hidden;
        display: flex;
        align-items: center;
        justify-content: center;
        user-select: none;
        touch-action: none;
    }

    img {
        max-width: 100%;
        max-height: 100%;
        transition: transform 0.05s ease-out;
        transform-origin: center center;
        -webkit-user-drag: none;
    }
    
     .click-to-return {
         width: 100%;
         height: 100%;
         position: absolute;
         z-index: 101;
     }
    .image-details {
        display: flex;
        align-items: center;
        gap: 0.4rem;
    }
    .image-detail {
        display: inline-flex;
        flex-direction: row;
        align-items: center;
        gap: 0.6rem;
        height: calc(1.5em + 0.5rem + 2px);
        box-sizing: border-box;
        border-radius: 0.65rem;
        border-width: 0;
        cursor: default;
        pointer-events: none;
    }
    .image-detail span {
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .fullscreen-image-wrapper {
        z-index: 100;
        background-color: rgba(0, 0, 0, 0.7);
        position: fixed;
        top: 0;
        left: 0;
        height: 100vh;
        width: 100vw;
    }
    .image-container {
        position: relative;
        width: 100%;
        height: 100%;
        display: flex;
        justify-content: center;
        align-items: center;
    }
    .image-content {
        display: flex;
        width: 90vw;
        height: 90vh;
        flex-direction: column;
        align-items: center;
        z-index: 102;
    }
    .image {
        display: block;
        border-radius: 5px;
        width: auto;
        height: auto;
        max-width: 90vw;
        max-height: 80vh;
    }
    .close-control {
        position: absolute;
        top: 0.5rem;
        right: 0.5rem;
        z-index: 3;
        line-height: normal;
    }
</style>