<script lang="ts">
    import AddButton from "$lib/components/buttons/add-button.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { resolve } from "$app/paths";
    import ImageGallery from "$lib/components/images/image-gallery.svelte";
    import { goto } from "$app/navigation";
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";

    let tripName = "my amazing helsinki trip";
    let files: File[] | null | undefined = $state([]);
    let loading = $state(false);
    let { params } = $props();
    let fileErrors: string[] = $state([]);
    let tripId = params.tripId;
    let imageMetadata = $state([]);
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

    let errors = $state<AlbumImageUploadErrors>(emptyErrors());

    /**
     * Gets the Metadata for all images in the album
     */
    async function getImageMetadata() {
        const response = await fetch(resolve(`/api/album/${tripId}`), {
            method: "GET",
            credentials: "include",
        });
        imageMetadata = await response.json();
    }

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
            if (files != null) {
                for (let i = 0; i < files.length; i++) {
                    form.append("files", files[i]);
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
        <div class="header-content-wrapper">
            <div class="header-content-item">
                <ImagePicker
                    bind:filesList={files}
                    errors={errors.photos}
                    bind:fileErrors
                    bind:aiTaggingForUpload
                    {loading}
                    uploadFunc={uploadPhotos}
                    showAiTaggingOption={aiTaggingEnabled}
                />
            </div>
        </div>
    </div>
    <div class="card-body"></div>
    <div class="card-footer text-muted images"></div>
    <div class="card-body p-4" style="min-height: 100vh;">
        <ImageGallery bind:imageMetadata {tripId} />
    </div>
</div>

<style>
    .card-header {
        display: flex;
        position: relative;
        align-items: center;
    }
    .header-content-item {
        display: flex;
        gap: 4px;
        align-items: baseline;
        justify-content: end;
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
