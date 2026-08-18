<script lang="ts">
    import AddButton from "$lib/components/buttons/add-button.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { resolve } from "$app/paths";
    import TagSearchForm from "$lib/components/forms/tag-search-form.svelte";
    import ImageGallery from "$lib/components/images/image-gallery.svelte";
    import { goto } from "$app/navigation";
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import AiTaggingUploadOption from "$lib/components/images/ai-tagging-upload-option.svelte";

    let tripName = "my amazing helsinki trip";
    let filesToUpload: File[] | null | undefined = $state([]);
    let loading = $state(false);
    let { params } = $props();
    let fileErrors: string[] = $state([]);
    let imageMetadata = $state<{ photoId: number }[]>([]);
    let tripId = Number(params.tripId);
    let tagQuery = $state("");
    let tagDataError = $state("");
    let aiTaggingEnabled = $state(false);
    let aiTaggingForUpload = $state(false);

    type AlbumImageUploadErrors = {
        generic: string;
        photos: string[];
    };

    /**
     * returns an empty AlbumImageUploadErrors object
     */
    function emptyErrors(): AlbumImageUploadErrors {
        return {
            generic: "",
            photos: [],
        };
    }

    /**
     * Gets the image metadata of all images in the album matching the tagQuery
     */
    async function getImageMetadata() {
        try {
            const response = await fetch(
                resolve(`/api/album/${tripId}/tag?tag=${tagQuery}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            if (!response.ok) {
                tagDataError = await response.text();
            }
            imageMetadata = await response.json();
        } catch (err) {
            tagDataError = "An unexpected error occurred: ";
        } finally {
            loading = false;
        }
    }

    let errors = $state<AlbumImageUploadErrors>(emptyErrors());

    async function loadAiTaggingPreference() {
        const response = await fetchWithCsrf(resolve("/api/user" as any), {
            credentials: "include",
        });

        if (!response.ok) return;

        const user = await response.json();
        aiTaggingEnabled = user.aiTagging;
        aiTaggingForUpload = user.aiTagging;
    }

    onMount(async () => {
        await loadAiTaggingPreference();
    });

    /**
     * Function for creating a post
     */
    async function uploadPhotos() {
        try {
            loading = true;
            const form = new FormData();
            if (filesToUpload != null) {
                for (let i = 0; i < filesToUpload.length; i++) {
                    form.append("files", filesToUpload[i]);
                }
            }
            // add the ai tagging option to the form data
            form.append("aiTaggingForUpload", aiTaggingForUpload.toString());

            const response = await fetchWithCsrf(
                resolve(`/api/album/${params.tripId}/images` as any),
                {
                    method: "POST",
                    body: form,
                    credentials: "include",
                },
            );

            if (response.status != 201) {
                let data = await response.json();
                if (response.status === 413) {
                    errors.generic =
                        "Upload too large, please upload less photos at a time";
                    return;
                }
                errors.photos =
                    data.errors?.photos ?? data.errors?.Photos ?? [];
                return;
            }
        } catch (err) {
            console.error("Failed to upload images:", err);
            errors.generic =
                "Failed to upload images: " + (err as Error).message;
        } finally {
            loading = false;
            filesToUpload = [];
            getImageMetadata();
        }
    }

    onMount(getImageMetadata);
</script>

<div class="card mb-2 post-wrapper">
    <div class="card-header">
        <NavigationButton
            path={resolve(`/home/trips/${params.tripId}`)}
            text="Back"
        />
        <h6 class="card-subtitle mb-2 text-muted trip-name">{tripName}</h6>
    </div>
    <div class="card-body"></div>
    <div class="card-footer text-muted images"></div>
    <div class="card-body p-4" style="min-height: 100vh;">
        <div class="album-search container px-0">
            <TagSearchForm
                bind:query={tagQuery}
                performSearch={getImageMetadata}
                getSuggestionsUrl={`/api/image/tags/trip/${tripId}?query=${encodeURIComponent(tagQuery)}`}
            />
        </div>

        {#if imageMetadata.length > 0 && tagQuery !== ""}
            <p class="text-center">
                {imageMetadata.length} images found matching "{tagQuery}"
            </p>
        {:else if imageMetadata.length > 0 && tagQuery === ""}
            <p class="text-center">{imageMetadata.length} images found</p>
        {/if}

        <div class:empty-album={imageMetadata.length === 0}>
            <ImageGallery bind:imageMetadata {tripId}>
                <div class="header-content-item">
                    <AiTaggingUploadOption
                        enabled={aiTaggingEnabled}
                        bind:aiTaggingForUpload
                        {loading}
                        compact
                        floating
                        showTemporaryNotice={false}
                    />
                    <ImagePicker
                        bind:filesList={filesToUpload}
                        errors={errors.photos}
                        bind:fileErrors
                        {loading}
                        uploadFunc={uploadPhotos}
                        fill
                    />
                </div>
            </ImageGallery>
        </div>
    </div>
</div>

<style>
    .card-header {
        display: flex;
        position: relative;
        align-items: center;
    }
    .header-content-item {
        position: relative;
        display: flex;
        height: 100%;
        width: 100%;
        flex-direction: column;
        gap: 0;
    }

    .empty-album {
        padding-top: 2.25rem;
    }

    .album-search {
        margin-bottom: 1.25rem;
    }

    .header-content-wrapper {
        width: 100%;
    }

    .trip-name {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        margin: 0;
        max-width: 40%;
        min-width: 20%;
        text-align: center;
    }

    .post-wrapper {
        margin-top: 10px;
    }

    .card-body {
        max-height: 500px;
        overflow: scroll;
    }

    .images {
        max-height: 800px;
        overflow: scroll;
    }
</style>
