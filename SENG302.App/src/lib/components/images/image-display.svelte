<script lang="ts">
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import ClickableImage from "./clickable-image.svelte";

    let {
        images = [],
        letDelete = false,
        ondelete,
    }: { images?: File[] | null; 
         letDelete?: boolean
         ondelete?: (index: number) => void } = $props();
    
   
</script>

<div class="row row-cols-1 row-cols-md-2 row-cols-xl-3 g-2 container image-display-wrapper">
    {#each images ?? [] as image, index}
        <div class="col">
            <div class="position-relative">
                <ClickableImage {image} {index} {images} />
                {#if letDelete} 
                    <div class="position-absolute top-0 end-0 m-1">
                        <DeleteButton deleteAction={() => ondelete?.(index)} class="btn-sm" />
                    </div>
                {/if}
            </div>
        </div>
    {:else}
        <p>No image selected</p>
    {/each}
</div>  