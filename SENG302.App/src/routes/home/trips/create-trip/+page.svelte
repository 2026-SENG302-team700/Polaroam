<script lang="ts">
    import CancelButton from "$lib/components/buttons/cancel-button.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import Datepicker from "$lib/datepicker/datepicker.svelte";

    let tripName = $state("");
    let tripDescription = $state("");
    let error; //use for validation
    let startDate = $state(new Date());
    let endDate = $state(new Date("9999-03-03"));
</script>

<div class="container max-w-md">
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
            <TitleForm bind:name={tripName} />
            <DescriptionForm bind:description={tripDescription} />
            <div class="grid grid-cols-2">
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
                <CreationButton text={"Create Trip"} />
            </div>
        </div>
    </div>
    <div></div>
</div>
