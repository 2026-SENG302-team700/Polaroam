<script lang="ts">
    import { resolve } from "$app/paths";
    import AddButton from "$lib/components/buttons/add-button.svelte";
    import SearchForm from "$lib/components/forms/search-form.svelte";
    import TripCard from "$lib/components/trip-components/trip-card.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { onMount } from "svelte";

    let loading = $state(false);
    let error = $state("");
    let trips = $state([]);

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
        console.log(trips);
    }
</script>

<div class="container">
    <div style="display: flex; flex-direction: row; ">
        <h1 class="text-center mb-4" style="flex: 1; justify-content: center">
            Your Trips
        </h1>
    </div>

    <div class="card-body d-flex justify-content-between align-items-center">
        <AddButton path="/home/trips/create-trip" />
        <div>
            <SearchForm />
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
