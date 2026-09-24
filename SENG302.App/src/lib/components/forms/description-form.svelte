<script>
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    let {
        description = $bindable(""),
        errors = [],
        loading = $bindable(false),
        placeholder = "Description",
        maxLength = 2048,
    } = $props();
</script>

<div class="mb-4">
    <textarea
        class="form-control char-counter-space"
        class:is-invalid={errors.length > 0 ||
            description.trim().length > maxLength}
        style="field-sizing: content; resize: none;"
        {placeholder}
        bind:value={description}
        disabled={loading}
    >
    </textarea>
    <span
        class="max-length-text"
        class:is-invalid={description.trim().length > maxLength}
        >{`${description.trim().length} / ${maxLength}`}</span
    >

    <ErrorDisplay {errors} />
</div>

<style>
    .max-length-text {
        position: relative;
        float: right;
        bottom: -2px;
        right: 0px;
    }

    .max-length-text.is-invalid {
        color: red;
    }
</style>
