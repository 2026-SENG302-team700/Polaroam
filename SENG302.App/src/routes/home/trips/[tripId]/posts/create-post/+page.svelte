<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import MultipleErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import LocationForm from "$lib/components/forms/location-form.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";

    type PostFormErrors = {
        title: string[];
        content: string[];
        date: string;
        generic: string;
        location: string;
        photos: string;
    };

    /**
     * returns an empty PostFormErrors object
     */
    function emptyErrors(): PostFormErrors {
        return {
            title: [],
            content: [],
            date: "",
            generic: "",
            location: "",
            photos: "",
        };
    }

    let errors = $state<PostFormErrors>(emptyErrors());

    /**
     * refreshes the PostFormErrors object
     */
    function clearErrors() {
        errors = emptyErrors();
    }
    
    let loading = $state(false);
    const tripId = page.params.tripId!;

    let title = $state("");
    let date = $state(new Date());
    let searchQuery = $state("");
    let location = $state({
        osmId: 0,
        lat: 0,
        lon: 0,
        name: "",
        displayName: "",
    });
    let content = $state("");
    let files: File[] = $state([]);
    let noContentAllowed = $state(false);

    let showConfirmationModal = $state(false);

    /**
     * Function for uploading photos to the backend for a particular post
     * @param postId Id of the post
     */
    async function uploadPhotos(postId: number) {
        if (files != null) {
            try {
                const formData = new FormData();
                for (let i = 0; i < files.length; i++) {
                    formData.append("files", files[i]);
                    formData.append("postId", postId.toString());
                    formData.append("tripId", tripId.toString());
                }

                const response = await fetchWithCsrf(
                    resolve(`/api/post/images` as any),
                    {
                        method: "PUT",
                        body: formData,
                    },
                );

                let customImageIds: String[];
                if (!response.ok) {
                    if (response.status == 500) {
                        errors.photos = "Failed to upload picture";
                    } else {
                        errors.photos = await response.text();
                    }
                } else {
                    customImageIds = await response.json();
                    goto(resolve(`/home/trips/${tripId}`));;
                }
            } catch (err) {
                errors.photos = (err as Error).message;
            }
        }
    }

    /**
     * Function for creating a post
     */
    async function createPost() {
        clearErrors();

        title = title.trim();
        content = content.trim();

        // validation the title
        if (title.length < 3 || title.length > 128) {
            errors.title = [
                "Title is required and must be between 3 and 128 characters long",
            ];

            return;
        }

        try {
            loading = true;

            const response = await fetchWithCsrf(`/api/post`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    TripId: Number(tripId),
                    Title: title,
                    OsmId: location.osmId,
                    Content: content,
                    Date: date,
                    NoContentAllowed: noContentAllowed,
                }),
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                errors.title = data.errors?.title ?? [];
                errors.date = data.errors?.date?.[0] ?? "";
                errors.content = data.errors?.content ?? [];
                errors.location = data.errors?.location?.[0] ?? "";
                errors.generic = data.message ?? "";

                if (
                    errors.content.length === 1 &&
                    errors.title.length === 0 &&
                    !errors.date &&
                    !errors.generic &&
                    !errors.location
                ) {
                    showConfirmationModal = true;
                }
                return;
            }

            if (files != null) {
                await uploadPhotos(data.postId);
            } else {
                goto(resolve(`/home/trips/${tripId}`));
            }
        } catch (err) {
            errors.generic = "Failed to create trip";
        } finally {
            loading = false;
            noContentAllowed = false;
        }
    }
</script>

<div class="container d-flex justify-content-center align-items-center">
    <div class="w-100" style="max-width: 600px;">
        <TitleForm bind:name={title} errors={errors.title} {loading} />
        <DatePicker bind:value={date} error={errors.date} />
        {#if errors.date}
            <div class="invalid-feedback d-block">{errors.date}</div>
        {/if}
        <LocationForm
            bind:searchQuery
            bind:location
            bind:error={errors.location}
        />
        <DescriptionForm placeholder="Content" bind:description={content} />
        <ImageDisplay images={files}/>
        <ImagePicker bind:filesList={files} error={errors.photos}></ImagePicker>
        <MultipleErrorDisplay errors={errors.generic ? [errors.generic] : []} />
        <div class="d-grid">
            <CreationButton onClick={createPost} text="Create Post" />
        </div>
    </div>
</div>

<ConfirmationModal
    bind:show={showConfirmationModal}
    message="Post contains no content, are you sure you want to create post?"
    option1Text="Create anyway"
    option2Text="Cancel"
    onOption1={() => {
        noContentAllowed = true;
        showConfirmationModal = false;
        createPost();
    }}
    onOption2={() => (showConfirmationModal = false)}
/>
