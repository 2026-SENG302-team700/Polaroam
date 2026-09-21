<script lang="ts">
    import Excalidraw from "svelte-excalidraw";
    import {type ExcalidrawImperativeAPI} from "@excalidraw/excalidraw/types";
    import React from "react";
    import { exportToBlob } from "@excalidraw/excalidraw";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import {onMount} from "svelte";
    import {resolve} from "$app/paths";
    import {page} from "$app/state";
    import { goto } from "$app/navigation";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import { addToast } from "$lib/toast/toast";
    
    let excalidrawAPI: ExcalidrawImperativeAPI | undefined = $state();
    let saving = $state(false);
    let loadError = $state("");
    let showModal = $state(false);
    let lastSaveState = $state("");

    /**
     * Helper function to convert the Excalidraw files into a
     * standard file type
     * @param dataURL
     * @param filename
     * @param mimeType
     */
    function dataURLtoFile(dataURL, filename, mimeType) {
        const arr = dataURL.split(",");
        const byteString = atob(arr[1]);
        const bytes = new Uint8Array(byteString.length);
        for (let i = 0; i < byteString.length; i++) {
            bytes[i] = byteString.charCodeAt(i);
        }
        return new File([bytes], filename, { type: mimeType });
    }

    /**
     * Gets the elements, appstate, and files from the scene
     * and saves them to be reloaded later. Also creates a thumbnail
     * of the scrapboard to be used later
     */
    async function saveScrapboard() {
        const elements = excalidrawAPI!.getSceneElements();
        const appState = excalidrawAPI!.getAppState();
        const files = excalidrawAPI!.getFiles();
        
        const blob = await exportToBlob({
            elements,
            appState,
            files,
            mimeType: "image/png",
            exportPadding: 10,
        });
        
        const form = new FormData();
        form.append("TripId", page.params.tripId);
        form.append("Elements", JSON.stringify(elements));
        form.append("Thumbnail", blob);

        Object.entries(files).forEach(([fileId, fileData]) => {
            const extension = fileData.mimeType.split("/")[1] ?? "bin";
            const file = dataURLtoFile(fileData.dataURL, `${fileId}.${extension}`, fileData.mimeType);
            form.append("Files", file);
        });
        
        const response = await fetchWithCsrf(resolve(`/api/scrapboard/save` as any), {
            method: "PUT",
            body: form,
            credentials: "include",
        });
        lastSaveState = JSON.stringify(elements);
        addToast("Scrapboard saved", "success");
    }

    /**
     * Gets the trips scrapboard from the backend, adds the files then updates the 
     * scene elements
     */
    async function loadScrapboard() {
        try {
            const response = await fetchWithCsrf(resolve(`/api/scrapboard/${page.params.tripId}` as any), {
                method: "GET",
                credentials: "include",
            });
            
            if (!response.ok) {
                loadError = "Failed to load scrapboard";
            }
            
            const scrapboard = await response.json();
            lastSaveState = scrapboard.elements;

            const files: BinaryFileData[] = scrapboard.files.map((f: ScrapboardFileDto) => ({
                id: f.id,
                mimeType: f.mimeType,
                dataURL: `data:${f.mimeType};base64,${f.blob}`,
                created: Date.now(),
            }));
            excalidrawAPI!.addFiles(files);
            const elements = JSON.parse(scrapboard.elements);
            excalidrawAPI.updateScene({elements})
        } catch (error) {
            loadError = "Failed to load scrapboard";
        }
    }

    /**
     * Checks whether the scrapboard has been updated,
     * opens modal if changes have been made or returns to
     * trip page otherwise
     */
    function handleBackButton() {
        const currentState = JSON.stringify(excalidrawAPI!.getSceneElements());
        if (currentState !== lastSaveState) {
            showModal = true;
        } else {
            goto(resolve(`/home/trips/${page.params.tripId}`))
        }
    }
    
    onMount(() => {
        loadScrapboard();
    })

    /**
     * Builds a custom hamburger menu to remove unneeded items
     */
    function buildMainMenu(mod: typeof import("@excalidraw/excalidraw")) {
        const { MainMenu } = mod;
        const Def = MainMenu.DefaultItems;
        return React.createElement(MainMenu, { key: "main-menu" }, [
            React.createElement(Def.ClearCanvas, { key: "clearCanvas" }),
            React.createElement(Def.ChangeCanvasBackground, { key: "changeCanvasBackground" }),
        ]);
    }

    /**
     * Calls other builder functions to build custom menu's
     */
    function buildChildren(mod: typeof import("@excalidraw/excalidraw")) {
        return React.createElement(React.Fragment, null, [
            buildMainMenu(mod),
        ]);
    }
    
    
</script>
<div class="page-wrapper">
    <div class="scrapboard-buttons">
        <button
            class="btn btn-primary"
            onclick={handleBackButton}
        >
            Back to Trips
        </button>
        
        <CreationButton
                text="Update Scrapboard"
                onClick={saveScrapboard}
                loading={saving}
        />
    </div>
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
        />
    </div>
</div>
<ConfirmationModal
    message="You have unsaved changes."
    option1Text="Cancel"
    option2Text="Discard"
    option3Text="Save"
    option2Style="danger"
    option3Style="primary"
    onOption1={() => {}}
    onOption2={() => goto(resolve(`/home/trips/${page.params.tripId}`))}
    onOption3={async () => {
        await saveScrapboard();
        goto(resolve(`/home/trips/${page.params.tripId}`));
    }}
    bind:show={showModal}
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
        margin-bottom: 5px;
    }
</style>