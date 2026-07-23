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
     * Parse date stored within database to the local format
     * @param dateString the strin value of the date
     */
    function parseLocalDate(dateString: string): Date {
        const datePart = dateString.split("T")[0];
        const [year, month, day] = datePart.split("-").map(Number);

        return new Date(year, month - 1, day);
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
                resolve(`/api/trip/${params.slug}` as any),
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
            startDate = parseLocalDate(data.startDate);
            endDate = parseLocalDate(data.endDate);
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

            goto(resolve(`/home/trips/${params.slug}`));
        } catch (err) {
            errors.generic =
                `Failed to update trip: ${err}` || "Failed to update trip.";
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
    submitText="Update Trip"
    cancelPath={`/home/trips/${params.slug}`}
    {errors}
    {loading}
    onSubmit={updateTrip}
/>
