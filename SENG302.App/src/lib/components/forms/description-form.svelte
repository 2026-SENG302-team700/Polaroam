<script>
    let {description = $bindable(""), error = "", loading = false, placeholder="Description" } = $props();
</script>


<textarea
        class="form-control mb-3"
        class:is-invalid={error}
        {placeholder}
        bind:value={description}
        disabled={loading}
        
></textarea>
{#if error}
    <div class="invalid-feedback">
        {error}
    </div>
{/if}
