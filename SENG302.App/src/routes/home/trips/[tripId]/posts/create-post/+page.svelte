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
    import ImageDisplay from "$lib/components/images/image-display.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";
    
    type PostFormErrors = {
        title: string[];
        content: string[];
        date: string;
        generic: string;
        location: string;
        photos: string[];
    }

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

    /**
     * refreshes the PostFormErrors object
     */
    function clearErrors() {
        errors = emptyErrors();
    }
    
    let errors = $state<PostFormErrors>(emptyErrors());
    
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
    let files: File[] = $state([]);
    let noContentAllowed = $state(false);
    
    let showConfirmationModal = $state(false);

    /**
     * Function for creating a post
     */
    async function createPost() {
        clearErrors();

        title = title.trim();
        content = content.trim();
        try {
            loading = true;

            const form = new FormData();
            form.append("TripId", tripId);
            form.append("Title", title);
            form.append("OsmId", location.osmId);
            form.append("Content", content);
            form.append("Date", date ? date.toISOString() : "");
            form.append("NoContentAllowed", noContentAllowed);
            if (files != null) {
                for (let i = 0; i < files.length; i++) {
                    form.append("files", files[i]);
                }
            }
            
            const response = await fetchWithCsrf(`/api/post`, {
                method: "POST",
                body: form,
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                errors.title = data.errors?.title ?? [];
                errors.date = data.errors?.date?.[0] ?? data.errors?.Date?.[0] ?? "";
                errors.content = data.errors?.content ?? [];
                errors.location = data.errors?.location?.[0] ?? "";
                errors.generic = data.message;
                errors.photos = data.errors?.photos ?? [];
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
            
            goto(resolve(`/home/trips/${tripId}`));
        } catch (err) {
            console.error("Failed to create post:", err);
            errors.generic = "Failed to create post: " + (err as Error).message;
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
