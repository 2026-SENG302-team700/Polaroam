<script lang="ts">
    import Excalidraw from "svelte-excalidraw";
    import type {
        BinaryFileData,
        ExcalidrawImperativeAPI,
    } from "@excalidraw/excalidraw/types";
    // @ts-expect-error React is installed for Excalidraw but its optional type package is not.
    import React from "react";
    import { exportToBlob } from "@excalidraw/excalidraw";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { onMount } from "svelte";
    import { resolve } from "$app/paths";
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import AlbumImagePickerModal from "$lib/components/modals/album-image-picker-modal.svelte";
    import { addToast } from "$lib/toast/toast";

    type ScrapboardFileDto = {
        id: string;
        mimeType: string;
        blob: string;
    };

    let excalidrawAPI: ExcalidrawImperativeAPI | undefined = $state();
    let saving = $state(false);
    let loadError = $state("");
    let showBackModal = $state(false);
    let showPickerModal = $state(false);
    let lastSaveState = $state("");
    let tripId = Number(page.params.tripId);
    const excalidrawUiOptions = {
        tools: {
            image: false,
        },
    } as any;

    /**
     * Helper function to convert the Excalidraw files into a standard file type.
     */
    function dataURLtoFile(
        dataURL: string,
        filename: string,
        mimeType: string,
    ) {
        const arr = dataURL.split(",");
        const byteString = atob(arr[1]);
        const bytes = new Uint8Array(byteString.length);
        for (let i = 0; i < byteString.length; i++) {
            bytes[i] = byteString.charCodeAt(i);
        }
        return new File([bytes], filename, { type: mimeType });
    }

    /**
     * Gets the elements, app state, and files from the scene and saves them to
     * be reloaded later. Also creates a thumbnail of the scrapboard.
     */
    async function saveScrapboard() {
        if (!excalidrawAPI) return;

        saving = true;
        try {
            const elements = excalidrawAPI.getSceneElements();
            const appState = excalidrawAPI.getAppState();
            const files = excalidrawAPI.getFiles();

            const blob = await exportToBlob({
                elements,
                appState,
                files,
                mimeType: "image/png",
                exportPadding: 10,
            });

            const form = new FormData();
            form.append("TripId", tripId.toString());
            form.append("Elements", JSON.stringify(elements));
            form.append("Thumbnail", blob);

            Object.entries(files).forEach(([fileId, fileData]) => {
                const extension = fileData.mimeType.split("/")[1] ?? "bin";
                const file = dataURLtoFile(
                    fileData.dataURL,
                    `${fileId}.${extension}`,
                    fileData.mimeType,
                );
                form.append("Files", file);
            });

            const response = await fetchWithCsrf(
                resolve("/api/scrapboard/save" as any),
                {
                    method: "PUT",
                    body: form,
                    credentials: "include",
                },
            );

            if (response.ok) {
                lastSaveState = JSON.stringify(elements);
                addToast("Scrapboard saved", "success");
            } else {
                addToast("Failed to save scrapboard", "error");
            }
        } finally {
            saving = false;
        }
    }

    /**
     * Gets the trip's scrapboard from the backend, adds the files, then updates
     * the scene elements.
     */
    async function loadScrapboard() {        
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/scrapboard/${tripId}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );

            if (!response.ok) {
                loadError = "Failed to load scrapboard";
                return;
            }

            const scrapboard = await response.json();
            lastSaveState = scrapboard.elements;

            const files: BinaryFileData[] = scrapboard.files.map(
                (file: ScrapboardFileDto) => ({
                    id: file.id as BinaryFileData["id"],
                    mimeType: file.mimeType as BinaryFileData["mimeType"],
                    dataURL: `data:${file.mimeType};base64,${file.blob}` as BinaryFileData["dataURL"],
                    created: Date.now(),
                }),
            );

            excalidrawAPI!.addFiles(files);
            if (scrapboard.elements != "") {
                const elements = JSON.parse(scrapboard.elements);
                excalidrawAPI!.updateScene({ elements });
                excalidrawAPI!.scrollToContent(elements, {
                    fitToContent: true,
                    animate: true,
                    duration: 300,
                });
            }
        } catch (error) {
            loadError = `Failed to load scrapboard ${error}`;
        }
    }

    /**
     * Opens a warning before leaving when the scrapboard has unsaved changes.
     */
    function handleBackButton() {
        const currentState = JSON.stringify(excalidrawAPI!.getSceneElements());
        if (currentState !== lastSaveState) {
            showBackModal = true;
        } else {
            goto(resolve(`/home/trips/${tripId}`));
        }
    }

    function showImagePickerModal() {
        showPickerModal = true;
    }

    onMount(() => {
        loadScrapboard();
    });

    /** Builds a custom hamburger menu containing only the required actions. */
    function buildMainMenu(mod: typeof import("@excalidraw/excalidraw")) {
        const { MainMenu } = mod;
        const Def = MainMenu.DefaultItems;
        return React.createElement(MainMenu, { key: "main-menu" }, [
            React.createElement(Def.ClearCanvas, { key: "clearCanvas" }),
            React.createElement(Def.ChangeCanvasBackground, {
                key: "changeCanvasBackground",
            }),
        ]);
    }

    function buildChildren(mod: typeof import("@excalidraw/excalidraw")) {
        return React.createElement(React.Fragment, null, [buildMainMenu(mod)]);
    }
</script>

<div class="page-wrapper">
    <div class="scrapboard-buttons">
        <button class="btn btn-primary back-button" onclick={handleBackButton}>
            Back to Trips
        </button>

        <div class="scrapboard-buttons-right">
            <CreationButton
                text="Update Scrapboard"
                onClick={saveScrapboard}
                loading={saving}
            />
            <button
                class="btn btn-secondary bi-images"
                aria-label="Add images from trip album"
                onclick={showImagePickerModal}
            ></button>
        </div>
    </div>

    {#if loadError}
        <p class="text-danger">{loadError}</p>
    {/if}

    <div class="scrapboard-wrapper">
        <Excalidraw
            childrenBuilder={buildChildren}
            bind:excalidrawAPI
            theme="light"
            name="Basic Test"
            viewModeEnabled={false}
            zenModeEnabled={false}
            gridModeEnabled={false}
            autoFocus={true}
            UIOptions={excalidrawUiOptions}
        />
    </div>
</div>

<AlbumImagePickerModal {tripId} bind:show={showPickerModal} {excalidrawAPI} />

<ConfirmationModal
    message="You have unsaved changes."
    option1Text="Cancel"
    option2Text="Discard"
    option3Text="Save"
    option2Style="danger"
    option3Style="primary"
    onOption1={() => {}}
    onOption2={() => goto(resolve(`/home/trips/${tripId}`))}
    onOption3={async () => {
        await saveScrapboard();
        goto(resolve(`/home/trips/${tripId}`));
    }}
    bind:show={showBackModal}
/>

<style>
    .scrapboard-wrapper {
        height: 70vh;
        border: 3px solid black;
        border-radius: 5px;
        box-shadow: 8px 8px black;
    }

    .scrapboard-buttons {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 5px;
    }

    .scrapboard-buttons-right {
        display: flex;
        align-items: center;
        gap: 5px;
    }

    .back-button {
        margin-right: 5px;
    }
</style>
