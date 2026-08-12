<script lang="ts">
    import CancelButton from "$lib/components/buttons/cancel-button.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import Datepicker from "$lib/datepicker/datepicker.svelte";
    import DescriptionForm from "./description-form.svelte";
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
        submitText,
        cancelPath,
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

<div class="container max-w-md">
    <div class="form-header">
        <CancelButton path={cancelPath} />
        <h1 class="text-center mb-4">{heading}</h1>
    </div>

    <form>
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

        <div class="flex justify-end">
            <CreationButton onClick={onSubmit} text={submitText} loading={loading}/>
        </div>
    </form>
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

    .date-error {
        margin-top: -0.75rem;
        margin-bottom: 0.75rem;
    }

    .date-picker-wrapper :global(.date-form-wrapper) {
        margin-bottom: 0.75rem;
    }
</style>
