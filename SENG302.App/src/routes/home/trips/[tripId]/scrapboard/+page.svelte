<script lang="ts">
    import Excalidraw from "svelte-excalidraw";
    import {type ExcalidrawImperativeAPI} from "@excalidraw/excalidraw/types";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import {onMount} from "svelte";
    import {resolve} from "$app/paths";
    import {page} from "$app/state";
    import AlbumImagePickerModal from "$lib/components/modals/album-image-picker-modal.svelte";

    let excalidrawAPI: ExcalidrawImperativeAPI | undefined = $state();
    let saving = $state(false);
    let error = $state("");
    let showPickerModal = $state(false)
    let tripId = Number(page.params.tripId);

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
    
    async function onClick() {
        console.log("scene elements:")
        console.log(excalidrawAPI!.getSceneElements());
    }

    async function showImagePickerModal() {
        showPickerModal = true;
    }
    
</script>
<div>
    <CreationButton
            text="scrapboard elements"
            onClick={onClick}
            loading={saving}
    />
    <button class="btn btn-primary bi-image" onclick={showImagePickerModal}/>

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
                UIOptions={{
                    tools: {
                        image: false,
                    }
                }}
        />
    </div>
</div>

<AlbumImagePickerModal tripId={tripId} bind:show={showPickerModal}/>

<style>
    .scrapboard-wrapper {
        height: 70vh;
        border: 3px solid black;
        border-radius: 5px;
        box-shadow: 8px 8px black;
    }
</style>