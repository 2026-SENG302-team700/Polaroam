<script lang="ts">
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import Datepicker from "$lib/datepicker/datepicker.svelte";

    let tripName = $state("");
    let tripDescription = $state("");
    let error; //use for validation

</script>

<div class="container">
    <div style="display: flex; flex-direction: row; ">
        <h1 class="text-center mb-4" style="flex: 1; justify-content: center">
            Create New Trip
        </h1>
    </div>

    <div>
    <TitleForm name={tripName} {error} />
    <DescriptionForm description={tripDescription} {error}/>
    <Datepicker />
    <Datepicker />
    </div>
</div>

<style>
    .cursor-pointer {
        cursor: pointer;
    }

    .task-item-box {
        background-color: transparent;
        transition: background-color 0.2s ease;
    }

    .task-item-box:hover {
        background-color: #f8f8f8;
    }
</style>
