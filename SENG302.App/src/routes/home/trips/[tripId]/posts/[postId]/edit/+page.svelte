<script lang="ts">
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import CancelButton from "$lib/components/buttons/cancel-button.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import MultipleErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import LocationForm from "$lib/components/forms/location-form.svelte";
    import ImagePicker from "$lib/components/images/image-picker.svelte";

    import { onMount } from "svelte";
    import { page } from "$app/state";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import { goto } from "$app/navigation";
    import { uploadPhotos } from "$lib/posts/images";

    let title = $state("");
    let content = $state("");
    let date = $state(new Date());
    let photoIds = $state([]);

    let confirmationModalShown = $state(false);
    let noContentAllowed = $state(false);
    let searchQuery = $state("");
    let files: File[] | null | undefined = $state([]);
    let location = $state({
        osmId: 0,
        lat: 0,
        lon: 0,
        name: "",
        displayName: "",
    });

    let loading = $state(false);
    let errors = $state({
        title: [],
        content: [],
        date: "",
        location: "",
        generic: "",
        photos: "",
    });

    onMount(async () => {
        await fetchPost();
    });

    /**
     * Parse date stored within database to the local format
     * @param dateString the strin value of the date
     */
    function parseLocalDate(dateString: string): Date {
        const datePart = dateString.split("T")[0];
        const [year, month, day] = datePart.split("-").map(Number);

        return new Date(year, month - 1, day);
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

        return `${year}-${month}-${day}T00:00:00`;
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

            const data = await response.json();
            if (!response.ok) {
                errors.generic =
                    `Failed to fetch post: ${data}` || "Failed to fetch post.";
                return;
            }

            title = data.title;
            content = data.content;
            date = parseLocalDate(data.date);
            location.osmId = data.osmId;
            photoIds = data.photoIds;
            searchQuery = data.locationName;
        } catch (err) {
            errors.generic =
                `Failed to fetch post: ${err}` || "Failed to fetch post.";
        } finally {
            loading = false;
        }
    }

    /**
     * Updates the trip with the new values from the form.
     * Sends a PUT request to the backend with the updated trip details.
     */
    async function updatePost() {
        errors.title = [];
        errors.content = [];
        errors.date = "";
        errors.location = "";
        errors.generic = "";
        errors.photos = "";

        try {
            const response = await fetchWithCsrf(
                resolve(`/api/post/${page.params.postId}` as any),
                {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        Id: Number(page.params.postId),
                        TripId: Number(page.params.tripId),
                        Title: title,
                        Content: content,
                        Date: serialiseLocalDate(date),
                        OsmId: location.osmId,
                        NoContentAllowed: noContentAllowed,
                    }),
                    credentials: "include",
                },
            );
            const data = await response.json();
            

            if (!response.ok) {
                errors.date = data.errors?.date?.[0] ?? "";
                errors.content = data.errors?.content ?? [];
                errors.location = data.errors?.location?.[0] ?? "";
                errors.title = data.errors?.title ?? [];

                if (
                    errors.content.length === 1 &&
                    errors.title.length === 0 &&
                    !errors.date &&
                    errors.generic.length === 0 &&
                    errors.location.length === 0 &&
                    photoIds.length === 0 &&
                    files?.length === 0
                ) {
                    confirmationModalShown = true;
                } else if (
                    (errors.content.length === 1 &&
                        errors.title.length === 0 &&
                        !errors.date &&
                        errors.generic.length === 0 &&
                        errors.location.length === 0 &&
                        photoIds.length !== 0) ||
                    files?.length !== 0
                ) {
                    confirmationModalShown = false;
                    noContentAllowed = true;
                    updatePost();
                }
                return;
            }
            if (files != null && files !== undefined && files.length > 0) {
                let message: string | undefined = await uploadPhotos(
                    data.postId,
                    data.tripId,
                    files,
                );
                if (message !== undefined) {
                    errors.photos = message;
                }
            }

            if (errors.photos === "") {
                goto(resolve(`/home/trips/${data.tripId}`));
            }
        } catch (err) {
            errors.generic =
                `Failed to update trip: ${err}` || "Failed to update trip.";
        } finally {
            loading = false;
        }
    }
</script>

<div class="container d-flex justify-content-center align-items-center">
    <div class="w-100" style="max-width: 600px;">
        <TitleForm bind:name={title} errors={errors.title}/>
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
        <ImagePicker bind:filesList={files} error={errors.photos}></ImagePicker>
        <MultipleErrorDisplay errors={[errors.generic]} />
        <div class="d-grid">
            <CreationButton onClick={updatePost} text="Update Post" />
        </div>
        <div class="d-grid mt-3">
            <CancelButton path={`/home/trips/${page.params.tripId}`} />
        </div>
    </div>
</div>

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
