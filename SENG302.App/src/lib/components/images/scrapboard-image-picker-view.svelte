<script lang="ts">
    let {
        src,
        selectedPhotoId = $bindable(null),
        imagePhotoId = null,
        image = $bindable(null)
    } : {
        src: string,
        selectedPhotoId: number | null,
        imagePhotoId: number | null,
        image: File | null
    } = $props()

    let objectUrl = $state<string | null>(null);
    let imageFile = $state<File | null>(null);
    let displaySrc = $derived(imageFile instanceof File ? objectUrl : imageFile);
    let containerEl: HTMLDivElement | undefined = $state();
    
    let loading = $state(false);
    let failed = $state(false);
    
    let imageSelected = $state(false);

    let rootMargin = "200px"
    
    

    $effect(() => {
        if (selectedPhotoId !== imagePhotoId) {
            imageSelected = false;
        }
        
        if (imageFile instanceof File) {
            const url = URL.createObjectURL(imageFile);
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
            imageFile = new File([blob], filenameFromSrc(src), { type: blob.type });
        } catch (e) {
            console.error(e);
            failed = true;
        } finally {
            loading = false;
        }
    }
    
    async function clicked() {
        imageSelected = !imageSelected;
        selectedPhotoId = imagePhotoId

        if (imageSelected) {
            image = imageFile;
        }
    }
    
</script>

<div bind:this={containerEl} class="lazy-image-wrapper">
    {#if imageFile}
        <img
                src={displaySrc}
                onclick={clicked}
                alt="Image"
                class="img-thumbnail clickable-image {imageSelected ? 'rectangular-glow' : ''}"
                tabindex="0"
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

    .rectangular-glow {
        box-shadow: 0px 0px 5px 5px rgba(0, 150, 255, 0.7);
        border-radius: 8px;
    }

    img:focus {
        transform: none;
        box-shadow: 0px 0px 5px 5px rgba(0, 150, 255, 0.7);
        border-radius: 8px;
    }
    
</style>