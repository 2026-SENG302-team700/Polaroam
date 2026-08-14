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
    import LocationForm from "$lib/components/forms/location-form.svelte";
    import PostForm from "$lib/components/forms/post-form.svelte";
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
    let fileErrors: string[] = $state([]);
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
                errors.title = data.errors?.title ?? data.errors?.Title ?? [];
                errors.date =
                    data.errors?.date?.[0] ?? data.errors?.Date?.[0] ?? "";
                errors.content =
                    data.errors?.content ?? data.errors?.Content ?? [];
                errors.location =
                    data.errors?.location?.[0] ??
                    data.errors?.Location?.[0] ??
                    "";
                errors.generic = data.message ?? "";
                errors.photos =
                    data.errors?.photos ?? data.errors?.Photos ?? [];
                if (
                    errors.content.includes(
                        "Post contains no content, are you sure you want to create post?",
                    ) &&
                    errors.title.length === 0 && 
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

<PostForm heading="Create Post">
    <div class="section-heading">
        <i class="bi bi-pencil-square"></i>
        <h2>Post Details</h2>
    </div>

    <TitleForm bind:name={title} errors={errors.title} {loading} />

    <DatePicker bind:value={date} error={errors.date} disabled={loading} />

    <LocationForm
        bind:searchQuery
        bind:location
        bind:error={errors.location}
        bind:loading
    />

    <DescriptionForm
        placeholder="Content"
        bind:description={content}
        errors={errors.content}
        bind:loading
    />

    <div class="section-divider"></div>

    <div class="section-heading">
        <i class="bi bi-camera"></i>
        <h2>Photos</h2>
    </div>

    <ImageDisplay
        bind:images={files}
        letDelete={true}
        errors={[...errors.photos, ...fileErrors]}
    >
        <ImagePicker
            bind:filesList={files}
            errors={errors.photos}
            bind:fileErrors
            {loading}
        />
    </ImageDisplay>

    <MultipleErrorDisplay errors={errors.generic ? [errors.generic] : []} />

    <div class="post-form-actions">
        <ConfirmCancelButton
            path={`/home/trips/${tripId}`}
            option1Text="Back to your Trip"
            option2Text="Stay on This Page"
        />
        <CreationButton onClick={createPost} text="Create Post" {loading} />
    </div>
</PostForm>

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

<style>
    .section-heading {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        margin: 1.25rem 0 1rem;
        color: #686868;
    }

    .section-heading h2 {
        margin: 0;
        font-size: 1.2rem;
        font-weight: 700;
    }

    .section-heading i {
        font-size: 1.15rem;
    }

    .section-divider {
        margin: 1.75rem 0;
        border-top: 2px dashed #686868;
        opacity: 0.65;
    }

    .post-form-actions {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
        margin-top: 1.75rem;
    }

    @media (max-width: 500px) {
        .post-form-actions {
            grid-template-columns: 1fr;
        }
    }
</style>
