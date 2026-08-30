<script lang="ts">
    import CancelButton from "$lib/components/buttons/cancel-button.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import Datepicker from "$lib/datepicker/datepicker.svelte";
    import DescriptionForm from "./description-form.svelte";
    import FormWrapper from "./form-wrapper.svelte";
    import TitleForm from "./title-form.svelte";

    type TripFormErrors = {
        title: string[];
        description: string[];
        dateOne: string;
        dateTwo: string;
        generic: string;
    };

    let {
        title = $bindable(""),
        description = $bindable(""),
        startDate = $bindable(null),
        endDate = $bindable(null),
        heading,
        errors = {
            title: [],
            description: [],
            dateOne: "",
            dateTwo: "",
            generic: "",
        },
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
        errors?: TripFormErrors;
        loading?: boolean;
        onSubmit: () => void | Promise<void>;
    } = $props();
</script>

{#snippet header()}
    <span class="header-icon bi-camera"></span>
    <h1>{heading}</h1>
{/snippet}

{#snippet body()}
    <div class="post-form-body">
        <div class="section-heading">
            <i class="bi bi-pencil-square"></i>
            <h2>Trip Details</h2>
        </div>
        <TitleForm
            bind:name={title}
            type="trip"
            errors={errors.title}
            {loading}
        />
        <DescriptionForm
            bind:description
            errors={errors.description}
            {loading}
        />

        <div class="date-picker-wrapper">
            <div class="date-field">
                <label for="trip-start-date">Start Date</label>
                <Datepicker
                    bind:value={startDate}
                    disabled={loading}
                    error={errors.dateOne}
                />
            </div>
            <div class="date-field">
                <label for="trip-end-date">End Date</label>
                <Datepicker
                    bind:value={endDate}
                    disabled={loading}
                    error={errors.dateTwo}
                />
            </div>
        </div>

        {#if errors.generic}
            <div class="alert alert-danger" role="alert">
                {errors.generic}
            </div>
        {/if}
    </div>
{/snippet}
{#snippet footer()}
    <div class="post-form-actions">
        <CancelButton path={`/home`} />
        <CreationButton onClick={onSubmit} text="Create Post" {loading} />
    </div>
{/snippet}

<FormWrapper {header} {body} {footer} />

<style>
    .date-picker-wrapper {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.75rem;
        align-items: start;
    }

    .date-field {
        min-width: 0;
    }

    .date-field label {
        display: block;
        margin-bottom: 0.25rem;
    }

    .date-picker-wrapper :global(.date-form-wrapper) {
        margin-bottom: 0.75rem;
    }
</style>
