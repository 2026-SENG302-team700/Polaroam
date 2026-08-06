<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import { uploadPhotos } from "$lib/posts/images";
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import MultipleErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import LocationForm from "$lib/components/forms/location-form.svelte";
    import ImageDisplay from "$lib/components/images/image-display.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";
    import ConfirmCancelButton from "$lib/components/buttons/confirmation-cancel-button.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";

    type PostFormErrors = {
        title: string[];
        content: string[];
        date: string;
        generic: string;
        location: string;
        photos: string[];
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
            photos: [],
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
    const tripId = page.params.tripId;

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
    let files: File[] | null | undefined = $state([]);
    let noContentAllowed = $state(false);
    let showConfirmationModal = $state(false);

    /**
     * Function for creating a post
     */
    async function createPost() {
        clearErrors();

        try {
            loading = true;

            const form = new FormData();
            form.append("TripId", tripId);
            form.append("Title", title);
            form.append("OsmId", location.osmId);
            form.append("Content", content);
            form.append("Date", serialiseLocalDate(date) ?? "");
            form.append("NoContentAllowed", noContentAllowed);
            if (files != null) {
                for (let i = 0; i < files.length; i++) {
                    form.append("files", files[i]);
                }
            }

            const response = await fetchWithCsrf(resolve(`/api/post` as any), {
                method: "POST",
                body: form,
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                if (response.status === 413) {
                    errors.generic =
                        "Upload too large, please upload less photos at a time";
                    return;
                }
                errors.title = data.errors?.title ?? [];
                errors.date = data.errors?.date?.[0] ?? "";
                errors.content = data.errors?.content ?? [];
                errors.location = data.errors?.location?.[0] ?? "";
                errors.generic = data.message ?? "";
                errors.photos = data.errors?.photos ?? [];
                if (
                    errors.content.includes(
                        "Post contains no content, are you sure you want to create post?",
                    ) &&
                    errors.title.length === 0 &&
                    errors.photos.length === 0 &&
                    !errors.date &&
                    !errors.generic &&
                    !errors.location
                ) {
                    showConfirmationModal = true;
                }
                return;
            }

            goto(resolve(`/home/trips/${tripId}`));
        } catch (err) {
            console.error("Failed to create post:", err);
            errors.generic = "Failed to create post: " + (err as Error).message;
        } finally {
            loading = false;
            noContentAllowed = false;
        }
    }

    /**
     * Converts a local date to a date only string without UTC timezone shifting
     * @param date
     */
    function serialiseLocalDate(date: Date | null): string | null {
        if (!date || Number.isNaN(date.getTime())) {
            return null;
        }

        const year = date.getFullYear().toString().padStart(4, "0");
        const month = (date.getMonth() + 1).toString().padStart(2, "0");
        const day = date.getDate().toString().padStart(2, "0");

        return `${year}-${month}-${day}`;
    }
</script>

<div class="container d-flex justify-content-center align-items-center">
    <div class="w-100" style="max-width: 600px;">
        <div class="form-header">
            <ConfirmCancelButton
                path={`/home/trips/${tripId}`}
                option1Text="Back to your Trip"
                option2Text="Stay on This Page"
            />

            <h1 class="text-center mb-4">Create Post</h1>
        </div>

        <TitleForm bind:name={title} errors={errors.title} {loading} />
        <DatePicker bind:value={date} error={errors.date} />
        <LocationForm
            bind:searchQuery
            bind:location
            bind:error={errors.location}
        />
        <DescriptionForm placeholder="Content" bind:description={content} errors={errors.content} />
        <ImageDisplay 
                bind:images={files} 
                letDelete={true} 
                errors="{errors.photos}"
        >
            <ImagePicker bind:filesList={files} errors={errors.photos} />
        </ImageDisplay>
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
