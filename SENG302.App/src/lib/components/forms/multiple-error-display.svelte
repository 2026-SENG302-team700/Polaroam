<script>
    let { errors = [] } = $props();

    let nonEmptyErrors = $derived(
        errors.length > 0
            ? errors.filter((err) => err && err.trim().length > 0)
            : [],
    );
</script>

<div>
    {#if nonEmptyErrors.length > 0}
        <div class="invalid-feedback d-block mb-0">
            {#each nonEmptyErrors as err}
                <div>{err}</div>
            {/each}
        </div>
    {/if}
</div>
