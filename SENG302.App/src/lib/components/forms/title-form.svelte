<script>
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    let {
        name = $bindable(""),
        errors = [],
        loading = false,
        type = "task",
        
    } = $props();
</script>

<div class="mb-3">
    <input
        type="text"
        class="form-control"
        class:is-invalid={errors.length > 0}
        placeholder={type === "task" || type == "trip" ? "Title *" : "Name *"}
        bind:value={name}
        disabled={loading}
    />
    <ErrorDisplay {errors} />
</div>
