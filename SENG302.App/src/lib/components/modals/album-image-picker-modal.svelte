<script lang="ts">
    import ScrapBoardImagePickerDisplay from "$lib/components/images/scrapboard-image-picker-display.svelte";
    import {onMount} from "svelte";
    import {resolve} from "$app/paths";

    let {
        tripId,
        show = $bindable(false),
    }: {
        tripId: number;
        show: boolean;
    } = $props();

    let imageMetadata = $state<{ photoId: number }[]>([]);
    let loading = $state(false);
    let imageError = $state("");
    let metadataLoaded = $state(false);

    let selectedPhotoId = $state<number | null>(null);
    
    /**
     * Gets the image metadata of all images in the album
     */
    async function getImageMetadata() {
        try {
            loading = true;
            const response = await fetch(
                resolve(`/api/album/${tripId}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            if (!response.ok) {
                imageError = await response.text();
                return;
            }
            imageMetadata = await response.json();
            metadataLoaded = true;
        } catch (err) {
            imageError = "An unexpected error occurred: ";
        } finally {
            loading = false;
        }
    }

    onMount(async () => {
        getImageMetadata();
    });

    /**
     * Get image to drag to the scrapboard
     */
    async function getImage() {
        if (selectedPhotoId == null) {
            imageError = "Image has not been selected. Click on image to select a image"
        } else {
            const response = await fetch(resolve(`/api/album/${tripId}/images/${selectedPhotoId}` as any));
        }
    }
</script>
{#if show}
    <div class="modal-backdrop show"></div>
    <div class="modal d-block" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-dialog-centered modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-title">
                    <span class="card-subtitle mb-2 h2">Select one image to insert</span>
                    <button
                            class = "btn"
                            aria-label="exit"
                            onclick={() => {
                            show = false;
                        }}
                    >
                        <i class="bi bi-x" />
                    </button>
                </div>
                <div class="modal-body">
                    <ScrapBoardImagePickerDisplay tripId={tripId} imageMetadata={imageMetadata} bind:selectedPhotoId={selectedPhotoId}/>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary select-button" onclick={getImage} disabled={loading && !metadataLoaded}>Select</button>
                    {#if imageError !== ""}
                        <span class="danger">{imageError}</span>
                        {/if}
                </div>
            </div>
        </div>
    </div>
{/if}

<style>
    .modal-title {
        display: flex;
        width: 100%;
        margin-left: 5px;
        margin-bottom: 5px;
        margin-top: 5px;
        align-items: center;
        justify-content: center;
        gap: 10px;
    }
    
    .select-button {
        width: 100%;
        margin-top: 10px;
        margin-bottom: 10px;
    }

    .modal-content {
        display: flex;
        flex-direction: column;
        max-height: 90vh; 
    }

    .modal-body {
        overflow-y: auto; 
        flex-grow: 1; 
    }

    .modal-footer {
        display: flex;
        flex-direction: column;
        flex-shrink: 0;
        background: #fff; 
    }
</style>