<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import LocationForm from "$lib/components/forms/location-form.svelte";
    import ImageDisplay from "$lib/components/images/image-display.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";
    import ConfirmCancelButton from "$lib/components/buttons/confirmation-cancel-button.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import { onMount } from "svelte";
    import AiTaggingOption from "$lib/components/images/ai-tagging-upload-option.svelte";
    import FormWrapper from "$lib/components/forms/form-wrapper.svelte";

    let aiTaggingEnabled = $state(false);
    let aiTaggingForUpload = $state(false);

    let dateState: "valid" | "invalid" | "empty" = $state("valid");

    type PostFormErrors = {
        title: string[];
        content: string[];
        date: string[];
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
            date: [],
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
        osmType: "",
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
     * Fetches the display name of the user based on the post
     */
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
    async function createPost() {
        clearErrors();

        try {
            loading = true;

            const form = new FormData();
            form.append("TripId", tripId);
            form.append("Title", title);
            form.append("OsmId", location.osmId);
            form.append("OsmType", location.osmType);
            form.append("Content", content);
            form.append("Date", date?.toISOString() ?? "");
            form.append("NoContentAllowed", noContentAllowed);
            if (files != null) {
                for (let i = 0; i < files.length; i++) {
                    form.append("files", files[i]);
                }
            }
            // add the ai tagging option to the form data
            form.append("aiTaggingForUpload", aiTaggingForUpload.toString());

            const response = await fetchWithCsrf(resolve(`/api/post` as any), {
                method: "POST",
                body: form,
                credentials: "include",
            });

            if (!response.ok) {
                if (response.status === 413) {
                    errors.generic =
                        "Upload too large, please upload fewer photos at a time";
                    return;
                }

                const contentType = response.headers.get("content-type");
                if (!contentType?.includes("application/json")) {
                    errors.generic =
                        "Failed to upload images. Please try fewer images.";
                    return;
                }

                const data = await response.json();
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
                errors.content = errors.content.filter(e => e !== "Post contains no content, are you sure you want to create post?");
                
                // This section of date validation is attributed to Codex gpt-5.6-sol
                if (dateState === "invalid") {
                    errors.date = ["Invalid date"];
                } else if (dateState === "empty") {
                    errors.date = ["Date is required"];
                }
                
                return;
            }

            const data = await response.json();
            goto(resolve(`/home/trips/${tripId}/posts/${data.postId}`));
        } catch (err) {
            console.error("Failed to create post:", err);
            errors.generic = "Failed to create post: " + (err as Error).message;
        } finally {
            loading = false;
            noContentAllowed = false;
        }
    }
</script>

{#snippet header()}
    <span class="header-icon bi-journal-richtext"></span>
    <h1>{"Create Post"}</h1>
{/snippet}

{#snippet body()}
    <div class="section-heading">
        <i class="bi bi-pencil-square"></i>
        <h2>Post Details</h2>
    </div>

    <TitleForm bind:name={title} errors={errors.title} {loading} />

    <DatePicker bind:value={date} bind:inputState={dateState} bind:error={errors.date} disabled={loading} />

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
        maxLength={8196}
        bind:loading
    />
{/snippet}

{#snippet footer()}
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

    <AiTaggingOption
        bind:enabled={aiTaggingEnabled}
        bind:aiTaggingForUpload
        {loading}
    />

    <div class="post-form-actions">
        <ConfirmCancelButton
            path={`/home/trips/${tripId}`}
            option1Text="Back to your Trip"
            option2Text="Stay on This Page"
        />
        <CreationButton onClick={createPost} text="Create Post" {loading} />
    </div>
{/snippet}

<FormWrapper {header} {body} {footer} />

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
