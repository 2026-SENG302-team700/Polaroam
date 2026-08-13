<script>
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    let {description = $bindable(""), errors = [], loading = $bindable(false), placeholder="Description" } = $props();
</script>

<div class="mb-3">
    <textarea
        class="form-control"
        class:is-invalid={errors.length > 0}
        style="field-sizing: content;"
        {placeholder}
        bind:value={description}
        disabled={loading}
    >
    </textarea>
    <ErrorDisplay {errors} />
</div>
