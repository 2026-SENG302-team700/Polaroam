<script lang="ts">
    import MultipleErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import { onMount } from "svelte";

    let {
        value = $bindable<Date | null | undefined>(undefined),
        inputState = $bindable<DateInputState>("empty"),
        error = [],
        disabled = $bindable(false),
    }: {
        value: Date | null | undefined;
        inputState?: DateInputState;
        error?: string[];
        disabled?: boolean;
    } = $props();

    type DateInputState = "valid" | "invalid" | "empty";

    let datePicker: HTMLInputElement | undefined = $state();

    onMount(() => {
        if (datePicker)
            datePicker.addEventListener("invalid", function (event) {
                event.preventDefault();
            });
    });

    $effect(() => {
        if (!datePicker) return;

        if (inputState === "invalid") return;

        const target = value != null ? utcDateToLocalISO(value) : "";
        if (datePicker.value !== target) {
            datePicker.value = target;
        }
    });

    /**
     * Handles the input event for the date picker updating the value with a new Date object based on the input value.
     * The time of the new date object is set to 0:00
     * @param event
     */
    function handleInput(event: Event) {
        const input = event.currentTarget as HTMLInputElement;
        if (input.validity.badInput) {
            inputState = "invalid";
            value = null;
        } else if (input.value.length === 0) {
            inputState = "empty";
            value = undefined;
        } else {
            inputState = "valid";
            value = input.value ? new Date(`${input.value}T00:00:00`) : undefined;
        }
    }

    /**
     * Converts a utc date to a string formatted YYYY-MM-DD in the user's local time
     * @param date a date formatted YYYY-MM-DD in the user's local time
     */
    function utcDateToLocalISO(date: Date) {
        const year = date.getFullYear().toString().padStart(4, "0");
        const month = (date.getMonth() + 1).toString().padStart(2, "0");
        const day = date.getDate().toString().padStart(2, "0");

        const localDate = `${year}-${month}-${day}`;
        return localDate;
    }
</script>

<div class="date-form-wrapper">
    <div
        class="date-form-inputs input-group w-auto"
        class:has-error={Boolean(error)}
    >
        <input
            type="date"
            class="form-control"
            class:is-invalid={error.length > 0}
            oninput={handleInput}
            onchange={handleInput}
            onblur={handleInput}
            bind:this={datePicker}
            {disabled}
        />
        <button
            type="button"
            class="btn btn-outline-secondary"
            {disabled}
            onclick={() => {
                inputState = "empty";
                value = undefined;
                if (datePicker) {
                    datePicker.value = "";
                }
            }}>Clear</button
        >
    </div>
    {#if error.length > 0}
        <MultipleErrorDisplay errors={error} />
    {/if}
</div>

<style>
    .date-form-inputs {
        display: flex;
        align-items: center;
        gap: 2px;
    }
    .date-form-wrapper {
        margin-bottom: 20px;
    }

    .date-form-wrapper.has-error {
        margin-bottom: 0;
    }
</style>
