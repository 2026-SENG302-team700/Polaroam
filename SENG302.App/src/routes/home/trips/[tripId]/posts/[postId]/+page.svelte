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

    let loading = $state(false);
    let error = $state("");
    let deletionModalShown = $state(false);

    let { params } = $props();
    let postId = params.postId;

    let displayName = $state("");

    let postObject = $state({
        postId: 0,
        tripId: 0,
        title: "No Title",
        description: "No Description",
        content: "Content",
        date: "9999-12-31",
        photoIds: [],
        osmId: 0,
        locationName: "No Location",
        creationTime: "9999-12-31",
    });

    let photos: File[] = $state([]);
    let photoIds: number[] = $state([]);

    onMount(() => {
        fetchPost();
        fetchDisplayName();
    });

    // /**
    //  * Fetches the location name from the backend for an osmId
    //  */
    // async function fetchLocationName() {

    //     try {
    //         loading = true;
    //         const response = await fetchWithCsrf(
    //             resolve(`/api/geo/location?osmId=${postObject.osmId}` as any),
    //             {
    //                 method: "GET",
    //                 credentials: "include",
    //             },
    //         );
    //         let location = await response.json();
    //         locationName = location.name;
    //     } catch (err) {
    //         error = "Failed to fetch location name: " + (err as Error).message;
    //     } finally {
    //         loading = false;
    //     }
    // }

    /**
     * Fetch the display name of the user based on the post
     */
    async function fetchDisplayName() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/post/${postId}/user` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            displayName = await response.text();
        } catch (err) {
            error = "Failed to fetch post creator: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
    /**
     * Fetch Images
     */
    async function fetchImages() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/post/${postId}/images` as any),
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
                            `/api/post/${postId}/images/${meta.photoId}` as any,
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
            photoIds = metadata.map((meta) => meta.photoId);
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
                resolve(`/api/post/${postId}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            const data = await response.json();

            if (!response.ok) {
                error = data.message || "Failed to fetch post.";
                return;
            }
            postObject = data;
            await fetchImages();
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

    function showConfirmationModal() {
        deletionModalShown = true;
    }

    function hideConfirmationModal() {
        deletionModalShown = false;
    }
</script>

<div
    class="container trip-page-content d-flex justify-content-center align-items-center"
>
    <div class="w-100" style="max-width: 900px;">
        <NavigationButton path={`/home/trips/${params.tripId}`} text="Back" />
        <EditButton
            path={`/home/trips/${params.tripId}/posts/${params.postId}/edit`}
        />
        <DeleteButton deleteAction={showConfirmationModal} />
        <ErrorDisplay errors={[error]} />
        <PostDisplayCard post={postObject} userName={displayName} {photos} {photoIds}/>
    </div>
</div>

<ConfirmationModal
    bind:show={deletionModalShown}
    message="Are you sure you want to delete this post? This action cannot be undone."
    option1Text="Delete"
    option2Text="Do not delete"
    onOption1={deletePost}
    onOption2={hideConfirmationModal}
/>

<style>
    .post-head-grid {
        display: flex;
        flex-direction: column;
    }
    .date-object {
        margin-left: 10px;
        bottom: 0;
    }

    .photo-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 600px));
        gap: 0.75rem;
    }
</style>
