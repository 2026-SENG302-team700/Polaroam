<script>
    let {description = $bindable(""), error = "", loading = false } = $props();
</script>


<textarea
        class="form-control mb-3"
        class:is-invalid={error}
        placeholder="Description"
        bind:value={description}
        disabled={loading}
        
></textarea>
{#if error}
    <div class="invalid-feedback">
        {error}
    </div>
{/if}
