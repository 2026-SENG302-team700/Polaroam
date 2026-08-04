<script lang="ts">
    import TripForm from "$lib/components/forms/trip-form.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { onMount } from "svelte";

    let { params } = $props();

    let loading = $state(true);
    let errors = $state({
        title: [],
        description: [],
        date: "",
        generic: "",
    });

    let tripTitle = $state("");
    let tripDescription = $state("");
    let startDate = $state(new Date());
    let endDate = $state(new Date("9999-03-03"));

    onMount(fetchTrip);

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
     * Fetches the trip from the backend corresponding
     * with the slug value being the trip's id
     */
    async function fetchTrip() {
        clearErrors();
        try {
            loading = true;

            const response = await fetchWithCsrf(
                resolve(`/api/trip/${params.tripId}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );

            const data = await response.json();
            if (!response.ok) {
                errors.generic =
                    `Failed to fetch trip: ${data}` || "Failed to fetch trip.";
                return;
            }

            tripTitle = data.title;
            tripDescription = data.description;
            startDate = new Date(data.startDate);
            endDate = new Date(data.endDate);
        } catch (err) {
            errors.generic =
                `Failed to fetch trip: ${err}` || "Failed to fetch trip.";
        } finally {
            loading = false;
        }
    }

    /**
     * Updates the trip with the new values from the form.
     * Sends a PUT request to the backend with the updated trip details.
     */
    async function updateTrip() {
        clearErrors();
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${params.tripId}` as any),
                {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        Id: Number(params.tripId),
                        Title: tripTitle,
                        Description: tripDescription,
                        StartDate: startDate.toISOString(),
                        EndDate: endDate.toISOString(),
                    }),
                    credentials: "include",
                },
            );

            const data = await response.json();
            if (!response.ok) {
                errors.date = data.errors?.dates?.join(" ") ?? "";
                errors.title = data.errors?.title ?? "";
                errors.description = data.errors?.description ?? "";

                if (data.generic) {
                    errors.generic =
                        typeof data === "string"
                            ? data
                            : "Failed to update trip.";
                }

                return;
            }

            goto(resolve(`/home/trips/${params.tripId}`));
        } catch (err) {
            errors.generic =
                `Failed to update trip: ${err}` || "Failed to update trip.";
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
    heading="Edit Trip"
    submitText="Update Trip"
    cancelPath={`/home/trips/${params.tripId}`}
    {errors}
    {loading}
    onSubmit={updateTrip}
/>
