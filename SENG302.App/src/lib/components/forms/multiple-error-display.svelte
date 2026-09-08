<script>
    let { errors = [] } = $props();
    let nonEmptyErrors = $derived(
        errors.filter((err) => err && err.trim().length > 0),
    );
</script>

<div>
    {#if nonEmptyErrors.length > 0}
        {#if nonEmptyErrors.length > 1}
            <div class="invalid-feedback d-block mb-0">
                {#each nonEmptyErrors as err}
                    <div>{err}</div>
                {/each}
            </div>
        {:else}
            <div class="invalid-feedback d-block mb-0">
                <div>{nonEmptyErrors[0]}</div>
            </div>
        {/if}
    {/if}
</div>
