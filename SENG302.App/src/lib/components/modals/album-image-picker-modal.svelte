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

    let dialog: HTMLDialogElement;
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
    
    function openModal() {
        if (!dialog.open) {
            dialog.showModal()
        }
    }
    
    function closeModal() {
        dialog.close()
        show = false;
    }
    
    $effect(() => {
        if (show) {
            openModal()
        } else if (dialog?.open) {
            dialog.close()
        }
    })

    onMount(async () => {
        getImageMetadata();
    });

    /**
     * Get image to drag to the scrapboard
     */
    async function getImage() {
        console.log(`Selected photo Id: ${selectedPhotoId}`)
        if (selectedPhotoId == null) {
            imageError = "Image has not been selected. Click on image to select a image"
        } else {
            const response = await fetch(resolve(`/api/album/${tripId}/images/${selectedPhotoId}` as any));
            console.log(JSON.stringify(response))
        }
    }
</script>
{#if show}
    <dialog bind:this={dialog} class="modal-dialog-container">
        <div class="modal-content">
            <div class="modal-title">
                <span class="card-subtitle mb-2 h2">Select one image to insert</span>
                <button
                        class = "btn close-button"
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
    </dialog>
{/if}

<style>
    .modal-dialog-container {
        width: min(900px, 90vw);
        max-height: 90vh;
        padding: 0;
        border: none;
        border-radius: 5px;
        
        max-width: 90vw;
        overflow: hidden;
    }
    
    .modal-title {
        position: relative;
        display: flex;
        width: 100%;
        align-items: center;
        justify-content: center;
        text-align: center;
        padding: 0.75rem 3.5rem;
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
        padding-top: 6px;
        overflow-y: auto; 
        flex-grow: 1; 
    }

    .modal-footer {
        display: flex;
        flex-direction: column;
        flex-shrink: 0;
        background: #fff; 
    }
    
    .close-button {
        position: absolute;
        top: 0.5rem;
        right: 0.75rem;
        font-size: 1.5rem;
        line-height: 1;
    }
    
</style>