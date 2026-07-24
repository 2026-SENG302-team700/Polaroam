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
    import EditButton from "$lib/components/buttons/edit-button.svelte";

    let loading = $state(false);
    let error = $state("");
    let deletionModalShown = $state(false);

    let { params } = $props();
    let postId = params.postId;

    let postObject = $state({
        postId: 0,
        tripId: 0,
        title: "No Title",
        description: "No Description",
        content: "Content",
        date: "9999-12-31",
        photoIds: [],
        //osmId: 0,
        location: "No Location",
        creationTime: "9999-12-31",
        userId: 0,
    });

    onMount(() => {
        fetchPost();
    });

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

<div class="container trip-page-content">
    <NavigationButton path={`/home/trips/${params.tripId}`} text="Back" />
    <EditButton
        path={`/home/trips/${params.tripId}/posts/${params.postId}/edit`}
    />
    <DeleteButton deleteAction={showConfirmationModal} />
    <ErrorDisplay errors={[error]} />
    <div class="trip-page-header">
        <div class="trip-head-grid">
            <h1>{postObject.title}</h1>

            <p class="date-object">
                <i>
                    {postObject.location}
                </i>
            </p>

            <p class="date-object">
                {formatDate(postObject.date)}
            </p>
        </div>
        {#if postObject.content.length > 0}
            <p><i>Posted at {postObject.creationTime}</i></p>
            <span>{postObject.content}</span>
        {/if}
    </div>
</div>

<ConfirmationModal
    bind:show={deletionModalShown}
    message="Are you sure you want to delete this post?"
    option1Text="Delete"
    option2Text="Do not delete"
    onOption1={deletePost}
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
</style>
