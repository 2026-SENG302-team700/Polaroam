<script lang="ts">
    
    import ClickableImage from "$lib/components/images/clickable-image.svelte";

    let {
        src
    } : {
        src: string
    } = $props()

    let image = $state<File | null>(null);
    
    let objectUrl = $state<string | null>(null);
    let displaySrc = $derived(image instanceof File ? objectUrl : image);

    let containerEl: HTMLDivElement | undefined = $state();
    
    let loading = $state(false);
    let failed = $state(false);

    let rootMargin = "200px"
    
    

    $effect(() => {
        if (image instanceof File) {
            const url = URL.createObjectURL(image);
            objectUrl = url;
            return () => URL.revokeObjectURL(url);
        }

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

        objectUrl = null;
    });

    function filenameFromSrc(url: string) {
        try {
            const path = new URL(url, window.location.origin).pathname;
            return path.split("/").pop() || "image";
        } catch {
            return "image";
        }
    }

    async function loadFile() {
        if (image || loading) return;
        loading = true;
        failed = false;
        try {
            const res = await fetch(src);
            if (!res.ok)
                throw new Error(`Failed to fetch image: ${res.status}`);
            const blob = await res.blob();
            image = new File([blob], filenameFromSrc(src), { type: blob.type });
        } catch (e) {
            console.error(e);
            failed = true;
        } finally {
            loading = false;
        }
    }
    
    async function clicked() {
        console.log("clicked")
    }
    
</script>

<div bind:this={containerEl} class="lazy-image-wrapper">
    {#if image}
        <img
                src={displaySrc}
                onclick={clicked}
                alt="Image"
                class="img-thumbnail clickable-image"
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
    .img-thumbnail {
        width: 100%;
        cursor: pointer;
        aspect-ratio: 1;
        object-fit: cover;
    }
</style>