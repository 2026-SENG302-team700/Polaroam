<script lang="ts">
    import Excalidraw from "svelte-excalidraw";
    import {type ExcalidrawImperativeAPI} from "@excalidraw/excalidraw/types";
    import { exportToBlob } from "@excalidraw/excalidraw";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import {onMount} from "svelte";
    import {resolve} from "$app/paths";
    import {page} from "$app/state";
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";

    let excalidrawAPI: ExcalidrawImperativeAPI | undefined = $state();
    let saving = $state(false);
    let loadError = $state("");
    
    function dataURLtoFile(dataURL, filename, mimeType) {
        const arr = dataURL.split(",");
        const byteString = atob(arr[1]);
        const bytes = new Uint8Array(byteString.length);
        for (let i = 0; i < byteString.length; i++) {
            bytes[i] = byteString.charCodeAt(i);
        }
        return new File([bytes], filename, { type: mimeType });
    }
    
    // to be removed once proper implementation with backend is created
    const initialData = {
        elements: [
            {
                type: "rectangle",
                version: 141,
                versionNonce: 361174001,
                isDeleted: false,
                id: "rect-1",
                fillStyle: "hachure",
                strokeWidth: 1,
                strokeStyle: "solid",
                roughness: 1,
                opacity: 100,
                angle: 0,
                x: 100,
                y: 100,
                strokeColor: "#000000",
                backgroundColor: "#868dfb",
                width: 300,
                height: 200,
                seed: 196841035,
                groupIds: [],
                roundness: null,
                boundElements: null,
                updated: 1,
            },
        ],
        appState: {
            viewBackgroundColor: "#ffffff",
            gridSize: null,
        },
        scrollToContent: true,
    };
    // end of init data block

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
    }
    
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
            const elements = JSON.parse(scrapboard.elements);
            excalidrawAPI.updateScene({elements})
        } catch (error) {
            loadError = "Failed to load scrapboard";
        }
    }
    
    onMount(() => {
        loadScrapboard();
    })
</script>
<div class="page-wrapper">
    <div class="scrapboard-buttons">
        <NavigationButton path="/home" text="Back to Trip" />
        <CreationButton
                text="Update Scrapboard"
                onClick={saveScrapboard}
                loading={saving}
        />
    </div>
    <div class="scrapboard-wrapper">
        <Excalidraw
                bind:excalidrawAPI
                {initialData}
                theme="light"
                name="Basic Test"
                viewModeEnabled={false}
                zenModeEnabled={false}
                gridModeEnabled={false}
                autoFocus={true}
        />
    </div>
</div>
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