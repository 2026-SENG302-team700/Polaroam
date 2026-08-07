<script>
    let { errors = [] } = $props();
    let nonEmptyErrors = $derived(errors.filter((err) => err && err.trim().length > 0));
</script>

<div>
    {#if nonEmptyErrors.length > 0}
        {#if nonEmptyErrors.length > 1}
            <ul class="invalid-feedback d-block mb-0 ps-3">
                {#each nonEmptyErrors as err}
                    <li>{err}</li>
                {/each}
            </ul>
        {:else}
            <div class="invalid-feedback d-block mb-0">
                <span>{nonEmptyErrors[0]}</span>
            </div>
        {/if}
    {/if}
</div>
