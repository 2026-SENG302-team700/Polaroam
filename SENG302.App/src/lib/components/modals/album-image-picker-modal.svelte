<script lang="ts">
    import type { Snippet } from "svelte";
    import ImageGallery from "../images/image-gallery.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import {onMount} from "svelte";
    import {resolve} from "$app/paths";
    import {page} from "$app/state";

    let {
        tripId,
        show = $bindable(false),
    }: {
        tripId: number;
        show: boolean;
    } = $props();

    let imageMetadata = $state<{ photoId: number }[]>([]);
    let loading = $state(false);
    let imageError = $state("");
    let metadataLoaded = $state(false);
    
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

    onMount(async () => {
        getImageMetadata();
    });
</script>

{#if show}
    <div class="modal-backdrop show"></div>
    <div class="modal d-block" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-dialog-centered modal-lg" role="document">
            <div class="modal-content">
                <ImageGallery tripId={tripId} imageMetadata={imageMetadata}/>
            </div>
        </div>
    </div>
{/if}