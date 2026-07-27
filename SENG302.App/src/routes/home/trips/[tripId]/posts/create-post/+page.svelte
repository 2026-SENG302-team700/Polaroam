<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    
    import { uploadPhotos } from "$lib/posts/images";

    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import SearchForm from "$lib/components/forms/search-form.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import MultipleErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import LocationForm from "$lib/components/forms/location-form.svelte";
    import ImagePicker from "$lib/components/image-picker.svelte";

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
    let files: FileList | null = $state(null);
    let noContentAllowed = $state(false);

    let showConfirmationModal = $state(false);

    let errors = $state({
        title: [],
        content: [],
        date: "",
        location: "",
        generic: "",
        photos: "",
    });

    

    /**
     * Function for creating a post
     */
    async function createPost() {
        title = title.trim();
        content = content.trim();

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
                errors.date = data.errors?.date?.[0] ?? "";
                errors.content = data.errors?.content ?? [];
                errors.location = data.errors?.location?.[0] ?? "";

                if (
                    errors.content.length === 1 &&
                    errors.title.length === 0 &&
                    !errors.date &&
                    errors.generic.length === 0 &&
                    errors.location.length === 0
                ) {
                    showConfirmationModal = true;
                }
                return;
            }

            if (files != null) {
                await uploadPhotos(data.postId);
            }
            
            goto(resolve(`/home/trips/${tripId}`));

        } catch (err) {
            errors.generic = ["Failed to create trip"];
        } finally {
            loading = false;
            noContentAllowed = false;
        }
    }
</script>

<div class="container d-flex justify-content-center align-items-center">
    <div class="w-100" style="max-width: 600px;">
        <TitleForm bind:name={title} />
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
        <ImagePicker bind:files error={errors.photos}></ImagePicker>
        <MultipleErrorDisplay errors={[errors.generic]} />
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
