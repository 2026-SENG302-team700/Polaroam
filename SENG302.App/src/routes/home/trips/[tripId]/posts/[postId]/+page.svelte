<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { resolve } from "$app/paths";
    import DeleteButton from "$lib/components/buttons/delete-button.svelte";
    import { goto } from "$app/navigation";
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import EditButton from "$lib/components/buttons/edit-button.svelte";
    import PostDisplayCard from "$lib/components/trip-components/post-display-card.svelte";
    import NotFound from "$lib/components/content/not-found.svelte";
    import { user } from "$lib/stores/user.js";

    let loading = $state(false);
    let error = $state("");
    let deletionModalShown = $state(false);
    let postNotFound = $state(false);

    let { params } = $props();
    let postId = params.postId;

    let postObject: Post = $state({
        postId: 0,
        tripId: 0,
        title: "No Title",
        description: "No Description",
        content: "Content",
        date: "9999-12-31",
        photoIds: [],
        osmId: 0,
        osmType: "",
        creatorId: 0,
        creatorDisplayName: "Creator Name",
        locationName: "No Location",
        creationTime: "9999-12-31",
    });

    let tripObject: Trip = $state({
        id: -1,
        title: "No Title",
        description: "No Description",
        startDate: "9998-12-31",
        endDate: "9999-12-31",
        userId: 0,
        ownerName: "No Owner",
        collaborators: [],
    });

    let photos: File[] = $state([]);
    let photosData: TripIdAndPhoto[] = $state([]);

    onMount(() => {
        fetchPost();
    });

    /**
     * Fetch Images
     */
    async function fetchImages() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/post/${postId}/images/trip/${params.tripId}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            if (!response.ok) {
                error = (await response.text()) || "Failed to fetch images.";
                return;
            }

            const metadata: {
                photoId: number;
                fileName: string;
                mimeType: string;
            }[] = await response.json();

            photos = await Promise.all(
                metadata.map(async (meta) => {
                    const res = await fetchWithCsrf(
                        resolve(
                            `/api/trips/${postObject.tripId}/images/${meta.photoId}` as any,
                        ),
                        {
                            method: "GET",
                            credentials: "include",
                        },
                    );
                    const blob = await res.blob();
                    return new File([blob], meta.fileName, {
                        type: meta.mimeType,
                    });
                }),
            );
            photosData = metadata.map((meta) => {
                return {
                    photoId: meta.photoId,
                    tripId: postObject.tripId,
                } as TripIdAndPhoto;
            });
        } catch (err) {
            error = "Failed to fetch images: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    /**
     * Fetches the post from the backend corresponding
     * with the slug value being the post's id
     */
    async function fetchPost() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/post/${postId}/trip/${params.tripId}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );

            if (response.status == 404) {
                postNotFound = true;
                return;
            }

            const data = await response.json();

            if (!response.ok) {
                error = data.message || "Failed to fetch post.";
                return;
            }
            postObject = data;
            await fetchImages();
            await fetchTrip();
        } catch (err) {
            error = "Failed to fetch post: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    /**
     * Deletes the post from the backend corresponding
     * with the slug value being the post's id
     */
    async function deletePost() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/post/${params.postId}` as any),
                {
                    method: "DELETE",
                    credentials: "include",
                },
            );

            const data = await response.text();

            if (data === "") {
                goto(resolve(`/home/trips/${postObject.tripId}`));
            } else {
                error = data || "Failed to delete post.";
            }
        } catch (err) {
            error = "Failed to delete post: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    /**
     * Fetches the trip from the backend corresponding
     * with the slug value being the trip's id
     */
    async function fetchTrip() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${postObject.tripId}` as any),
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
            error = "Failed to fetch trip";
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

{#if postNotFound}
    <NotFound />
{:else}
    <div
        class="container trip-page-content d-flex justify-content-center align-items-center"
    >
        <div class="w-100" style="max-width: 900px;">
            <NavigationButton
                path={`/home/trips/${params.tripId}`}
                text="Back"
            />
            <EditButton
                path={`/home/trips/${params.tripId}/posts/${params.postId}/edit`}
            />
            {#if postObject.creatorId == $user.id || tripObject.userId == $user.id}
                <DeleteButton deleteAction={showConfirmationModal} />
            {/if}
            <ErrorDisplay errors={[error]} />

            <PostDisplayCard post={postObject} {photos} {photosData} />
        </div>
    </div>
{/if}
<ConfirmationModal
    bind:show={deletionModalShown}
    message="Are you sure you want to delete this post? This action cannot be undone."
    option1Text="Do not delete"
    option2Text="Delete"
    option2Style="danger"
    onOption1={hideConfirmationModal}
    onOption2={deletePost}
/>
