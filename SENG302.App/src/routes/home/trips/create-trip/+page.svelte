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

    let startDate: Date | null | undefined = $state(new Date());
    let endDate: Date | null | undefined = $state(new Date());

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

        console.log("startdate: " + startDate + " end: " + endDate);

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

                if (startDate === undefined) {
                    errors.dateOne = "Start date is required";
                } 
                if (endDate === undefined) {
                    errors.dateTwo = "End date is required";
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
    heading="Create New Trip"
    submitText="Create Trip"
    cancelPath="/home"
    {errors}
    {loading}
    onSubmit={createTrip}
/>
