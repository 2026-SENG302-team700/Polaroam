<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { resolve } from "$app/paths";
    import { formatDate } from "$lib/datepicker/formatDate.js";
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import { goto } from "$app/navigation";
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import CollaborationModal from "$lib/components/modals/collaboration-modal.svelte";
    import EditButton from "$lib/components/buttons/edit-button.svelte";
    import PostSection from "$lib/components/trip-components/post-section.svelte";
    import Map from "$lib/components/trip-components/map.svelte";
    import AlbumButton from "$lib/components/buttons/album-button.svelte";
    import { user } from "$lib/stores/user";

    let loading = $state(false);
    let error = $state("");
    let deletionModalShown = $state(false);
    let { params } = $props();
    let tripId = params.tripId;
    let ownerDisplayName: null | string = $state(null);
    let hoveredPostId: number | null = $state(null);
    let collaborationModalShown: boolean = $state(false);
    let collaboratingUsersError: string = $state("");

    let tripObject = $state({
        id: 0,
        title: "No Title",
        description: "No Description",
        startDate: "9998-12-31",
        endDate: "9999-12-31",
        userId: 0,
    });

    let collaboratingUsers: CollaboratingUser[] = $state([]);

    const LoadState = {
        UNLOADED: 0,
        LOADING: 1,
        LOADED: 2,
    };

    let collaboratingLoadingState: number = $state(LoadState.UNLOADED);

    let postLocations = $state([]);
    let posts = $state([]);
    let selectedPost = $state(null);

    onMount(() => {
        fetchTrip();
        fetchPosts();
        fetchLocations();
        fetchUserName();
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
                resolve(`/api/trip/locations/${tripId}` as any),
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
                resolve(`/api/trip/${tripId}` as any),
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

    async function fetchUserName() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${tripId}/user` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            ownerDisplayName = await response.text();
        } catch (err) {
            throw new Error(
                "Failed to fetch post creator: " + (err as Error).message,
            );
        }
    }

    /**
     * Turn on the collaboration modal
     * If the collaborators are not yet loaded then load them
     */
    function showCollaborationModal() {
        if (collaboratingLoadingState === LoadState.UNLOADED) {
            collaboratingLoadingState = LoadState.LOADING;
            fetchTripCollaborators()
                .then(() => {
                    collaboratingLoadingState = LoadState.LOADED;
                })
                .catch((e: Error) => {
                    collaboratingUsersError = e.message;
                });
        }

        collaborationModalShown = true;
    }

    /**
     * Fetch all collaborators of trip from backend
     */
    async function fetchTripCollaborators() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${tripId}/collaborators` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            collaboratingUsers = await response.json();
        } catch (err: any) {
            collaboratingUsersError = err.toString();
        }
    }
</script>

<div class="container trip-page-content">
    <div class="button-wrapper">
        <NavigationButton path="/home" text="Back" />
        <div class="modify-button-wrapper">
            <button
                class="btn btn-secondary bi-people-fill"
                onclick={() => {
                    showCollaborationModal();
                }}>Collaboration</button
            >
            <AlbumButton tripid={params.tripId} />
            <EditButton path={`/home/trips/${params.tripId}/edit`} />
            {#if tripObject.userId == $user.id}
                <DeleteButton deleteAction={showConfirmationModal} />
            {/if}
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

<ConfirmationModal
    bind:show={deletionModalShown}
    message="Are you sure you want to delete this trip? This action cannot be undone."
    option1Text="Delete"
    option2Text="Do not delete"
    onOption1={deleteTrip}
    onOption2={hideConfirmationModal}
/>

<CollaborationModal
    bind:show={collaborationModalShown}
    tripOwner={ownerDisplayName ?? ""}
    tripCollaborators={collaboratingUsers}
    {error}
/>

<style>
    .button-wrapper {
        display: flex;
        justify-content: space-between;
        margin-bottom: 5px;
    }
</style>
