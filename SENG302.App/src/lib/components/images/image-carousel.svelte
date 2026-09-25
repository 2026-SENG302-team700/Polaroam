<script lang="ts">
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";

    let { post } = $props();

    let index = $state(0);
    let photos: File[] = $state([]);
    let image = <File>$state();
    let imageUrl = $state("");
    onMount(() => {
        fetchImages();
    });

    function moveToLeftImage() {
        index -= 1;
        let newImage = photos[index];
        if (newImage !== undefined) {
            image = newImage;
            updateImageUrl();
        }
    }

    function moveToRightImage() {
        index += 1;
        let newImage = photos[index];
        if (newImage !== undefined) {
            image = newImage;
            updateImageUrl();
        }
    }

    function updateImageUrl() {
        if (imageUrl) {
            URL.revokeObjectURL(imageUrl); // Clean up old URL
        }
        imageUrl = image ? URL.createObjectURL(image) : "";
    }

    /**
     * Fetch Images
     */
    async function fetchImages() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/post/${post.postId}/images` as any),
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
                            `/api/trips/${post.tripId}/images/${meta.photoId}` as any,
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
            image = photos[0];
            updateImageUrl();
        } catch (err) {
            error = "Failed to fetch images: " + (err as Error).message;
        }
    }

    /**
     * Handler for carousel buttons click
     * @param event
     * @param func
     */
    function handleCarouselButtons(event, func: () => {}) {
        event.stopPropagation();
        func();
    }
</script>

<svelte:window
    onkeydown={(e) => {
        if (e.key === "ArrowLeft" && index !== 0) {
            moveToLeftImage();
        } else if (
            e.key === "ArrowRight" &&
            photos !== null &&
            index !== photos.length - 1
        ) {
            moveToRightImage();
        }
    }}
/>
<div class="fullscreen-image-wrapper">
    <div class="image-container">
        <div class="close-button-wrapper">
            <img src={imageUrl} alt="Image" class="image" />
            {#if photos !== null && photos?.length > 1 && index !== 0}
                <button
                        type="button"
                        class="btn btn-secondary bi-arrow-left carousel-arrow left-arrow"
                        title="Previous image"
                        onclick={(event) => handleCarouselButtons(event, moveToLeftImage)}
                />
            {/if}
            {#if photos !== null && photos?.length > 1 && index !== photos?.length - 1}
                <button
                    type="button"
                    class="btn btn-secondary bi-arrow-right carousel-arrow right-arrow"
                    title="Next image"
                        onclick={(event) => handleCarouselButtons(event, moveToRightImage)}
                />
            {/if}
        </div>
    </div>
</div>

<style>
    .image-container {
        position: relative;
        width: 100%;
        height: 14rem;
        display: flex;
        justify-content: center;
        align-items: center;
        background: #efe7d6;
    }
    .image {
        position: relative;
        width: 100%;
        height: 100%;
        object-fit: contain;
    }
    .close-button-wrapper {
        position: relative;
        width: 100%;
        height: 100%;
    }
    .fullscreen-image-wrapper {
        display: flex;

        justify-content: center;
    }

 .carousel-arrow {
    position: absolute;
    z-index: 2;
    top: 50%;
 }

    .left-arrow {
        left: 0.75rem;
    }

    .right-arrow {
        right: 0.75rem;
    }
</style>
