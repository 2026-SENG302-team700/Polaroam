<script lang="ts">
    import ScrapBoardImagePickerDisplay from "$lib/components/images/scrapboard-image-picker-display.svelte";
    import { onMount } from "svelte";
    import { resolve } from "$app/paths";
    import { type ExcalidrawImperativeAPI } from "@excalidraw/excalidraw/types";
    import type { FileId } from "@excalidraw/excalidraw/element/types";
    import { convertToExcalidrawElements } from "@excalidraw/excalidraw";

    let {
        tripId,
        show = $bindable(false),
        excalidrawAPI = $bindable(undefined),
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

    /**
     * Open the file picker modal
     */
    function openModal() {
        if (!dialog.open) {
            dialog.showModal();
        }
    }

    /**
     * Close the file picker modal
     */
    function closeModal() {
        dialog.close();
        show = false;
    }

    /**
     * Effect to open the modal when the show prop is set to true, and close it when it is set to false
     */
    $effect(() => {
        if (show) {
            openModal();
        } else if (dialog?.open) {
            dialog.close();
        }
    });

    /**
     * On mount that gets all required image metdata for the image picker modal
     */
    onMount(async () => {
        getImageMetadata();
    });

    interface ImageDimensions {
        width: number;
        height: number;
    }

    /**
     * Gets the dimensions of an image file
     * @param file - the image file
     * @returns the dimensions of the image file
     */
    async function getImageDimensions(file: File): Promise<ImageDimensions> {
        const bitmap = await createImageBitmap(file);
        const { width, height } = bitmap;
        bitmap.close();

        return { width, height };
    }

    /**
     * Reads a file as a data URL
     * @param file the file to read
     * @returns a promise resolving to the data URL
     */
    function readFileAsDataUrl(file: File): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                if (typeof reader.result == "string") {
                    resolve(reader.result);
                } else {
                    reject(new Error("Could not read image"));
                }
            };

            reader.onerror = () => {
                reject(reader.error);
            };
            reader.readAsDataURL(file);
        });
    }

    /**
     * Appends the selected image to the excalidraw canvas
     */
    async function appendImage() {
        if (!image) return;

        dataUrl = await readFileAsDataUrl(image);

        const { width: originalWidth, height: originalHeight } =
            await getImageDimensions(image);
        const maxWidth = 400;
        const maxHeight = 300;
        const reseizeRatio = Math.min(
            maxWidth / originalWidth,
            maxHeight / originalHeight,
        );
        const width = originalWidth * reseizeRatio;
        const height = originalHeight * reseizeRatio;

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
                    status: "saved",
                    scale: [1, 1],
                    width: width,
                    height: height,
                },
            ]);
            excalidrawAPI?.addFiles([
                {
                    id: uniqueId,
                    dataURL: dataUrl as any,
                    mimeType: image.type as any,
                    created: Date.now(),
                },
            ]);

            excalidrawAPI?.updateScene({
                elements: [...excalidrawAPI!.getSceneElements(), imageElement],
            });
        } catch (err) {
            imageError = "An unexpected error occurred: ";
        } finally {
            loading = false;
            closeModal();
        }
    }
</script>

{#if show}
    <dialog bind:this={dialog} class="modal-dialog-container">
        <div class="modal-content">
            <div class="modal-title">
                <span class="card-subtitle mb-2 h2"
                    >Select one image to insert</span
                >
                <button
                    class="btn close-button"
                    aria-label="exit"
                    onclick={() => {
                        show = false;
                    }}
                >
                    <i class="bi bi-x" />
                </button>
            </div>
            <div class="modal-body">
                <ScrapBoardImagePickerDisplay
                    {tripId}
                    {imageMetadata}
                    bind:selectedPhotoId
                    bind:image
                />
            </div>
            <div class="modal-footer">
                <button
                    class="btn btn-secondary select-button"
                    onclick={appendImage}
                    disabled={loading && !metadataLoaded}>Select</button
                >
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
