<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";

    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import SearchForm from "$lib/components/forms/search-form.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import MultipleErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import { error } from "node:console";

    let loading = $state(false);
    const tripId = page.params.tripId;

    let title = $state("");
    let date = $state(new Date());
    let searchQuery = $state("");
    let location = $state("Temporary");
    let content = $state("");

    let errors = {
        title: [] as string[],
        description: [] as string[],
        date: [] as string[],
        generic: "",
    };

    /**
     * Clear all current errors to start fresh
     */
    function clearErrors() {
        errors.title = [];
        errors.description = [];
        errors.date = [];
        errors.generic = "";
    }

    /**
     * Create a new post after nessasary validation and trimming is complete.
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
                    Location: location,
                    Content: content,
                    Date: date,
                }),
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                errors.title = data.errors?.title ? [data.errors.title] : [];
                // remaining validation
                const hasFieldErrors = errors.title.length > 0;

                if (!hasFieldErrors) {
                    errors.generic = data.message ?? "Failed to create post";
                }

                return;
            }

            goto(resolve(`/home/trips/${tripId}`));
        } catch (err) {
            console.error("Failed to create trip:", err);
            errors.generic = "Failed to create trip: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
</script>

<div class="container d-flex justify-content-center align-items-center">
    <div class="w-100" style="max-width: 600px;">
        <TitleForm bind:name={title} errors={errors.title} {loading} />
        <DatePicker bind:value={date} />
        <SearchForm bind:query={searchQuery} placeholder="Location *" />
        <DescriptionForm placeholder="Content" bind:description={content} />
        <MultipleErrorDisplay errors={[errors.generic]} />
        <div class="d-grid">
            <CreationButton onClick={createPost} text="Create Post" />
        </div>
    </div>
</div>
