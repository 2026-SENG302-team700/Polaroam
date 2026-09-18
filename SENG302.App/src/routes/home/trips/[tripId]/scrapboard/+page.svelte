<script lang="ts">
    import Excalidraw from "svelte-excalidraw";
    import {type ExcalidrawImperativeAPI} from "@excalidraw/excalidraw/types";
    import { exportToBlob } from "@excalidraw/excalidraw";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import {onMount} from "svelte";
    import {resolve} from "$app/paths";
    import {page} from "$app/state";

    let excalidrawAPI: ExcalidrawImperativeAPI | undefined = $state();
    let saving = $state(false);
    let error = $state("");

    
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
        
        console.log("trip id", page.params.tripId);
        console.log("elements", elements);
        console.log("Files", files ? files : []);
        console.log("thumbnail", blob)
        
        const form = new FormData();
        form.append("TripId", page.params.tripId);
        form.append("Elements", elements);
        form.append("Thumbnail", blob);

        Object.entries(files).forEach(([fileId, fileData]) => {
            const extension = fileData.mimeType.split("/")[1] ?? "bin";
            const file = dataURLtoFile(fileData.dataURL, `${fileId}.${extension}`, fileData.mimeType);
            form.append("Files", file);
        });
        
        
        const response = await fetchWithCsrf(resolve(`/api/scrapboard/save` as any), {
            method: "POST",
            body: form,
            credentials: "include",
        });
        
        console.log(response.ok);
    }
    
    async function loadScrapboard() {
        
    }
    
</script>
<div>
    <CreationButton
            text="save"
            onClick={saveScrapboard}
            loading={saving}
    />

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
</style>