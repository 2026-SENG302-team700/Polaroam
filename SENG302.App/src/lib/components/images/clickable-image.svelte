<script lang="ts">
    import FullscreenImage from "./fullscreen-image.svelte";
    type ImageViewContext = "search" | "album" | "post" | "none";
    type ImageSource = File | string;

    let {
        image,
        index,
        images,
        photoId,
        photoIds,
        viewContext,
    }: {
        image: ImageSource;
        index: number;
        images: ImageSource[] | null;
        photoId?: number;
        photoIds?: number[];
        viewContext: ImageViewContext;
    } = $props();
    let enlarged = $state(false);
    let objectUrl = $state<string | null>(null);

    $effect(() => {
        if (image instanceof File) {
            const url = URL.createObjectURL(image);
            objectUrl = url;
            return () => URL.revokeObjectURL(url);
        }

        objectUrl = null;
    });

    let displaySrc = $derived(image instanceof File ? objectUrl : image);

    function clicked() {
        enlarged = !enlarged;
    }
</script>

<div class="clickable-image-wrapper">
    <img
        src={displaySrc}
        onclick={clicked}
        alt="Image"
        class="img-thumbnail clickable-image"
    />
</div>
{#if enlarged}
    <FullscreenImage
        {image}
        {index}
        {images}
        {photoId}
        {photoIds}
        {viewContext}
        bind:enlarged
    />
{/if}

<style>
    .img-thumbnail {
        width: 100%;
        cursor: pointer;
        aspect-ratio: 1;
        object-fit: cover;
    }
</style>
