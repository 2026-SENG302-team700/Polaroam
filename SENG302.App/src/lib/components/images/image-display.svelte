<script lang="ts">
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import ClickableImage from "./clickable-image.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";

    let {
        images = $bindable([]),
        letDelete = false,
        ondelete,
        errors = [],
    }: { images?: File[] | null;
         letDelete?: boolean
         ondelete?: (index: number) => void
         errors?: string[] } = $props();

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
        if (pendingDeleteIndex !== null) {
            images.splice(pendingDeleteIndex, 1);
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
    {#each images ?? [] as image, index}
        <div class="col">
            <div class="position-relative" class:has-error={hasError(image)}>
                <ClickableImage {image} {index} {images} />
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
    {:else}
        <p>No image selected</p>
    {/each}
</div>

<style>
    .has-error :global(.img-thumbnail) {
        border-color: var(--bs-danger, #dc3545);
        border-width: 2px;
    }
</style>
</div>

<ConfirmationModal
    bind:show={confirmationModalShown}
    message="Are you sure you want to delete this image?"
    option1Text="Delete"
    option2Text="Do not delete"
    onOption1={() => ConfirmDelete()}
    onOption2={() => (confirmationModalShown = false)}
/>
