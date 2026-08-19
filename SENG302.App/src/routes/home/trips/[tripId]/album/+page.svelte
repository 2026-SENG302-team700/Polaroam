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

    let tripName = $state("");
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
    let uploadModalShown = $state(false);

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
        fetchTrip();
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

    /**
     * Fetches the trip from the backend corresponding
     * with the slug value being the trip's id
     */
    async function fetchTripName() {
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
            tripName = data.title;
        } catch (err) {
            error = "Failed to fetch trip: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
    function preparePhotoUpload() {
        if (!filesToUpload?.length) return;

        if (aiTaggingEnabled) {
            aiTaggingForUpload = true;
            uploadModalShown = true;
            return;
        }

        uploadPhotos();
    }

    function cancelPhotoUpload() {
        uploadModalShown = false;
        filesToUpload = [];
    }

    function confirmPhotoUpload() {
        uploadModalShown = false;
        uploadPhotos();
    }

    onMount(getImageMetadata);
</script>

<div class="card mb-2 post-wrapper">
    <div class="card-header">
        <NavigationButton path={`/home/trips/${params.tripId}`} text="Back" />
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
            <ImageGallery
                bind:imageMetadata
                {tripId}
                errors={[errors.generic, ...errors.photos, ...fileErrors]}
                errorsBesidePicker
            >
                <div class="header-content-item">
                    <ImagePicker
                        bind:filesList={filesToUpload}
                        errors={errors.photos}
                        bind:fileErrors
                        {loading}
                        uploadFunc={preparePhotoUpload}
                        fill
                    />
                </div>
            </ImageGallery>
        </div>
    </div>
</div>

{#if uploadModalShown}
    <div class="modal-backdrop show"></div>
    <div class="modal d-block" tabindex="-1" role="dialog" aria-modal="true">
        <div class="modal-dialog modal-dialog-centered" role="document">
            <div class="modal-content upload-modal">
                <div class="modal-header">
                    <h2 class="modal-title fs-5">Upload images</h2>
                </div>
                <div class="modal-body">
                    <p>
                        AI image tagging is enabled. The selected images will be
                        analysed and tagged after they are uploaded.
                    </p>
                    <AiTaggingUploadOption
                        enabled={aiTaggingEnabled}
                        bind:aiTaggingForUpload
                        {loading}
                    />
                </div>
                <div class="modal-footer d-flex gap-2">
                    <button
                        type="button"
                        class="btn btn-outline-secondary flex-fill"
                        onclick={cancelPhotoUpload}
                    >
                        Cancel
                    </button>
                    <button
                        type="button"
                        class="btn btn-primary flex-fill"
                        onclick={confirmPhotoUpload}
                    >
                        Upload
                    </button>
                </div>
            </div>
        </div>
    </div>
{/if}

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

    .upload-modal {
        border: 2px solid #686868;
        border-radius: 0.55rem;
        background: #fff8e8;
        box-shadow: 6px 6px 0 #686868;
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
