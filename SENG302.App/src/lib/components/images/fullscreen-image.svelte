<script lang="ts">
    import CloseButton from "../buttons/close-button.svelte";
    import RightArrowButton from "../buttons/right-arrow-button.svelte";
    import LeftArrowButton from "../buttons/left-arrow-button.svelte";
    import TagPill from "../content/tag-pill.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";
    import { goto } from "$app/navigation";
    import { queuetooltip } from "$lib/ToolTips/QueueFullToolTip";

    type ImageSource = File | string;
    type ImageViewContext = "search" | "album" | "post" | "none";
    type ImageContext = {
        tripId: number;
        tripTitle: string;
        postId?: number;
        postTitle?: string;
    };

    let {
        image,
        enlarged = $bindable(false),
        index = $bindable(0),
        images,
        photoId,
        photoIds,
        viewContext,
    }: {
        image: ImageSource;
        enlarged: boolean;
        index: number;
        images: ImageSource[] | null;
        photoId?: number;
        photoIds?: number[];
        viewContext: ImageViewContext;
    } = $props();

    let tags: string[] = $state([]);
    let error = $state("");
    let loading = $state(false);
    let showRetryTagging = $state(false);
    let aiTaggingPreference: string | null = $state(null);
    let tagStatus = $state("");
    let tagColour = $state("secondary");
    let imageContext = $state<ImageContext | null>(null);
    let objectUrl = $state<string | null>(null);

    let currentPhotoId = $derived(photoIds?.[index] ?? photoId);
    let displaySrc = $derived(image instanceof File ? objectUrl : image);
    let showTripAlbumButton = $derived(
        imageContext !== null &&
            viewContext !== "album" &&
            viewContext !== "none",
    );
    let showPostButton = $derived(
        imageContext?.postId != null &&
            viewContext !== "post" &&
            viewContext !== "none",
    );

    $effect(() => {
        if (image instanceof File) {
            const url = URL.createObjectURL(image);
            objectUrl = url;
            return () => URL.revokeObjectURL(url);
        }
        objectUrl = null;
    });

    $effect(() => {
        const id = currentPhotoId;

        resetMessages();
        tags = [];
        showRetryTagging = false;

        if (id === undefined || viewContext === "none") {
            imageContext = null;
            return;
        }

        fetchImageContext(id);
        loadImageTags(id);
    });

    function resetMessages() {
        error = "";
        tagStatus = "";
        tagColour = "secondary";
    }

    async function loadImageTags(id: number) {
        await getUserPreferences();
        if (aiTaggingPreference === "true") {
            await fetchImageTags(id);
        }
    }

    async function getUserPreferences() {
        aiTaggingPreference = sessionStorage.getItem("aiTaggingPreference");
        if (aiTaggingPreference !== null) return;

        try {
            loading = true;
            const response = await fetchWithCsrf(resolve("/api/user" as any), {
                method: "GET",
                credentials: "include",
            });
            const data = await response.json();
            if (!response.ok) {
                error = data.error;
                return;
            }

            aiTaggingPreference = String(data.aiTagging);
            sessionStorage.setItem("aiTaggingPreference", aiTaggingPreference);
        } catch {
            console.log("Cannot get user AI tagging preferences");
        } finally {
            loading = false;
        }
    }

    async function fetchImageContext(id: number) {
        imageContext = null;
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/image/${id}/context` as any),
                { method: "GET", credentials: "include" },
            );
            if (!response.ok) {
                error =
                    (await response.text()) || "Failed to fetch image context.";
                return;
            }
            imageContext = await response.json();
        } catch (err) {
            error = "Failed to fetch image context: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    async function deleteImageTag(id: number, tag: string) {
        try {
            const response = await fetchWithCsrf(
                resolve(
                    `/api/image/${id}/tags/${encodeURIComponent(tag)}` as any,
                ),
                { method: "DELETE", credentials: "include" },
            );

            if (!response.ok) {
                if (response.status === 404) return "Tag not found";
                return (await response.text()) || "Failed to remove tag.";
            }

            tags = tags.filter((existingTag) => existingTag !== tag);
        } catch (err) {
            return "Failed to remove tag: " + (err as Error).message;
        }

        return undefined;
    }

    function moveToLeftImage() {
        if (images === null || index === 0) return;
        const newImage = images[index - 1];
        if (newImage !== undefined) {
            index -= 1;
            image = newImage;
        }
    }

    function moveToRightImage() {
        if (images === null || index === images.length - 1) return;
        const newImage = images[index + 1];
        if (newImage !== undefined) {
            index += 1;
            image = newImage;
        }
    }

    async function fetchImageTags(id: number) {
        try {
            loading = true;
            tags = [];
            const response = await fetchWithCsrf(
                resolve(`/api/image/${id}/tags` as any),
                { method: "GET", credentials: "include" },
            );

            if (!response.ok) {
                if (response.status === 500) {
                    const data = await response.json();
                    if (data?.detail === "Tagging failed: Queue was full") {
                        error = data.detail;
                        showRetryTagging = true;
                        return;
                    }
                    if (
                        data?.detail ===
                        "Tags not available. AI tagging was disabled when image was uploaded"
                    ) {
                        error = data.detail;
                        return;
                    }
                    error = "AI failed to generate tags";
                    return;
                }
                error = "No tags available";
                return;
            }

            const message = await response.text();
            if (message === "Tags are being generated.") {
                tagStatus = "Tags are being generated. Come back later";
                tagColour = "info";
                return;
            }
            if (message.length === 0) {
                error = "No tags available";
                return;
            }
            tags = JSON.parse(message);
        } catch (err) {
            tagStatus = "Failed to fetch tags: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    async function retryImageTagging(id: number) {
        loading = true;
        error = "";
        try {
            tags = [];
            const response = await fetchWithCsrf(
                resolve(`/api/image/${id}/tags` as any),
                { method: "PATCH", credentials: "include" },
            );
            if (!response.ok) {
                const data = await response.json();
                error = "Failed to fetch tags: " + data.errors;
                return;
            }
            tagStatus = "Tags are being generated. Come back later";
            tagColour = "info";
        } catch (err) {
            error = "Failed to retry tagging: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
</script>

<svelte:window
    onkeydown={(event) => {
        if (event.key === "ArrowLeft") moveToLeftImage();
        else if (event.key === "ArrowRight") moveToRightImage();
        else if (event.key === "Escape") enlarged = false;
    }}
/>

<div class="fullscreen-image-wrapper">
    <div class="image-container">
        <div class="close-button-wrapper">
            {#if displaySrc}
                <img src={displaySrc} alt="Image" class="image" />
            {/if}

            <div class="image-toolbar">
                {#if imageContext}
                    <div class="image-details">
                        <div class="image-detail btn btn-secondary btn-sm">
                            <span
                                ><strong>Trip:</strong>
                                {imageContext.tripTitle}</span
                            >
                            {#if imageContext.postId}
                                <span class="detail-divider"
                                    ><strong>Post:</strong>
                                    {imageContext.postTitle}</span
                                >
                            {/if}
                        </div>
                    </div>
                {/if}

                <div class="action-buttons">
                    {#if imageContext?.postId && showPostButton}
                        <button
                            class="btn btn-secondary btn-sm"
                            onclick={() =>
                                goto(
                                    resolve(
                                        `/home/trips/${imageContext!.tripId}/posts/${imageContext!.postId}`,
                                    ),
                                )}>Go to Post</button
                        >
                    {/if}
                    {#if imageContext && showTripAlbumButton}
                        <button
                            class="btn btn-secondary btn-sm"
                            onclick={() =>
                                goto(
                                    resolve(
                                        `/home/trips/${imageContext!.tripId}/album`,
                                    ),
                                )}>Go to Trip Album</button
                        >
                    {/if}
                    <CloseButton bind:close={enlarged} class="btn-sm" />
                </div>
            </div>

            {#if images !== null && images.length > 1 && index !== images.length - 1}
                <RightArrowButton
                    class="position-absolute"
                    bind:image
                    bind:index
                    bind:images
                    {moveToRightImage}
                />
            {/if}
            {#if images !== null && images.length > 1 && index !== 0}
                <LeftArrowButton
                    class="position-absolute"
                    bind:image
                    bind:index
                    bind:images
                    {moveToLeftImage}
                />
            {/if}

            {#if aiTaggingPreference === "true"}
                {#if error.length > 0}
                    <div class="error-wrapper">
                        <span
                            class="bi bi-exclamation-triangle-fill text-warning fs-2"
                            use:queuetooltip={error}
                        ></span>
                    </div>
                {/if}
                <div class="tags-wrapper">
                    {#if tags.length > 0}
                        {#each tags as tag}
                            <TagPill
                                {tag}
                                isRealTag
                                deleteFunc={() =>
                                    currentPhotoId === undefined
                                        ? "Photo not found"
                                        : deleteImageTag(currentPhotoId, tag)}
                            />
                        {/each}
                    {:else if tagStatus}
                        <TagPill
                            {tagColour}
                            tag={tagStatus}
                            isRealTag={false}
                        />
                    {/if}
                </div>
                {#if showRetryTagging && currentPhotoId !== undefined}
                    <div class="tags-wrapper">
                        <button
                            class="btn btn-primary"
                            onclick={() => {
                                showRetryTagging = false;
                                retryImageTagging(currentPhotoId);
                            }}>Retry Tagging</button
                        >
                    </div>
                {/if}
            {/if}
        </div>
    </div>
</div>

<style>
    .image-details {
        display: flex;
        align-items: center;
        gap: 0.4rem;
    }
    .image-detail {
        display: inline-flex;
        flex-direction: row;
        align-items: center;
        gap: 0.6rem;
        height: calc(1.5em + 0.5rem + 2px);
        box-sizing: border-box;
        border-radius: 0.65rem;
        border-width: 0;
        cursor: default;
        pointer-events: none;
    }
    .image-detail span {
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .detail-divider {
        width: auto;
        padding-top: 0;
        padding-left: 0.6rem;
        border-top: 0;
        border-left: 1px solid rgba(255, 255, 255, 0.45);
    }
    .action-buttons {
        display: flex;
        gap: 0.4rem;
        margin-left: auto;
    }
    .image-toolbar {
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        z-index: 2;
        display: flex;
        flex-wrap: wrap;
        align-items: flex-start;
        gap: 0.4rem;
        padding: 5px;
    }
    .fullscreen-image-wrapper {
        z-index: 100;
        background-color: rgba(0, 0, 0, 0.7);
        position: fixed;
        top: 0;
        left: 0;
        height: 100vh;
        width: 100vw;
    }
    .image-container {
        position: relative;
        width: 100%;
        height: 100%;
        display: flex;
        justify-content: center;
        align-items: center;
    }
    .image {
        border-radius: 5px;
        position: relative;
        min-width: 20vw;
        max-height: 70vh;
        max-width: 70vw;
    }
    .close-button-wrapper {
        position: relative;
    }
    .tags-wrapper {
        position: relative;
        display: flex;
        flex-wrap: wrap;
        gap: 0.375rem;
        justify-content: center;
        margin-top: 6px;
    }
    .error-wrapper {
        position: relative;
        display: flex;
        justify-content: center;
        margin-bottom: 10px;
    }

</style>
