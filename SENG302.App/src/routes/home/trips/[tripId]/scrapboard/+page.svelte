<script lang="ts">
    import Excalidraw from "svelte-excalidraw";

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
</script>
<div class="page-wrapper">
    <div class="scrapboard-wrapper">
        <Excalidraw
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