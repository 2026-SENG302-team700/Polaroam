<script lang="ts">
    import AddButton from "$lib/components/buttons/add-button.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { resolve } from "$app/paths";

    let tripName = "my amazing helsinki trip";
    let filesToUpload: File[] | null | undefined = $state([]);
    let loading = $state(false);
    let { params } = $props();
    let fileErrors: string[] = $state([]);
    let imageMetadata = $state([]);
    let tripId = params.tripId;
    let tagQuery = $state("");
    
    let tagDataError = $state("");

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
        try{
            const response = await fetch(resolve(`/api/album/${tripId}/tags?query=${tagQuery}`),
                {
                    method: 'GET',
                    credentials: 'include'
                });
            if (!response.ok) {
                tagDataError = await response.text();
            }
            imageMetadata = await response.json();
        } catch (err) {
            tagDataError = "An unexpected error occurred: ";
        }
    }

    let errors = $state<AlbumImageUploadErrors>(emptyErrors());

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

            const response = await fetchWithCsrf(resolve(`/api/album/${params.tripId}/images` as any), {
                method: "POST",
                body: form,
                credentials: "include",
            });

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
            errors.generic = "Failed to upload images: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
</script>

<div class="card mb-2 post-wrapper">
    <div class="card-header">
        <h6 class="card-subtitle mb-2 text-muted trip-name">{tripName}</h6>
        <div class="header-content-wrapper"> 
            <div class="header-content-item">
                <ImagePicker
                    bind:filesList={filesToUpload}
                    errors={errors.photos}
                    bind:fileErrors
                    {loading}
                    uploadFunc={uploadPhotos}
                />
            </div>
        </div>
    </div>
    <div class="card-body">

    </div>
    <div class="card-footer text-muted images">

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
    align-items:baseline;
    justify-content: end;
}

.header-content-wrapper {
    width: 100%;
}

.trip-name {
    max-width: 40%;
    min-width: 20%;
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