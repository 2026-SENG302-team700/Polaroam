<script lang="ts">
    import Excalidraw from "svelte-excalidraw";
    import {type ExcalidrawImperativeAPI} from "@excalidraw/excalidraw/types";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import {onMount} from "svelte";
    import {resolve} from "$app/paths";
    import {page} from "$app/state";

    let excalidrawAPI: ExcalidrawImperativeAPI | undefined = $state();
    let saving = $state(false);
    let error = $state("");

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

    //let blankInitialData = {
    //    elements: [],
    //    appState: {
    //        viewBackgroundColor: "#ffffff",
    //        gridSize: null,
    //    },
    //    scrollToContent: true,
    //};

    async function onClick() {
        console.log("scene elements:")
        console.log(excalidrawAPI!.getSceneElements());
    }

    // async function getScrapboard(tripId: string | undefined) {
    //    if (tripId == undefined) {
    //        return blankInitialData;
    //    }

    //    const response = await fetchWithCsrf(
    //        resolve(`/api/trip/${tripId}/scrapboard` as any), {
    //            method: "GET",
    //            credentials: "include",
    //        },
    //    );

    //    if (!response.ok) {
    //        error = (await response.text()) || "Failed to fetch scrapboard.";
    //        return;
    //    }
    //    if (response.status === 404) {
    //        return blankInitialData
    //    }
    //    return {
    //        elements: await response.json(),
    //        appState: {
    //            viewBackgroundColor: "#ffffff",
    //            gridSize: null,
    //    },
    //        scrollToContent: true,
    //    }
    //}
</script>
<div class="page-wrapper">
    <CreationButton
            text="Save Scrapboard"
            onClick={onClick}
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
                gridModeEnabled={true}
                autoFocus={true}
        />
    </div>
</div>
<style>
.page-wrapper {
    display: flex;
    justify-content: center;
}
    .scrapboard-wrapper {
        height: 800px;
        width: 80vw;
        border: 3px solid black;
        border-radius: 5px;
        box-shadow: 8px 8px black;
    }
</style>