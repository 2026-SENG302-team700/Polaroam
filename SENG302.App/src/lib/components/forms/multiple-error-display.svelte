<script>
    let { errors = [] } = $props();

    let nonEmptyErrors = $derived(errors.filter((err) => err && err.trim().length > 0));
</script>

<div>
    {#if nonEmptyErrors.length > 0}
        <ul class="invalid-feedback d-block mb-0 ps-3">
            {#each nonEmptyErrors as err}
                <li>{err}</li>
            {/each}
        </ul>
    {/if}
</div>
