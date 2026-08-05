<script lang="ts">
    import { MapLibre, Marker, Popup } from 'svelte-maplibre';
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { resolve } from "$app/paths";
    import { formatDate } from "$lib/datepicker/formatDate.js";
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import { goto } from "$app/navigation";
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import AddButton from "$lib/components/buttons/add-button.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import EditButton from "$lib/components/buttons/edit-button.svelte";
    import PostSection from "$lib/components/trip-components/post-section.svelte";
    import Map from "$lib/components/trip-components/map.svelte";

    let loading = $state(false);
    let error = $state("");
    let deletionModalShown = $state(false);
    let { params } = $props();
    let tripId = params.tripId;

    let tripObject = $state({
        id: 0,
        title: "No Title",
        description: "No Description",
        startDate: "9998-12-31",
        endDate: "9999-12-31",
        userId: 0,
    });
    
    let postLocations = $state([]);
    let posts = $state([]);

    onMount(() => {
        fetchTrip();
        fetchPosts();
        fetchLocations();
    });

    

    /**
     * Fetches the trip from the backend corresponding
     * with the slug value being the trip's id
     */
    async function fetchTrip() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${tripId}` as any),
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
     * Retrieves all posts associated with the trip
     * from the backend
     */
    async function fetchPosts() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/trip/post/${tripId}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            posts = await response.json();
        } catch (err) {
            error = "Failed to fetch posts: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    /**
     * Fetches all locations from the posts of the given trip
     */
    async function fetchLocations() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/trip/locations/${params.tripId}` as any),
                {
                    method: "Get",
                    credentials: "include",
                },
            );
            postLocations = await response.json();
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
                resolve(`/api/trip/${params.tripId}` as any),
                {
                    method: "DELETE",
                    credentials: "include",
                },
            );

            const data = await response.text();

            if (!response.ok) {
                error = data || "Failed to delete post.";
                return;
            }

            await goto(resolve(`/home` as any));
        } catch (err) {
            error = "Failed to delete trip: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    function showConfirmationModal() {
        deletionModalShown = true;
    }

    function hideConfirmationModal() {
        deletionModalShown = false;
    }
</script>

<div class="container trip-page-content">
    <NavigationButton path="/home" text="Back" />
    <AddButton path="/home/trips/{tripObject.id}/posts/create-post" />
    <EditButton path={`/home/trips/${params.tripId}/edit`} />
    <DeleteButton deleteAction={showConfirmationModal} />
    <ErrorDisplay errors={[error]} />
    <div class="trip-page-header">
        <div class="trip-head-grid">
            <h1>{tripObject.title}</h1>
            <p class="date-object">
                {formatDate(tripObject.startDate)} - {formatDate(
                    tripObject.endDate,
                )}
            </p>
        </div>
        {#if tripObject.description.length > 0}
            <span>{tripObject.description}</span>
        {:else}
            <p>No description</p>
        {/if}
    </div>
    <Map locations={postLocations} />
    <PostSection {posts} />
</div>

<ConfirmationModal
    bind:show={deletionModalShown}
    message="Are you sure you want to delete this post? This action cannot be undone."
    option1Text="Delete"
    option2Text="Do not delete"
    onOption1={deleteTrip}
    onOption2={hideConfirmationModal}
/>

<style>
    .trip-head-grid {
        align-items: end;
        display: flex;
    }
    .date-object {
        margin-left: 10px;
        bottom: 0;
    }

    .app {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        height: 100%;
    }

    .search-bar {
        display: flex;
        gap: 0.5rem;
    }

    .search-bar input {
        flex: 1;
        padding: 0.5rem 0.75rem;
        border: 1px solid #ccc;
        border-radius: 6px;
    }

    .search-bar button {
        padding: 0.5rem 1rem;
        border: none;
        border-radius: 6px;
        background: #2563eb;
        color: white;
        cursor: pointer;
    }

    .search-bar button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    .error {
        color: #dc2626;
        font-size: 0.9rem;
    }

</style>
