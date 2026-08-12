<script lang="ts">
    import TripForm from "$lib/components/forms/trip-form.svelte";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "../../../../lib/csrf";

    let loading = $state(false);
    let tripTitle = $state("");
    let tripDescription = $state("");
    let errors = $state({
        title: [],
        description: [],
        dateOne: "",
        dateTwo: "",
        generic: "",
    });

    let startDate = $state(new Date());
    let endDate = $state(new Date());

    /**
     * Removes all errors from the form.
     */
    function clearErrors() {
        errors = {
            title: [],
            description: [],
            dateOne: "",
            dateTwo: "",
            generic: "",
        };
    }

    /**
     * Serializes a Date object to a string in the format "YYYY-MM-DDT00:00:00".
     * If the date is null or invalid, returns null.
     *
     * @param {Date | null} date - The date to serialize.
     * @returns {string | null} The serialized date string or null.
     */
    function serializeLocalDate(date: Date | null): string | null {
        if (!date || Number.isNaN(date.getTime())) {
            return null;
        }

        const year = date.getFullYear().toString().padStart(4, "0");
        const month = (date.getMonth() + 1).toString().padStart(2, "0");
        const day = date.getDate().toString().padStart(2, "0");

        return `${year}-${month}-${day}`;
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

            const response = await fetchWithCsrf(resolve(`/api/trip`), {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    Title: tripTitle,
                    Description: tripDescription,
                    StartDate: serializeLocalDate(startDate),
                    EndDate: serializeLocalDate(endDate),
                }),
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                errors.title = data.errors?.title ?? [];
                errors.description = data.errors?.description ?? [];
                errors.dateOne = data.errors?.dateOne ?? "";
                errors.dateTwo = data.errors?.dateTwo ?? "";
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
    heading="Create New Trip"
    submitText="Create Trip"
    cancelPath="/home"
    {errors}
    {loading}
    onSubmit={createTrip}
/>
