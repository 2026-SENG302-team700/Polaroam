<script lang="ts">
    import { MapLibre, Marker, Popup } from "svelte-maplibre";
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
    import AlbumButton from "$lib/components/buttons/album-button.svelte";
    import NotFound from "$lib/components/content/not-found.svelte";

    let loading = $state(false);
    let error = $state("");
    let deletionModalShown = $state(false);
    let { params } = $props();
    let tripId = params.tripId;
    let hoveredPostId: number | null = $state(null);

    let tripObject = $state({
        id: -1,
        title: "No Title",
        description: "No Description",
        startDate: "9998-12-31",
        endDate: "9999-12-31",
        userId: 0,
    });

    let postLocations = $state([]);
    let posts = $state([]);
    let selectedPost = $state(null);

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
            
            // Check response
            if (!response.ok) {
                error = "Failed to fetch trip.";
                return;
            }
            
            // Parse response 
            const data = await response.json();
            tripObject = data;
            
        } catch (err) {
            error = "Failed to fetch trip"
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

{#if !loading && tripObject.id !== -1}
    <div class="container trip-page-content">
        <div class="button-wrapper">
            <NavigationButton path="/home" text="Back" />
            <div class="modify-button-wrapper">
                <AlbumButton tripid={params.tripId} />
                <EditButton path={`/home/trips/${params.tripId}/edit`} />
                <DeleteButton deleteAction={showConfirmationModal} />
            </div>
        </div>
        <div class="card mb-2 post-wrapper">
            <div class="card-header">
                <div
                    class="title-and-date"
                    style="display: flex; flex-direction: row;"
                >
                    <h1 class="card-subtitle mb-2 h1 user-name">
                        {tripObject.title}
                    </h1>
                    <div
                        class="header-content-item"
                        style="display: flex; align-items: center; flex:1; justify-content: end;"
                    >
                        <div class="bi-calendar-event"></div>
                        <h6
                            class="card-subtitle text-muted"
                            style="margin-left: 5px;"
                        >
                            {formatDate(tripObject.startDate)} - {formatDate(
                                tripObject.endDate,
                            )}
                        </h6>
                    </div>
                </div>
                {#if tripObject.description.length > 0}
                    <span>{tripObject.description}</span>
                {:else}
                    <p>No description</p>
                {/if}
                <ErrorDisplay errors={[error]} />
            </div>
            <div class="card-body">
                <Map
                    locations={postLocations}
                    {posts}
                    {hoveredPostId}
                    bind:selectedPost
                />
            </div>
            <div class="card-footer text-muted images">
                <PostSection
                    {posts}
                    tripId={tripObject.id}
                    onPostHover={(postId: number) => (hoveredPostId = postId)}
                    onPostLeave={() => (hoveredPostId = null)}
                    {selectedPost}
                />
            </div>
        </div>
    </div>
{:else if !loading}
    <NotFound />
{/if}

<ConfirmationModal
    bind:show={deletionModalShown}
    message="Are you sure you want to delete this trip? This action cannot be undone."
    option1Text="Delete"
    option2Text="Do not delete"
    onOption1={deleteTrip}
    onOption2={hideConfirmationModal}
/>

<style>
    .button-wrapper {
        display: flex;
        justify-content: space-between;
        margin-bottom: 5px;
    }
</style>
