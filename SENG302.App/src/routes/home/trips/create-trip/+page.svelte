<script lang="ts">
    import CancelButton from "$lib/components/buttons/cancel-button.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import Datepicker from "$lib/datepicker/datepicker.svelte";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "../../../../lib/csrf";

    let loading = $state(false);
    let tripTitle = $state("");
    let tripDescription = $state("");
    let error; //use for validation
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

        error = [];

        // validate information (started but realised this wasnt my task so left it here)
        if (tripTitle == "") {
            error = "Trip name";
        }

        try {
            loading = true;
            error = "";
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
            console.log(data);

            goto(resolve(`/home/trips/${data.id}`));
        } catch (err) {
            error = "Failed to create trip: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
</script>

<div class="container max-w-md">
    <!-- IMPORTANT!! When our UI spike is approved, please go through all used components and change them to use the spiked library -->
    <div>
        <div style="display: flex; flex-direction: row;">
            <div>
                <CancelButton path={"/home/trips"} />
            </div>
            <h1
                class="text-center mb-4"
                style="flex: 1; justify-content: center"
            >
                Create New Trip
            </h1>
        </div>

        <div>
            <TitleForm bind:name={tripTitle} />
            <DescriptionForm bind:description={tripDescription} />
            <div class="date-picker-wrapper">
                <div>
                    <p>From</p>
                    <Datepicker bind:value={startDate} />
                </div>
                <div>
                    <p>To</p>
                    <Datepicker bind:value={endDate} />
                </div>
            </div>
            <div class="flex justify-end">
                <CreationButton onClick={createTrip} text={"Create Trip"} />
            </div>
        </div>
    </div>
</div>

<style>
    .date-picker-wrapper {
        display: grid;
        grid-template-columns: 50% 50%;
    }
</style>
