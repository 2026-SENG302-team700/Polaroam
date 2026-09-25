<script lang="ts">
    import TripForm from "$lib/components/forms/trip-form.svelte";
    import NotFound from "$lib/components/content/not-found.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { onMount } from "svelte";
    import { serializeLocalDate } from "$lib/datepicker/formatDate";

    let { params } = $props();

    let loading = $state(true);
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

    let tripTitle = $state("");
    let tripDescription = $state("");
    let startDate = $state(new Date());
    let endDate = $state(new Date("9999-03-03"));
    let startDateState: "valid" | "invalid" | "empty" = $state("valid");
    let endDateState: "valid" | "invalid" | "empty" = $state("valid");

    onMount(fetchTrip);

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

            if (!response.ok) {
                errors.generic = "Failed to fetch trip.";
                return;
            }

            const data = await response.json();
            tripTitle = data.title;
            tripDescription = data.description;

            startDate = new Date(data.startDate);
            endDate = new Date(data.endDate);
        } catch (err) {
            errors.generic = "Failed to fetch trip.";
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
                        StartDate:
                            startDate == null
                                ? null
                                : serializeLocalDate(startDate),
                        EndDate:
                            endDate == null
                                ? null
                                : serializeLocalDate(endDate),
                    }),
                    credentials: "include",
                },
            );

            const data = await response.json();
            if (!response.ok) {
                errors.dateOne = data.errors?.dateOne ?? "";
                errors.dateTwo = data.errors?.dateTwo ?? "";
                errors.title = data.errors?.title ?? [];
                errors.description = data.errors?.description ?? [];

                if (data.generic) {
                    errors.generic =
                        typeof data === "string"
                            ? data
                            : "Failed to update trip.";
                }

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

            goto(resolve(`/home/trips/${params.tripId}`));
        } catch (err) {
            errors.generic =
                `Failed to update trip: ${err}` || "Failed to update trip.";
        } finally {
            loading = false;
        }
    }
</script>

{#if !loading && errors.generic !== "Failed to fetch trip."}
    <TripForm
        bind:title={tripTitle}
        bind:description={tripDescription}
        bind:startDate
        bind:endDate
        bind:startDateState
        bind:endDateState
        heading="Edit Trip"
        submitText="Update Trip"
        cancelPath={`/home/trips/${params.tripId}`}
        {errors}
        {loading}
        onSubmit={updateTrip}
    />
{:else if !loading}
    <NotFound />
{/if}
