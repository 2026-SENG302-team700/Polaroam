<script lang="ts">
    import { onMount } from "svelte";
    import CreationButton from "../buttons/creation-button.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";
    import ScrapboardPlaceholder from "$lib/assets/images/scrapboard-placeholder.png"
    import ScrapboardExpanded from "$lib/components/scrapboard/scrapboard-expanded.svelte"

    let {tripId = -1} = $props();

    let scrapboardCreated: Boolean = $state(false);
    let scrapboard = $state([]); //not used yet
    let loading = $state(true);
    let showExpanded = $state(false);
    let scrapboardImage = $state('');

    onMount(()=> {fetchScrapboard()});
    
    /**
     * uses the tripId to fetch the scrapboard for the trip.
     * @async
     */
    async function fetchScrapboard() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/scrapboard/${tripId}/thumbnail` as any),
                {
                    method: "Get",
                    credentials: "include",
                },
            );

            if (response.status == 404) {
                scrapboardCreated = false;
                loading = false;
            } else if (response.ok) {
                scrapboardCreated = true;
                loading = false;
                const scrapboardBlob = await response.blob();
                scrapboardImage = URL.createObjectURL(scrapboardBlob);
                console.log(scrapboardImage);
            }
        } finally {
            loading = false;
        }
    }

    /**
     * uses the tripId to create a scrapboard for the trip.
     * @async
     */
    async function createScrapboard() {
        try {
            loading = true;
            const response = await fetchWithCsrf(resolve(`/api/scrapboard` as any), {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    TripId:tripId,
                }),
                credentials: "include",
            });
            readScrapboard(response);
        } finally {
            loading = false;
        }
    }
</script>

<div class="container mt-4">
    {#if scrapboardCreated && !loading}
        <img
            src={scrapboardImage}
            alt="scrapboard"
            class="scrapboard-image card"
            onclick={() => showExpanded = true}
        />
    {:else if !scrapboardCreated && !loading}
        <div class="scrapboard-image-wrapper empty-trip-page-element">
            <i class="bi bi-journal-album" aria-hidden="true"></i>
            <p>No Scrapboard yet.</p>
            <CreationButton
                {loading}
                text="Create Scrapboard"
                onClick={()=>createScrapboard()}
            />
            
        </div>
    {:else}
        <div class="scrapboard-image-wrapper empty-trip-page-element">
            <i class="bi bi-journal-album" aria-hidden="true"></i>
            <p>Loading Scrapboard...</p>
        </div>
    {/if}
</div>

<ScrapboardExpanded
        bind:shown={showExpanded}
        scrapboardImage={scrapboardImage}
        {tripId}
/>

<style>
    .scrapboard-image {
        width: 100%;
        cursor: pointer;
    }
</style>
