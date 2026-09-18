<script lang="ts">
    import ExpandableErrorDisplay from "$lib/components/forms/expandable-error-display.svelte";
    import ScrapboardImagePickerView from "$lib/components/images/scrapboard-image-picker-view.svelte";
    import { resolve } from "$app/paths";

    type ImageMetadata = {
        photoId: number;
    };

    let {
        imageMetadata = $bindable([]),
        tripId,
        errors = [],
        errorsBesidePicker = false,
        selectedPhotoId = $bindable(null),
    }: {
        imageMetadata?: ImageMetadata[] | null;
        tripId: number;
        errors?: string[];
        errorsBesidePicker?: boolean;
        selectedPhotoId: number | null;
    } = $props();
    
    
    
</script>

<div class="d-flex justify-content-center">
    <div
            class="row row-cols-1 row-cols-md-2 row-cols-xl-3 g-2 container image-display-wrapper"
    >
        {#if errorsBesidePicker && errors.some((error) => error?.trim())}
            <div class="col">
                <div class="picker-errors">
                    <ExpandableErrorDisplay {errors} lines={5} />
                </div>
            </div>
        {/if}
        {#each imageMetadata ?? [] as image, index (image.photoId)}
            <div class="col">
                <div class="position-relative">
                    <ScrapboardImagePickerView
                            src={resolve(`/api/album/${tripId}/images/${image.photoId}`)} 
                            bind:selectedPhotoId={selectedPhotoId}
                            imagePhotoId={image.photoId}
                    />
                </div>
            </div>
        {/each}
    </div>
</div>

{#if !imageMetadata || imageMetadata.length === 0}
    <div
            class="position-relative d-flex flex-column align-items-center justify-content-center text-center"
    >
        <p class="fs-3">No images found</p>
    </div>
{/if}
{#if !errorsBesidePicker}
    <ExpandableErrorDisplay {errors} lines={5} />
{/if}





<style>
    .has-error :global(.img-thumbnail) {
        border-color: var(--bs-danger, #dc3545);
        border-width: 2px;
    }

    .picker-tile {
        width: 100%;
        aspect-ratio: 1;
        display: flex;
    }

    .picker-tile :global(> div) {
        flex: 1;
        display: flex;
        flex-direction: column;
    }

    .picker-tile :global(button) {
        flex: 1;
        font-size: 1.5rem;
    }

    .picker-errors {
        min-height: 100%;
        padding: 0.85rem 1rem;
        border: 2px solid var(--bs-danger);
        border-radius: 0.5rem;
        background: #ffffff;
        display: flex;
        align-items: center;
    }

    .clickable-image-wrapper img {
        width: 100%;
        aspect-ratio: 1;
        object-fit: cover;
    }
</style>
