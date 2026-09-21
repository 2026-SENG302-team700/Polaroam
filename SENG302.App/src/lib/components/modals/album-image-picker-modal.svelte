<script lang="ts">
    import ScrapBoardImagePickerDisplay from "$lib/components/images/scrapboard-image-picker-display.svelte";
    import {onMount} from "svelte";
    import {resolve} from "$app/paths";
    import {type ExcalidrawImperativeAPI} from "@excalidraw/excalidraw/types";
    import type { FileId } from "@excalidraw/excalidraw/element/types";
    import { convertToExcalidrawElements } from "@excalidraw/excalidraw";

    let {
        tripId,
        show = $bindable(false),
        excalidrawAPI = $bindable(undefined)
    }: {
        tripId: number;
        show: boolean;
        excalidrawAPI: ExcalidrawImperativeAPI | undefined;
    } = $props();

    let dialog: HTMLDialogElement;
    let imageMetadata = $state<{ photoId: number }[]>([]);
    let loading = $state(false);
    let imageError = $state("");
    let metadataLoaded = $state(false);
    let image = $state<File | null>(null);
    let dataUrl = $state<string | ArrayBuffer | null | undefined>(null);
    let uniqueId = $state<FileId>();


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

    async function appendImage() {
        if (!image) return;
        const reader = new FileReader();
        reader.onload = (event) => {
            dataUrl = event.target?.result as string;
        };
        await reader.readAsDataURL(image);

        uniqueId = crypto.randomUUID() as FileId;
        // create element
        try {
            const [imageElement] = convertToExcalidrawElements([
            {
                type: "image",
                id: `image_${Date.now()}`,
                fileId: uniqueId,
                x: 200,
                y: 150,
                width: 200,
                height: 200,
                status: "saved",
                scale: [1, 1],
            },
            ]);
            excalidrawAPI?.addFiles([{
                    id: uniqueId,
                    dataURL: dataUrl as any,
                    mimeType: image.type as any,
                    created: Date.now(),
                }])

            excalidrawAPI?.updateScene({
                elements: [...excalidrawAPI!.getSceneElements(), imageElement],
            })
        } catch (err) {
            imageError = "An unexpected error occurred: ";
        } finally {
            loading = false;
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
                <ScrapBoardImagePickerDisplay tripId={tripId} imageMetadata={imageMetadata} bind:selectedPhotoId={selectedPhotoId} bind:image={image}/>
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary select-button" onclick={appendImage} disabled={loading && !metadataLoaded}>Select</button>
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