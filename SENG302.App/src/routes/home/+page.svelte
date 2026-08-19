<script lang="ts">
    import {resolve} from "$app/paths";
    import AddButton from "$lib/components/buttons/add-button.svelte";
    import TagSearchForm from "$lib/components/forms/tag-search-form.svelte";
    import TripCard from "$lib/components/trip-components/trip-card.svelte";
    import {fetchWithCsrf} from "$lib/csrf";
    import {onMount} from "svelte";
    import SearchView from "$lib/components/search-view.svelte"

    let loading = $state(false);
    let trips = $state([]);
    let tripsError = $state("");
    let photosError = $state("");
    
    // search stuff
    let searchQuery = $state("");
    let inSearch = $state(false);
    
    let searchTrips = $state([]);
    let searchPhotoTrips: { 
        tripId: number; 
        tripTitle: string; 
        photos: File[]; 
        photoIds: number[] 
    }[] = $state([]);

    let tagQuery = $state("");

    onMount(() => {
        fetchTrips();
    });

    /**
     * Fetches all trips corresponding to the user's credentials stored in the browser
     */
    async function fetchTrips() {
        try {
            loading = true;
            const response = await fetchWithCsrf(resolve(`/api/trip` as any), {
                method: "GET",
                credentials: "include",
            });
            const data = await response.json();
            if (!response.ok) {
                tripsError = "Failed to fetch task lists.";
                return;
            }
            trips = data;
        } catch (err) {
            tripsError = "Failed to fetch task lists: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
    
    async function performSearch() {
        if (!tagQuery.trim()) {
            clearSearch();
            return;
        }

        searchTrips = [];
        searchPhotoTrips = [];

        inSearch = true;

        try {
            loading = true;
            const tripsResponse = await fetchWithCsrf(resolve(`/api/search/my-trips?query=${encodeURIComponent(tagQuery)}` as any), {
                method: "GET",
                credentials: "include",
            });

            const photosResponse = await fetchWithCsrf(resolve(`/api/search/photos?query=${encodeURIComponent(tagQuery)}` as any), {
                method: "GET",
                credentials: "include",
            });

            if (tripsResponse.ok) {
                searchTrips = await tripsResponse.json();
            } else {
                tripsError = "Failed to fetch search results.";
            }

            if (photosResponse.ok) {
                const groups: {
                    tripId: number;
                    tripTitle: string;
                    photos: { photoId: number; fileName: string; mimeType: string }[];
                }[] = await photosResponse.json();

                searchPhotoTrips = await Promise.all(
                    groups.map(async (group) => {
                        const photos = await Promise.all(
                            group.photos.map(async (meta) => {
                                const res = await fetchWithCsrf(
                                    resolve(`/api/image/${meta.photoId}` as any),
                                    {
                                        method: "GET",
                                        credentials: "include"
                                    }
                                );
                                const blob = await res.blob();
                                return new File([blob], meta.fileName, { type: meta.mimeType });
                            }),
                        );
                        return {
                            tripId: group.tripId,
                            tripTitle: group.tripTitle,
                            photos,
                            photoIds: group.photos.map((meta) => meta.photoId),
                        };
                    }),
                );
            } else {
                photosError = "Failed to fetch search results.";
            }

            const photoTripIds = new Set(searchPhotoTrips.map((pt) => pt.tripId));
            searchTrips = searchTrips.filter((trip) => !photoTripIds.has(trip.id));

        } catch (err) {
            tripsError = "Failed to fetch search results: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    /**
     * Clears the search and returns to my-trips page.
     */
    function clearSearch() {
        tagQuery = "";
        searchTrips = [];
        searchPhotoTrips = [];
        inSearch = false;
    }
</script>

<div class="container">
    <div style="display: flex; flex-direction: row; ">
        <h1 class="text-center mb-4" style="flex: 1; justify-content: center">
            My Trips
        </h1>
    </div>
    <div class="card">
        <div class="card-body d-flex justify-content-between align-items-center my-trips-top-bar">
            <AddButton path="/home/trips/create-trip" />
            <TagSearchForm bind:query={tagQuery} {performSearch} onClear={clearSearch} />
        </div>
        <div class="card-footer">
            {#if inSearch}
                <SearchView trips={searchTrips} photoTrips={searchPhotoTrips}/>
            {:else}
                {#if trips.length > 0}
                    <div class="trip-object-wrapper">
                        {#each trips as trip}
                            <TripCard tripObject={trip} />
                        {/each}
                    </div>
                {:else}
                    <div class="no-trips-message">
                        <p>You have no trips yet! Create one with the "+" button</p>
                    </div>
                {/if}
            {/if}
        </div>
    </div>
    
</div>

<style>
    .trip-object-wrapper {
        padding-top: 3px;
        display: grid;
        grid-template-columns: 33% 33% 33%;
        align-items: center;
    }
    .no-trips-message {
        width: 50vw;
        display: flex;
        flex-direction: row;
        position: absolute;
        align-items: center;
        text-align: center;
    }
    
    .my-trips-top-bar {
        margin-bottom: 10px;
    }
</style>
