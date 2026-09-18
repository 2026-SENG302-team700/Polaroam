<script lang="ts">
    import type { Snippet } from "svelte";
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import ExpandableErrorDisplay from "$lib/components/forms/expandable-error-display.svelte";
    import LazyClickableImage from "$lib/components/images/lazy-clickable-image.svelte";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { goto } from "$app/navigation";

    type ImageMetadata = {
        photoId: number;
    };

    let {
        imageMetadata = $bindable([]),
        tripId,
        errors = [],
        errorsBesidePicker = false,
        children,
    }: {
        imageMetadata?: ImageMetadata[] | null;
        tripId: number;
        errors?: string[];
        errorsBesidePicker?: boolean;
        children: Snippet;
    } = $props();

    let photoIds = $derived(imageMetadata?.map((m) => m.photoId) ?? []);

    let letDelete = true;
    let confirmDeleteModal = $state(false);
    let goToPostModal = $state(false);
    let pendingDeleteIndex: number | null = $state(null);
    let dataUrl = $state("");

    let deleteError = $state("");
    let linkedPostId = $state(0);

    /**
     * Stores the index of the image to be deleted so it is not out of scope
     * Shows the confirmation modal
     * @param index, the index of the image to be deleted
     */
    async function RequestDelete(index: number) {
        const image = imageMetadata?.[index];
        if (!image) return;

        deleteError = "";
        try {
            const response = await fetchWithCsrf(
                resolve(
                    `/api/trips/${tripId}/image/${image.photoId}/context` as any,
                ),
                { method: "GET", credentials: "include" },
            );

            if (!response.ok) {
                deleteError =
                    "Failed to check whether the image can be deleted";
                return;
            }

            const imageContext = await response.json();
            if (imageContext.postId) {
                linkedPostId = imageContext.postId;
                goToPostModal = true;
                return;
            }

            pendingDeleteIndex = index;
            confirmDeleteModal = true;
        } catch {
            deleteError = "Failed to check whether the image can be deleted";
        }
    }

    /**
     * Performs the deletion of the image and closes the modal
     */
    async function ConfirmDelete() {
        deleteError = "";
        if (pendingDeleteIndex !== null && imageMetadata) {
            try {
                const item = imageMetadata[pendingDeleteIndex];
                const response = await fetchWithCsrf(
                    resolve(
                        `/api/album/${tripId}/images/${item.photoId}` as any,
                    ),
                    {
                        method: "DELETE",
                        credentials: "include",
                    },
                );
                if (response.status == 200 || response.status == 204) {
                    imageMetadata = imageMetadata.filter(
                        (_, i) => i !== pendingDeleteIndex,
                    );
                }

                if (response.status == 405) {
                    linkedPostId = await response.json();
                    goToPostModal = true;
                }
            } catch {
                deleteError = "Failed to delete image";
            }

            pendingDeleteIndex = null;
        }
        confirmDeleteModal = false;
    }
</script>

<div class="d-flex justify-content-center">
    <div
        class="row row-cols-1 row-cols-md-2 row-cols-xl-3 g-2 container image-display-wrapper"
    >
        <div class="col">
            <div class="picker-tile">
                {@render children?.()}
            </div>
        </div>
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
                    <LazyClickableImage
                        src={resolve(
                            `/api/album/${tripId}/images/${image.photoId}`,
                        )}
                        {index}
                        viewContext="album"
                        images={imageMetadata.map((m) =>
                            resolve(`/api/album/${tripId}/images/${m.photoId}`),
                        )}
                        photoId={image.photoId}
                        {photoIds}
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
<ExpandableErrorDisplay errors={[deleteError]} lines={1} />

<ConfirmationModal
    bind:show={confirmDeleteModal}
    message="Are you sure you want to delete this image?"
    option1Text="Delete"
    option2Text="Do not delete"
    onOption1={() => ConfirmDelete()}
    onOption2={() => (confirmDeleteModal = false)}
/>

<ConfirmationModal
    bind:show={goToPostModal}
    message="Selected image belongs to a post, delete the image from the post to remove the image."
    option1Text="Go to post"
    option2Text="Cancel"
    option1Style="primary"
    onOption1={() => {
        goto(resolve(`/home/trips/${tripId}/posts/${linkedPostId}`));
    }}
    onOption2={() => (goToPostModal = false)}
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
