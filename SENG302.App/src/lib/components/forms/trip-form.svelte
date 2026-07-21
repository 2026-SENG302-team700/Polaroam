<script lang="ts">
    import CancelButton from "$lib/components/buttons/cancel-button.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import Datepicker from "$lib/datepicker/datepicker.svelte";
    import DescriptionForm from "./description-form.svelte";
    import TitleForm from "./title-form.svelte";

    let {
        title = $bindable(""),
        description = $bindable(""),
        startDate = $bindable(null),
        endDate = $bindable(null),
        heading,
        submitText,
        cancelPath,
        error = "",
        loading = false,
        onSubmit,
    }: {
        title: string;
        description: string;
        startDate: Date | null;
        endDate: Date | null;
        heading: string;
        submitText: string;
        cancelPath: string;
        error?: string;
        loading?: boolean;
        onSubmit: () => void | Promise<void>;
    } = $props();
</script>

<div class="container max-w-md">
    <div class="form-header">
        <CancelButton path={cancelPath} />
        <h1 class="text-center mb-4">{heading}</h1>
    </div>

    <TitleForm bind:name={title} type="trip" {loading} />
    <DescriptionForm bind:description {loading} />

    <div class="date-picker-wrapper">
        <div>
            <p>From</p>
            <Datepicker bind:value={startDate} disabled={loading} />
        </div>
        <div>
            <p>To</p>
            <Datepicker bind:value={endDate} disabled={loading} />
        </div>
    </div>

    {#if error}
        <div class="alert alert-danger" role="alert">{error}</div>
    {/if}

    <div class="flex justify-end">
        <CreationButton onClick={onSubmit} text={submitText} />
    </div>
</div>

<style>
    .form-header {
        display: grid;
        grid-template-columns: auto 1fr auto;
        align-items: start;
    }

    .form-header h1 {
        grid-column: 2;
    }

    .date-picker-wrapper {
        display: grid;
        grid-template-columns: 50% 50%;
    }
</style>
