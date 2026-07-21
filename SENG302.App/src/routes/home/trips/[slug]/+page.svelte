<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { resolve } from "$app/paths";
    import { formatDate } from "$lib/datepicker/formatDate.js";
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import { goto } from "$app/navigation";
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";


    let loading = $state(false);
    let error = $state("");
    let { params } = $props();

    let tripObject = $state({
        id: 0,
        title: "No Title",
        description: "No Description",
        startDate: "9998-12-31",
        endDate: "9999-12-31",
        userId: 0,
    });

    onMount(() => {
        fetchTrip();
    });

    /**
     * Fetches the trip from the backend corresponding 
     * with the slug value being the trip's id
     */
    async function fetchTrip() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${params.slug}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            const data = await response.json();

            if (!response.ok) {
                error = data.message || "Failed to fetch trip.";
                return;
            }
            tripObject = data;
        } catch (err) {
            error = "Failed to fetch trip: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    /**
     * Deletes the trip from the backend corresponding 
     * with the slug value being the trip's id
     */
    async function deleteTrip() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${params.slug}` as any),
                {
                    method: "DELETE",
                    credentials: "include",
                },
            );

            const data = await response.text();

            if (data === '') {
                goto(resolve("/home/trips"));
            } else {
                error = data || "Failed to delete trip.";
            }
        } catch (err) {
            error = "Failed to delete trip: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

</script>

<div class="container trip-page-content">
    <NavigationButton path="/home/trips" text="Back" />
    <DeleteButton deleteAction={deleteTrip}/>
    <ErrorDisplay errors={[error]} />
    <div class="trip-page-header">
        <div class="trip-head-grid">
             <h1>{tripObject.title}</h1>
            <p class="date-object">
                {formatDate(tripObject.startDate)} - {formatDate(tripObject.endDate)}
            </p>
        </div>
        {#if tripObject.description.length > 0}
            <span>{tripObject.description}</span>
        {:else} 
            <p>No description</p>
        {/if}
        
    </div>
    
</div>

<style>
    .trip-head-grid { 
        align-items:end;
        display: flex;
    }
    .date-object {
        margin-left: 10px;
        bottom: 0;
    }

</style>