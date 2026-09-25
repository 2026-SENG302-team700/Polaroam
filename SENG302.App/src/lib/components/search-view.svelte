<script lang="ts">
    import { resolve } from "$app/paths";
    import { goto } from "$app/navigation";
    import TripCard from "$lib/components/trip-components/trip-card.svelte";
    import ImageDisplay from "$lib/components/images/image-display.svelte";

    let {
        trips,
        photosPerTrip,
        query = "",
    }: {
        trips: Trip[];
        photosPerTrip: TripPhotoGroup[];
        query: string;
    } = $props();

    let imageCount = $derived(
        photosPerTrip.reduce(
            (sum, photoTrip) => sum + (photoTrip.photos?.length ?? 0),
            0,
        ),
    );
    let resultCount = $derived(imageCount + trips.length);

    /**
     * Navigates to the given trip's page
     */
    function goToTrip(tripId: number) {
        goto(resolve(`/home/trips/${tripId}`));
    }
</script>

<div class="search-results">
    <div class="results-header">
        <div>
            <span class="results-label">Search results</span>
            <h2>Results for “{query}”</h2>
        </div>
        <span class="result-count">
            {resultCount} result{resultCount === 1 ? "" : "s"}
        </span>
    </div>

    {#if photosPerTrip.length > 0}
        <section class="result-section" aria-labelledby="matching-images">
            <div class="section-heading">
                <h3 id="matching-images">
                    <i class="bi bi-images"></i> Matching images
                </h3>
                <span>{imageCount}</span>
            </div>
            <div class="photo-trip-list">
                {#each photosPerTrip as tripPhotos}
                    <div class="photo-trip">
                        <div
                            tabindex="0"
                            role="button"
                            class="photo-trip-header"
                            onclick={() => goToTrip(tripPhotos.tripId)}
                            onkeydown={(e) => {
                                if (e.key === "Enter" || e.key === " ") {
                                    goToTrip(tripPhotos.tripId);
                                }
                            }}
                        >
                            <span class="header-icon bi-journal-richtext" />
                            <h3>{tripPhotos.tripTitle}</h3>
                        </div>
                        <div class="photo-trip-body">
                            <ImageDisplay
                                images={tripPhotos.photos}
                                photosData={tripPhotos.photoIds.map((id) => {
                                    return {
                                        photoId: id,
                                        tripId: tripPhotos.tripId,
                                    };
                                })}
                                viewContext="search"
                            />
                        </div>
                    </div>
                {/each}
            </div>
        </section>
    {/if}

    {#if trips.length > 0}
        <section class="result-section" aria-labelledby="matching-trips">
            <div class="section-heading">
                <h3 id="matching-trips">
                    <i class="bi bi-journal-richtext"></i> Matching trips
                </h3>
                <span>{trips.length}</span>
            </div>
            <div class="trip-object-wrapper">
                {#each trips as trip}
                    <TripCard tripObject={trip} />
                {/each}
            </div>
        </section>
    {/if}

    {#if resultCount === 0}
        <div class="empty-results">
            <i class="bi bi-search" aria-hidden="true"></i>
            <h3>No results found</h3>
            <p>Try searching with a different trip name or image tag.</p>
        </div>
    {/if}
</div>

<style>
    /*Some of styling below was created with the use of ChatGPT for better styling*/
    .search-results {
        display: flex;
        flex-direction: column;
        gap: 1.75rem;
        padding: 0.5rem 0 1rem;
    }

    .results-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        padding-bottom: 1rem;
        border-bottom: 2px solid black;
    }

    .results-label {
        display: block;
        margin-bottom: 0.2rem;
        color: #686868;
        font-size: 0.8rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        text-transform: uppercase;
    }

    .results-header h2 {
        margin: 0;
        color: #3f3f3f;
        font-size: clamp(1.35rem, 3vw, 1.8rem);
        font-weight: 800;
    }

    .result-count,
    .section-heading span {
        flex-shrink: 0;
        border: 2px solid black;
        border-radius: 999px;
        background: #efe7d6;
        color: black;
        font-size: 0.8rem;
        font-weight: 800;
    }

    .result-count {
        padding: 0.35rem 0.75rem;
    }

    .result-section {
        display: flex;
        flex-direction: column;
        gap: 0.85rem;
    }

    .section-heading {
        display: flex;
        align-items: center;
        justify-content: space-between;
    }

    .section-heading h3 {
        display: flex;
        align-items: center;
        gap: 0.55rem;
        margin: 0;
        color: #3f3f3f;
        font-size: 1.2rem;
        font-weight: 800;
    }

    .section-heading span {
        min-width: 2rem;
        padding: 0.2rem 0.55rem;
        text-align: center;
    }

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
        gap: 1rem;
        padding: 0 8px 8px 0;
    }

    .photo-trip {
        overflow: hidden;
        border: 2px solid black;
        border-radius: 0.55rem;
        background: #fff8e8;
        box-shadow: 6px 6px 0 black;
    }

    .photo-trip-header {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 1.25rem 1.5rem;
        border-bottom: 2px solid black;
        background: #efe7d6;
        cursor: pointer;
    }

    .photo-trip-header:hover {
        filter: brightness(0.95);
    }

    .photo-trip-header h3 {
        margin: 0;
        color: #3f3f3f;
        font-size: 1.25rem;
        font-weight: 700;
        letter-spacing: 0.02em;
    }

    .header-icon {
        color: #3f3f3f;
        font-size: 1.2rem;
    }

    .photo-trip-body {
        padding: 1.25rem;
    }

    .empty-results {
        display: grid;
        place-items: center;
        min-height: 14rem;
        padding: 2rem;
        border: 2px dashed #686868;
        border-radius: 0.55rem;
        background: #fff8e8;
        text-align: center;
    }

    .empty-results i {
        margin-bottom: 0.6rem;
        font-size: 2.25rem;
    }

    .empty-results h3,
    .empty-results p {
        margin: 0;
    }

    .empty-results p {
        margin-top: 0.35rem;
        color: #686868;
    }

    @media (max-width: 992px) {
        .trip-object-wrapper {
            grid-template-columns: repeat(2, minmax(0, 1fr));
        }
    }

    @media (max-width: 576px) {
        .results-header {
            align-items: flex-start;
            flex-direction: column;
        }

        .trip-object-wrapper {
            grid-template-columns: 1fr;
        }
    }
</style>
