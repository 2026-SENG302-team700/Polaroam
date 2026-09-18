<script lang="ts">
    import TripForm from "$lib/components/forms/trip-form.svelte";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";

    let loading = $state(false);
    let tripTitle = $state("");
    let tripDescription = $state("");

    let errors = $state<{
        title: string[];
        description: string[];
        dateOne: string[];
        dateTwo: string[];
        generic: string;
    }>({
        title: [],
        description: [],
        dateOne: [],
        dateTwo: [],
        generic: "",
    });

    let startDate: Date | null | undefined = $state(new Date());
    let endDate: Date | null | undefined = $state(new Date());
    let startDateState: "valid" | "invalid" | "empty" = $state("valid");
    let endDateState: "valid" | "invalid" | "empty" = $state("valid");

    /**
     * Removes all errors from the form.
     */
    function clearErrors() {
        errors = {
            title: [],
            description: [],
            dateOne: [],
            dateTwo: [],
            generic: "",
        };
    }

    /**
     * Validates and submits a new trip to the API, then navigates to it.
     *
     * Trims the title/description, POSTs to `/api/trip`, and on success
     * redirects to `/home/trips/{id}`. On failure, sets `error` with a
     * message. Toggles `loading` for the duration of the request.
     *
     * @async
     * @returns {Promise<void>}
     */
    async function createTrip() {
        // trim name and desciption
        tripTitle = tripTitle.trim();
        tripDescription = tripDescription.trim();

        clearErrors();

        try {
            loading = true;

            const response = await fetchWithCsrf(resolve(`/api/trip` as any), {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    Title: tripTitle,
                    Description: tripDescription,
                    StartDate: startDate?.toISOString(),
                    EndDate: endDate?.toISOString(),
                }),
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                errors.title = data.errors?.title ?? [];
                errors.description = data.errors?.description ?? [];
                errors.dateOne = data.errors?.dateOne ?? "";
                errors.dateTwo = data.errors?.dateTwo ?? "";

                // This section of date validation is attributed to Codex gpt-5.6-sol
                if (startDateState === "invalid") {
                    errors.dateOne = ["Invalid date"];
                } else if (startDateState === "empty") {
                    errors.dateOne = ["Start date is required"];
                }

                if (endDateState === "invalid") {
                    errors.dateTwo = ["Invalid date"];
                } else if (endDateState === "empty") {
                    errors.dateTwo = ["End date is required"];
                }
                

                return;
            }

            goto(resolve(`/home/trips/${data.id}`));
        } catch (err) {
            errors.generic = "Failed to create trip: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
</script>

<TripForm
    bind:title={tripTitle}
    bind:description={tripDescription}
    bind:startDate
    bind:endDate
    bind:startDateState
    bind:endDateState
    heading="Create New Trip"
    submitText="Create Trip"
    cancelPath="/home"
    {errors}
    {loading}
    onSubmit={createTrip}
/>
