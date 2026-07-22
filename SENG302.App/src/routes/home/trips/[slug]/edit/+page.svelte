<script lang="ts">
    import TripForm from "$lib/components/forms/trip-form.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { onMount } from "svelte";

    let { params } = $props();

    let loading = $state(true);
    let error = $state("");

    let tripTitle = $state("");
    let tripDescription = $state("");
    let startDate = $state(new Date());
    let endDate = $state(new Date("9999-03-03"));

    onMount(fetchTrip);

    /**
     * Fetches the trip from the backend corresponding
     * with the slug value being the trip's id
     */
    async function fetchTrip() {
        try {
            loading = true;

            const response = await fetchWithCsrf(
                resolve(`/api/trip/${params.slug}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );

            const data = await response.json();
            if (!response.ok) {
                error =
                    `Failed to fetch trip: ${data}` || "Failed to fetch trip.";
                return;
            }

            tripTitle = data.title;
            tripDescription = data.description;
            startDate = new Date(data.startDate);
            endDate = new Date(data.endDate);
        } catch (err) {
            error = `Failed to fetch trip: ${err}` || "Failed to fetch trip.";
        } finally {
            loading = false;
        }
    }

    /**
     * Updates the trip with the new values from the form.
     * Sends a PUT request to the backend with the updated trip details.
     */
    async function updateTrip() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${params.slug}` as any),
                {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        Id: Number(params.slug),
                        Title: tripTitle,
                        Description: tripDescription,
                        StartDate: serializeLocalDate(startDate),
                        EndDate: serializeLocalDate(endDate),
                    }),
                    credentials: "include",
                },
            );

            const data = await response.json();
            if (!response.ok) {
                error =
                    `Failed to update trip: ${data}` ||
                    "Failed to update trip.";
                return;
            }

            goto(resolve(`/home/trips/${params.slug}`));
        } catch (err) {
            error = `Failed to update trip: ${err}` || "Failed to update trip.";
        } finally {
            loading = false;
        }
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

        return `${year}-${month}-${day}T00:00:00`;
    }
</script>

<TripForm
    bind:title={tripTitle}
    bind:description={tripDescription}
    bind:startDate
    bind:endDate
    heading="Edit Trip"
    submitText="Save Changes"
    cancelPath={`/home/trips/${params.slug}`}
    {error}
    {loading}
    onSubmit={updateTrip}
/>
