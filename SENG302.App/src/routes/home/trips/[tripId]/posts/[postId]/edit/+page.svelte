<script lang="ts">
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import PostForm from "$lib/components/forms/form-wrapper.svelte";
    import ConfirmCancelButton from "$lib/components/buttons/confirmation-cancel-button.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import LocationForm from "$lib/components/forms/location-form.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";
    import ImageDisplay from "$lib/components/images/image-display.svelte";
    import AiTaggingOption from "$lib/components/images/ai-tagging-upload-option.svelte";
    import { onMount } from "svelte";
    import { page } from "$app/state";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import { goto } from "$app/navigation";
    import FormWrapper from "$lib/components/forms/form-wrapper.svelte";
    import NotFound from "$lib/components/content/not-found.svelte";

    let title = $state("");
    let content = $state("");
    let date = $state(new Date());
    let photoIds = $state([]);
    let photoIdFileMap = new WeakMap();
    let aiTaggingEnabled = $state(false);
    let aiTaggingForUpload = $state(false);
    let postNotFound = $state(false);

    let dateState: "valid" | "invalid" | "empty" = $state("valid");

    const tripId = page.params.tripId;
    let newUploadsCount = $state(0);
    let fileErrors: string[] = $state([]);
    let confirmationModalShown = $state(false);
    let noContentAllowed = $state(false);
    let searchQuery = $state("");
    let files: File[] | null | undefined = $state([]);
    let location = $state({
        osmId: 0,
        osmType: "",
        lat: 0,
        lon: 0,
        name: "",
        displayName: "",
    });

    let loading = $state(false);

    type UpdateFormErrors = {
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
    function emptyErrors(): UpdateFormErrors {
        return {
            title: [],
            content: [],
            date: [],
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

    let errors = $state<UpdateFormErrors>(emptyErrors());

    onMount(async () => {
        await loadAiTaggingPreference();

        const postLoaded = await fetchPost();

        if (postLoaded) {
            await fetchImages();
        }
    });

    /**
     * Fetch Images
     */
    async function fetchImages() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/post/${page.params.postId}/images` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            if (!response.ok) {
                let error =
                    (await response.text()) || "Failed to fetch images.";
                errors.photos.push(error);
                return;
            }

            const metadata: {
                photoId: number;
                fileName: string;
                mimeType: string;
            }[] = await response.json();

            files = await Promise.all(
                metadata.map(async (meta) => {
                    const res = await fetchWithCsrf(
                        resolve(`/api/image/${meta.photoId}` as any),
                        {
                            method: "GET",
                            credentials: "include",
                        },
                    );

                    if (!res.ok) {
                        throw new Error(
                            (await res.text()) ||
                                `Failed to fetch image ${meta.photoId}`,
                        );
                    }

                    const blob = await res.blob();
                    const file = new File([blob], meta.fileName, {
                        type: meta.mimeType,
                    });
                    photoIdFileMap.set(file, meta.photoId);
                    return file;
                }),
            );
        } catch (err) {
            errors.photos.push(
                "Failed to fetch images: " + (err as Error).message,
            );
        } finally {
            loading = false;
        }
    }

    /**
     * Returns the details of a post to populate the form
     * the current information
     */
    async function fetchPost() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/post/${page.params.postId}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            if (response.status == 404) {
                postNotFound = true;
                return false;
            }

            const data = await response.json();
            if (!response.ok) {
                errors.generic =
                    `Failed to fetch post: ${data}` || "Failed to fetch post.";
                return false;
            }
            title = data.title;
            content = data.content;
            date = new Date(data.date);
            location.osmId = data.osmId;
            location.osmType = data.osmType;
            photoIds = data.photoIds;
            searchQuery = data.locationName;

            return true;
        } catch (err) {
            errors.generic =
                `Failed to fetch post: ${err}` || "Failed to fetch post.";
            return false;
        } finally {
            loading = false;
        }
    }

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

    /**
     * Updates the trip with the new values from the form.
     * Sends a PUT request to the backend with the updated trip details.
     */
    async function updatePost() {
        try {
            loading = true;

            const form = new FormData();
            form.append("Id", parseInt(page.params.postId));
            form.append("TripId", tripId);
            form.append("Title", title);
            form.append("OsmId", location.osmId);
            form.append("OsmType", location.osmType);
            form.append("Content", content);
            form.append("Date", date?.toISOString() ?? "");
            form.append("NoContentAllowed", noContentAllowed);
            // add the ai tagging option to the form data
            form.append("aiTaggingForUpload", aiTaggingForUpload.toString());

            const newUploadedFiles =
                newUploadsCount > 0 ? files?.slice(-newUploadsCount) : [];
            if (newUploadedFiles != null) {
                for (let i = 0; i < newUploadedFiles.length; i++) {
                    form.append("files", newUploadedFiles[i]);
                }
            }

            const response = await fetchWithCsrf(
                resolve(`/api/post/${page.params.postId}` as any),
                {
                    method: "PUT",
                    body: form,
                    credentials: "include",
                },
            );

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
                errors.date = data.errors?.date ?? [];
                errors.content = data.errors?.content ?? [];
                errors.location = data.errors?.location?.[0] ?? "";
                errors.title = data.errors?.title ?? [];
                errors.photos = data.errors?.photos ?? [];

                if (
                    errors.content.includes(
                        "Post contains no content, are you sure you want to create post?",
                    ) &&
                    errors.title.length === 0 &&
                    errors.date.length === 0 &&
                    errors.generic.length === 0 &&
                    errors.location.length === 0 &&
                    photoIds.length === 0
                ) {
                    confirmationModalShown = true;
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
            goto(resolve(`/home/trips/${data.tripId}`));
        } catch (err) {
            errors.generic =
                `Failed to update trip: ${err}` || "Failed to update trip.";
        } finally {
            loading = false;
        }
    }

    /**
     * Sends call to backend to delete image.
     * @param image
     * @param index NOT the photoId, index coming from image-display.
     */
    async function removePhoto(image: File, index: number) {
        const photoId = photoIdFileMap.get(image);
        if (photoId === undefined) {
            return;
        }

        try {
            const response = await fetchWithCsrf(
                resolve(
                    `/api/post/${page.params.postId}/images/${photoId}` as any,
                ),
                {
                    method: "DELETE",
                    credentials: "include",
                },
            );

            if (!response.ok) {
                errors.photos.push(
                    (await response.text()) || "Failed to delete image.",
                );
                files?.splice(index, 0, image);
                return;
            }

            photoIds = photoIds.filter((id) => id !== photoId);
        } catch (err) {
            errors.photos.push(
                "Failed to delete image: " + (err as Error).message,
            );
            files?.splice(index, 0, image);
        }
    }
</script>

{#snippet header()}
    <span class="header-icon bi-journal-richtext"></span>
    <h1>{"Edit Post"}</h1>
{/snippet}

{#snippet footer()}
    <div class="section-heading">
        <i class="bi bi-camera"></i>
        <h2>Photos</h2>
    </div>

    <ImageDisplay
        bind:images={files}
        {photoIds}
        errors={[...errors.photos, ...fileErrors]}
        letDelete={true}
        ondelete={removePhoto}
    >
        <ImagePicker
            bind:filesList={files}
            bind:newUploadsCount
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

        <CreationButton onClick={updatePost} text="Update Post" {loading} />
    </div>
{/snippet}

{#snippet body()}
    <div class="section-heading">
        <i class="bi bi-pencil-square"></i>
        <h2>Post Details</h2>
    </div>

    <TitleForm bind:name={title} errors={errors.title} {loading} />

    <DatePicker
        bind:value={date}
        bind:inputState={dateState}
        bind:error={errors.date}
        disabled={loading}
    />

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

{#if postNotFound}
    <NotFound />
{:else}
    <FormWrapper {header} {footer} {body} />

    <ConfirmationModal
        bind:show={confirmationModalShown}
        message="Post would contain no content or images, are you sure you want to update this post?"
        option1Text="Update anyway"
        option2Text="Cancel"
        onOption1={() => {
            noContentAllowed = true;
            confirmationModalShown = false;
            updatePost();
        }}
        onOption2={() => (confirmationModalShown = false)}
    />
{/if}

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
