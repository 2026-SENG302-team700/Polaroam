<script lang="ts">
    import CloseButton from "../buttons/close-button.svelte";
    import RightArrowButton from "../buttons/right-arrow-button.svelte";
    import LeftArrowButton from "../buttons/left-arrow-button.svelte";
    import TagPill from "../content/tag-pill.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";
    import { goto } from "$app/navigation";
    import { queuetooltip } from "$lib/ToolTips/QueueFullToolTip";
    import { onMount } from "svelte";

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
        photoData,
        photosData,
        viewContext,
    }: {
        image: ImageSource;
        enlarged: boolean;
        index: number;
        images: ImageSource[] | null;
        photoData?: TripIdAndPhoto;
        photosData?: TripIdAndPhoto[];
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
    let aspectRatio = $state(1);

    onMount(() => {
        const previousBodyOverflow = document.body.style.overflow;
        const previousHtmlOverflow = document.documentElement.style.overflow;

        document.body.style.overflow = "hidden";
        document.documentElement.style.overflow = "hidden";

        return () => {
            document.body.style.overflow = previousBodyOverflow;
            document.documentElement.style.overflow = previousHtmlOverflow;
        };
    });

    /**
     * Get's image aspect ratio
     */
    function handleImageLoad(e: Event) {
        const img = e.currentTarget as HTMLImageElement;
        if (img.naturalWidth && img.naturalHeight) {
            aspectRatio = img.naturalWidth / img.naturalHeight;
        }
    }

    const informationalTagMessages = [
        "Tags not available. AI tagging was disabled when image was uploaded",
        "No tags available",
    ];

    let isTagInformation = $derived(informationalTagMessages.includes(error));

    let currentPhotoData = $derived(photosData?.[index] ?? photoData);
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
        const data = currentPhotoData;

        resetMessages();
        tags = [];
        showRetryTagging = false;

        if (data === undefined || viewContext === "none") {
            imageContext = null;
            return;
        }

        fetchImageContext(data);
        loadImageTags(data);
    });

    function resetMessages() {
        error = "";
        tagStatus = "";
        tagColour = "secondary";
    }

    async function loadImageTags(data: TripIdAndPhoto) {
        await getUserPreferences();
        if (aiTaggingPreference === "true") {
            await fetchImageTags(data);
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
            console.error("Failed to get user AI tagging preferences");
        } finally {
            loading = false;
        }
    }

    async function fetchImageContext(data: TripIdAndPhoto) {
        imageContext = null;
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(
                    `/api/trips/${data.tripId}/images/${data.photoId}/context` as any,
                ),
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

    async function deleteImageTag(data: TripIdAndPhoto, tag: string) {
        try {
            const response = await fetchWithCsrf(
                resolve(
                    `/api/trips/${data.tripId}/images/${data.photoId}/tags/${encodeURIComponent(tag)}` as any,
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

    async function fetchImageTags(data: TripIdAndPhoto) {
        try {
            loading = true;
            tags = [];
            const response = await fetchWithCsrf(
                resolve(
                    `/api/trips/${data.tripId}/images/${data.photoId}/tags` as any,
                ),
                { method: "GET", credentials: "include" },
            );

            if (!response.ok) {
                const errorMessage = await response.text();
                if (errorMessage === "Tagging failed: Queue was full") {
                    error = errorMessage;
                    showRetryTagging = true;
                    return;
                }
                if (
                    errorMessage ===
                    "Tags not available. AI tagging was disabled when image was uploaded"
                ) {
                    error = errorMessage;
                    return;
                }
                error = "AI failed to generate tags";
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

    async function retryImageTagging(data: TripIdAndPhoto) {
        loading = true;
        error = "";
        try {
            tags = [];
            const response = await fetchWithCsrf(
                resolve(
                    `/api/trips/${data.tripId}/images/${data.photoId}/tags` as any,
                ),
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
    <a class="click-to-return" onclick={() => (enlarged = false)} />
    <div class="image-container">
        <div class="image-content">
            <div class="image-frame" style:--aspectRatio={aspectRatio}>
                {#if displaySrc}
                    <img
                        src={displaySrc}
                        alt="Image"
                        class="image"
                        onload={handleImageLoad}
                    />
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
                    </div>
                </div>

                <div class="close-control">
                    <CloseButton bind:close={enlarged} class="btn-sm" />
                </div>

                {#if images !== null && images.length > 1 && index !== images.length - 1}
                    <div class="arrow-control right-control">
                        <RightArrowButton
                            bind:image
                            bind:index
                            bind:images
                            {moveToRightImage}
                        />
                    </div>
                {/if}
                {#if images !== null && images.length > 1 && index !== 0}
                    <div class="arrow-control left-control">
                        <LeftArrowButton
                            bind:image
                            bind:index
                            bind:images
                            {moveToLeftImage}
                        />
                    </div>
                {/if}
            </div>
        </div>

        {#if aiTaggingPreference === "true"}
            {#if error.length > 0}
                <div class="error-wrapper">
                    <button
                        type="button"
                        class="tag-error-button bi fs-2"
                        class:bi-info-square-fill={isTagInformation}
                        class:text-white={isTagInformation}
                        class:bi-exclamation-triangle-fill={!isTagInformation}
                        class:text-warning={!isTagInformation}
                        use:queuetooltip={error}
                        aria-label={`Image tagging ${isTagInformation ? "information" : "error"}: ${error}`}
                    ></button>
                </div>
            {/if}
            <div class="tags-wrapper">
                {#if tags.length > 0}
                    {#each tags as tag}
                        <TagPill
                            {tag}
                            isRealTag
                            deleteFunc={() =>
                                currentPhotoData === undefined
                                    ? "Photo not found"
                                    : deleteImageTag(currentPhotoData, tag)}
                        />
                    {/each}
                {:else if tagStatus}
                    <TagPill {tagColour} tag={tagStatus} isRealTag={false} />
                {/if}
            </div>
            {#if showRetryTagging && currentPhotoData !== undefined}
                <div class="tags-wrapper">
                    <button
                        class="btn btn-primary"
                        onclick={() => {
                            showRetryTagging = false;
                            retryImageTagging(currentPhotoData);
                        }}>Retry Tagging</button
                    >
                </div>
            {/if}
        {/if}
    </div>
</div>

<style>
    .click-to-return {
        width: 100%;
        height: 100%;
        position: absolute;
        z-index: 101;
    }
    .image-details {
        display: flex;
        align-items: center;
        gap: 0.4rem;
        padding: 4px;
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
        padding: 3px;
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
        padding: 5px 3rem 5px 5px;
    }
    .fullscreen-image-wrapper {
        z-index: 100;
        background-color: rgba(0, 0, 0, 0.7);
        position: fixed;
        top: 0;
        left: 0;
        height: 100vh;
        width: 100vw;
        display: flex;
        justify-content: center;
        align-items: center;
    }
    .image-container {
        position: relative;
        display: flex;
        width: 90vw;
        height: 90vh;
        flex-direction: column;
        align-items: center;
        z-index: 102;
    }
    .image-content {
        position: relative;
        width: 100%;
        height: 85vh;
        flex-shrink: 0;
        display: flex;
        align-items: center;
        justify-content: center;
        container-type: size;
    }
    .image-frame {
        position: relative;
        width: min(100cqw, 100cqh * var(--aspectRatio, 1));
        height: min(100cqh, 100cqw / var(--aspectRatio, 1));
    }
    .image {
        display: block;
        border-radius: 5px;
        width: 100%;
        height: 100%;
    }
    .close-control {
        position: absolute;
        top: 0.5rem;
        right: 0.5rem;
        z-index: 3;
        line-height: normal;
    }
    .arrow-control {
        position: absolute;
        top: 50%;
        z-index: 3;
        line-height: normal;
        transform: translateY(-50%);
    }
    .left-control {
        left: 0.5rem;
    }
    .right-control {
        right: 0.5rem;
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
        align-items: center;
        margin-top: 10px;
        margin-bottom: 10px;
    }

    .tag-error-button {
        padding: 0;
        border: 0;
        background: transparent;
        line-height: 1;
        cursor: default;
    }
</style>
