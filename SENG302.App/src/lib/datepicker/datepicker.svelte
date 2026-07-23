<script lang="ts">
    import { onMount } from "svelte";

    let {
        value = $bindable<Date | null>(null),
        error = "",
        disabled = false,
    }: {
        value: Date | null;
        error?: string;
        disabled?: boolean;
    } = $props();

    let datePicker: HTMLInputElement | undefined = $state();

    onMount(() => {
        if (datePicker)
            datePicker.addEventListener("invalid", function (event) {
                event.preventDefault();
                error = "Please input a valid date";
            });
    });

    /**
     * Converts a Date object to a string in the format "YYYY-MM-DD"
     * @param date
     */
    function toInputDate(date: Date | null): string {
        if (!date || Number.isNaN(date.getTime())) return "";

        const year = date.getFullYear().toString().padStart(4, "0");
        const month = (date.getMonth() + 1).toString().padStart(2, "0");
        const day = date.getDate().toString().padStart(2, "0");

        return `${year}-${month}-${day}`;
    }

    /**
     * Handles the input event for the date picker updating the value with a new Date object based on the input value.
     * @param event
     */
    function handleInput(event: Event) {
        const input = event.currentTarget as HTMLInputElement;

        value = input.value ? new Date(`${input.value}T00:00:00`) : null;
    }
</script>

<div class="date-form-wrapper input-group w-auto">
    <input
        type="date"
        class="form-control"
        class:is-invalid={error}
        value={toInputDate(value)}
        oninput={handleInput}
        bind:this={datePicker}
        {disabled}
    />
    <button
        type="button"
        class="btn btn-outline-secondary"
        {disabled}
        onclick={() => {
            value = null;
        }}>Clear</button
    >
</div>

<style>
    .date-form-wrapper {
        display: flex;
        align-items: center;
        gap: 2px;
        margin-bottom: 20px;
    }
</style>
