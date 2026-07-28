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
    <img src={URL.createObjectURL(image)} onclick={clicked} alt="Image" class="img-thumbnail clickable-image" />
</div>
{#if enlarged} 
<FullscreenImage {image} bind:enlarged />
{/if}

<style>
 .img-thumbnail {
    width: 100%;
    cursor: pointer;
    aspect-ratio: 1; 
    object-fit: cover;
 }
</style>