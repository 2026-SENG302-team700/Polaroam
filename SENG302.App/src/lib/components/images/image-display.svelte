<script lang="ts">
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import ClickableImage from "./clickable-image.svelte";

    let {
        images = [],
        ondelete,
    }: { images?: File[] | null; ondelete?: (index: number) => void } = $props();
</script>

<div class="row row-cols-2 row-cols-md-3 row-cols-xl-4 g-2 container">
    {#each images ?? [] as image, index}
        <div class="col">
            <div class="position-relative">
                <ClickableImage {image} />
                <div class="position-absolute top-0 end-0 m-1">
                    <DeleteButton deleteAction={() => ondelete?.(index)} class="btn-sm" />
                </div>
            </div>
        </div>
    {:else}
        <p>No image selected</p>
    {/each}
</div>  