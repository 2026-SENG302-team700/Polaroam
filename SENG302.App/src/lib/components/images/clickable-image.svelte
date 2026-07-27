<script lang="ts">
    import FullscreenImage from "./fullscreen-image.svelte";

    let {
        image,
    }: { image: File; } = $props();
    let enlarged = $state(false);

    function clicked() {
        enlarged = !enlarged;
    }

</script>


<div class="clickable-image-wrapper">
    <img src={URL.createObjectURL(image)} onclick={clicked} alt="Image" class="img-thumbnail w-100" style="aspect-ratio: 1; object-fit: cover;" />
</div>
{#if enlarged} 
<FullscreenImage {image} />
{/if}

<style>

</style>