<script lang="ts">
    import ClickableImage from "./clickable-image.svelte";
    type ImageViewContext = "search" | "album" | "post" | "none";
    type ImageSource = File | string;

    let {
        src,
        index,
        images = null,
        photoId,
        photoIds,
        rootMargin = "200px",
        viewContext,
    }: {
        src: string;
        index: number;
        images: ImageSource[] | null;
        photoId: number;
        photoIds: number[];
        rootMargin?: string;
        viewContext: ImageViewContext;
    } = $props();

    let containerEl: HTMLDivElement | undefined = $state();
    let file = $state<File | null>(null);
    let loading = $state(false);
    let failed = $state(false);

    function filenameFromSrc(url: string) {
        try {
            const path = new URL(url, window.location.origin).pathname;
            return path.split("/").pop() || "image";
        } catch {
            return "image";
        }
    }

    async function loadFile() {
        if (file || loading) return;
        loading = true;
        failed = false;
        try {
            const res = await fetch(src);
            if (!res.ok)
                throw new Error(`Failed to fetch image: ${res.status}`);
            const blob = await res.blob();
            file = new File([blob], filenameFromSrc(src), { type: blob.type });
        } catch (e) {
            console.error(e);
            failed = true;
        } finally {
            loading = false;
        }
    }

    $effect(() => {
        if (!containerEl) return;

        const observer = new IntersectionObserver(
            (entries) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        loadFile();
                        observer.disconnect();
                    }
                }
            },
            { rootMargin },
        );

        observer.observe(containerEl);

        return () => observer.disconnect();
    });
</script>

<div bind:this={containerEl} class="lazy-image-wrapper">
    {#if file}
        <ClickableImage
            image={file}
            {index}
            images={images ?? []}
            {photoId}
            {photoIds}
            {viewContext}
        />
    {:else if failed}
        <div
            class="img-thumbnail lazy-placeholder lazy-error"
            onclick={loadFile}
            role="button"
            tabindex="0"
        >
            Failed to load — click to retry
        </div>
    {:else}
        <div class="img-thumbnail lazy-placeholder" class:loading></div>
    {/if}
</div>

<style>
    .lazy-image-wrapper {
        width: 100%;
    }
    .lazy-placeholder {
        width: 100%;
        aspect-ratio: 1;
        background: var(--bs-secondary-bg, #e9ecef);
    }
    .lazy-placeholder.loading {
        animation: pulse 1.2s ease-in-out infinite;
    }
    .lazy-error {
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 0.8rem;
        cursor: pointer;
        color: var(--bs-secondary-color, #6c757d);
    }
    @keyframes pulse {
        0%,
        100% {
            opacity: 1;
        }
        50% {
            opacity: 0.6;
        }
    }
</style>
