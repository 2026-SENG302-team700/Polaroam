<script lang="ts">
    import { onMount } from "svelte";

    let {
        value = $bindable<Date | null>(null),
        error = "",
        disabled = false,
    }: {
        value: Date;
        error?: string;
        disabled?: boolean;
    } = $props();

    let datePicker: HTMLInputElement | undefined = $state();
    
    let inputValue = $state(value ? value.toISOString().slice(0, 10) : "");
    
    onMount(() => {
        if (datePicker)
            datePicker.addEventListener("invalid", function (event) {
                event.preventDefault();
                error = "Please input a valid date";
            });
    });
</script>

<div class="date-form-wrapper input-group w-auto">
    <input
            type="date"
            class="form-control"
            class:is-invalid={error}
            bind:value={inputValue}
            bind:this={datePicker}
            {disabled}
    />
    <button
            type="button"
            class="btn btn-outline-secondary"
            {disabled}
            onclick={() => (value = new Date("9999-01-01"))}>Clear</button
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
