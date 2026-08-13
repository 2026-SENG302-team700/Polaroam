<script lang="ts">
    import { resolve } from "$app/paths";
    import AddButton from "$lib/components/buttons/add-button.svelte";
    import TagSearchForm from "$lib/components/forms/tag-search-form.svelte";
    import TripCard from "$lib/components/trip-components/trip-card.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { onMount } from "svelte";

    let loading = $state(false);
    let error = $state("");
    let trips = $state([]);
    
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
                error = "Failed to fetch task lists.";
                return;
            }
            trips = data;
        } catch (err) {
            error = "Failed to fetch task lists: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    /**
     * Fetches tag suggestions matching the given query for the current user's trips
     * @param query - the partial tag text to search for
     */
    async function fetchTagSuggestions(query: string) {
        const response = await fetchWithCsrf(
            resolve(`/api/image/tags?query=${encodeURIComponent(query)}` as any),
            {
                method: "GET",
                credentials: "include",
            },
        );
        
        if (!response.ok) {
            throw new Error("Failed to fetch tag suggestions.");
        }

        const data = await response.json();
        return data
    }
</script>

<div class="container">
    <div style="display: flex; flex-direction: row; ">
        <h1 class="text-center mb-4" style="flex: 1; justify-content: center">
            My Trips
        </h1>
    </div>

    <div class="card-body d-flex justify-content-between align-items-center">
        <AddButton path="/home/trips/create-trip" />
        <div>
            <TagSearchForm bind:query={tagQuery} getSuggestions={fetchTagSuggestions} />
        </div>
    </div>

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
</style>
