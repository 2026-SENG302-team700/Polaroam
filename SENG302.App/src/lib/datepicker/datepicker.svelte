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
        value={value?.toISOString()}
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

{#if error}
    <div class="invalid-feedback d-block date-error">
        {error}
    </div>
{/if}

<style>
    .date-form-wrapper {
        display: flex;
        align-items: center;
        gap: 2px;
        margin-bottom: 20px;
    }
</style>
