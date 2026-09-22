<script lang="ts">
    import type { Snippet } from "svelte";
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import ClickableImage from "./clickable-image.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import ExpandableErrorDisplay from "$lib/components/forms/expandable-error-display.svelte";

    type ImageViewContext = "search" | "album" | "post" | "none";

    let {
        images = $bindable([]),
        photoIds = [],
        letDelete = false,
        ondelete,
        errors = [],
        pickerFirst = false,
        viewContext = "none",
        children,
    }: {
        images?: File[] | null;
        photoIds?: number[];
        letDelete?: boolean;
        ondelete?: (image: File, index: number) => void;
        errors?: string[];
        pickerFirst?: boolean;
        viewContext?: ImageViewContext;
        children?: Snippet;
    } = $props();

    let confirmationModalShown = $state(false);
    let pendingDeleteIndex: number | null = $state(null);

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
        if (pendingDeleteIndex !== null && images) {
            const [removed] = images.splice(pendingDeleteIndex, 1);
            ondelete?.(removed, pendingDeleteIndex);
            pendingDeleteIndex = null;
        }
        confirmationModalShown = false;
    }

    /**
     * Checks whether a given image has a validation error associated with it
     */
    function hasError(image: File): boolean {
        return errors.some((error) => error.startsWith(`${image.name}:`));
    }
</script>

<div
    class="row row-cols-1 row-cols-md-2 row-cols-xl-3 g-2 container image-display-wrapper"
>
    {#if pickerFirst}
        <div class="col">
            <div class="picker-tile">
                {@render children?.()}
            </div>
        </div>
    {/if}
    {#each images ?? [] as image, index}
        <div class="col">
            <div class="position-relative" class:has-error={hasError(image)}>
                <ClickableImage
                    {image}
                    {index}
                    {images}
                    photoId={photoIds[index]}
                    {photoIds}
                    {viewContext}
                />
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
    {#if images && images.length > 0 && !pickerFirst}
        <div class="col">
            <div class="picker-tile">
                {@render children?.()}
            </div>
        </div>
    {:else if (!images || images.length === 0) && !pickerFirst}
        <div class="col">
            <div class="picker-tile">
                {@render children?.()}
            </div>
        </div>
    {/if}
</div>

{#if !images || images.length === 0}
    <p class="empty-message">No image selected</p>
{/if}
<ExpandableErrorDisplay {errors} lines={5} />

<ConfirmationModal
    bind:show={confirmationModalShown}
    message="Are you sure you want to delete this image?"
    option1Text="Do not delete"
    option2Text="Delete"
    option2Style="danger"
    onOption1={() => (confirmationModalShown = false)}
    onOption2={() => ConfirmDelete()}
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

    .empty-message {
        margin: 0.5rem 0 0;
        color: #686868;
        font-size: 0.9rem;
    }
</style>
