<script>
    import { resolve } from "$app/paths";
    import { goto } from "$app/navigation";
    import TripCard from "$lib/components/trip-components/trip-card.svelte";
    import ImageDisplay from "$lib/components/images/image-display.svelte";

    let {
        trips,
        photoTrips,
        query = ""
    } = $props();

    let imageCount = $derived(
        photoTrips.reduce((sum, photoTrip) => sum + (photoTrip.photos?.length ?? 0), 0)
    );

    /**
     * Navigates to the given trip's page
     */
    function goToTrip(tripId) {
        goto(resolve(`/home/trips/${tripId}`));
    }
</script>
<div>
    <h1>Search Results</h1>
    {#if query}
        <p class="search-summary">
            {imageCount} image{imageCount == 1 ? "" : "s"} found matching "{query}"
        </p>
    {/if}
    <div class="photo-trip-list">
        {#if photoTrips.length > 0}
                {#each photoTrips as photoTrip}
                    <div class="photo-trip">
                        <div
                            tabindex="0"
                            role="button"
                            class="photo-trip-header"
                            onclick={() => goToTrip(photoTrip.tripId)}
                            onkeydown={(e) => {
                                if (e.key === "Enter" || e.key === " ") {
                                    goToTrip(photoTrip.tripId);
                                }
                            }}
                        >
                            <span class="header-icon bi-journal-richtext" />
                            <h3>{photoTrip.tripTitle}</h3>
                        </div>
                        <div class="photo-trip-body">
                            <ImageDisplay images={photoTrip.photos} photoIds={photoTrip.photoIds} viewContext="search"/>
                        </div>
                    </div>
                {/each}
        {:else}
            <span>No images found matching your search.</span>
        {/if}
    </div>

    {#if trips.length > 0}
        <h2>Other Trips</h2>
        <div class="trip-object-wrapper">
            {#each trips as trip}
                <TripCard tripObject={trip} />
            {/each}
        </div>
    {/if}
</div>

<style>
    .trip-object-wrapper {
        padding: 3px 8px 10px 0;
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        align-items: stretch;
        gap: 1.25rem;
    }

    .photo-trip-list {
        display: flex;
        flex-direction: column;
        gap: 10px;
        margin-bottom: 10px;
    }

    .photo-trip {
        border: 2px solid #686868;
        border-radius: 0.35rem;
        background: #ffffff;
        box-shadow: 7px 7px 0 #686868;
    }
    
    .photo-trip-header {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 1.25rem 1.5rem;
        border-bottom: 2px solid #686868;
        background: #83beda;
        cursor: pointer;
    }

    .photo-trip-header:hover {
        filter: brightness(0.95);
    }

    .photo-trip-header h3 {
        margin: 0;
        color: #686868;
        font-size: 1.25rem;
        font-weight: 700;
        letter-spacing: 0.02em;
    }

    .header-icon {
        color: #686868;
        font-size: 1.2rem;
    }

    .photo-trip-body {
        padding: 1.25rem;
    }

    @media (max-width: 992px) {
        .trip-object-wrapper {
            grid-template-columns: repeat(2, minmax(0, 1fr));
        }
    }

    @media (max-width: 576px) {
        .trip-object-wrapper {
            grid-template-columns: 1fr;
        }
    }
</style>
