<script lang="ts">
    import TripForm from "$lib/components/forms/trip-form.svelte";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "../../../../lib/csrf";

    let loading = $state(false);
    let tripTitle = $state("");
    let tripDescription = $state("");
    let error = $state("");
    let startDate = $state(new Date());
    let endDate = $state(new Date("9999-03-03"));

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

        // validate information (started but realised this wasnt my task so left it here)
        if (tripTitle == "") {
            error = "Trip name required";
            return;
        }

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
                error =
                    `Failed to create list: ${data}` ||
                    "Failed to create list.";
                return;
            }

            goto(resolve(`/home/trips/${data.id}`));
        } catch (err) {
            error = "Failed to create trip: " + (err as Error).message;
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
    {error}
    {loading}
    onSubmit={createTrip}
/>
