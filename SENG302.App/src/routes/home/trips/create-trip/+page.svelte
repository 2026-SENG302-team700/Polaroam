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
        date: "",
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
            date: "",
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

            const response = await fetchWithCsrf(resolve(`/api/trip`), {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    Title: tripTitle,
                    Description: tripDescription,
                    StartDate: startDate,
                    EndDate: endDate,
                }),
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                errors.title = data.errors?.title ?? [];
                errors.description = data.errors?.description ?? [];
                errors.date = data.errors?.dates ?? "";
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
    cancelPath="/home/trips"
    {errors}
    {loading}
    onSubmit={createTrip}
/>
