<script lang="ts">
    import type { Snippet } from "svelte";
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import ClickableImage from "./clickable-image.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import ExpandableErrorDisplay from "$lib/components/forms/expandable-error-display.svelte";
    import { resolve } from "$app/paths";

    let {
        imageMetadata = $bindable([]),
        tripId,
        letDelete = false,
        ondelete,
        errors = [],
        children,
    }: {
        imageMetadata?: [] | null;
        tripId: number;
        letDelete?: boolean;
        ondelete?: (image: File, index: number) => void;
        errors?: string[];
        children?: Snippet;
    } = $props();

    let confirmationModalShown = $state(false);
    let pendingDeleteIndex: number | null = $state(null);
    let dataUrl = $state('');

    /**
     * Stores the index of the image to be deleted so it is not out of scope
     * Shows the confirmation modal
     * @param index, the index of the image to be deleted
     */
    function RequestDelete(index: number) {
        pendingDeleteIndex = index;
        confirmationModalShown = true;
    }

    /**
     * Performs the deletion of the image and closes the modal
     */
    function ConfirmDelete() {
        if (pendingDeleteIndex !== null && imageMetadata) {
            const [removed] = imageMetadata.splice(pendingDeleteIndex, 1);
            ondelete?.(removed, pendingDeleteIndex);
            pendingDeleteIndex = null;
        }
        confirmationModalShown = false;
    }
</script>

<div
        class="row row-cols-1 row-cols-md-2 row-cols-xl-3 g-2 container image-display-wrapper"
>
    
    {#each imageMetadata ?? [] as image, index}
        <div class="col">
            <div class="position-relative">
                <div class="clickable-image-wrapper">
                    <img src={resolve(`/api/album/${tripId}/images/${image.fileKey}`)} loading="lazy" decoding="async" alt="Gallery item" />
                </div>
                {#if letDelete}
                    <div class="position-absolute top-0 end-0 m-1">
                        <DeleteButton
                                deleteAction={() => RequestDelete(index)}
                                class="btn-sm"
                        />
                    </div>
                {/if}
            </div>
        </div>
    {/each}
    {#if imageMetadata && imageMetadata.length > 0}
        <div class="col">
            <div class="picker-tile">
                {@render children?.()}
            </div>
        </div>
    {/if}
</div>

{#if !imageMetadata || imageMetadata.length === 0}
    <p>No image selected</p>
    {@render children?.()}
{/if}
<ExpandableErrorDisplay {errors} lines={5} />

<ConfirmationModal
        bind:show={confirmationModalShown}
        message="Are you sure you want to delete this image?"
        option1Text="Delete"
        option2Text="Do not delete"
        onOption1={() => ConfirmDelete()}
        onOption2={() => (confirmationModalShown = false)}
/>

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

    .clickable-image-wrapper img {
        width: 100%;
        aspect-ratio: 1;
        object-fit: cover;
    }
</style>