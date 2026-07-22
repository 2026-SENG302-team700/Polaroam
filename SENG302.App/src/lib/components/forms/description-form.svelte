<script>
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    let {description = $bindable(""), errors = [], loading = false } = $props();
</script>


<textarea
        class="form-control mb-3"
        class:is-invalid={errors.length > 0}
        placeholder="Content"
        bind:value={description}
        disabled={loading}
        
></textarea>
<ErrorDisplay errors={errors} />
