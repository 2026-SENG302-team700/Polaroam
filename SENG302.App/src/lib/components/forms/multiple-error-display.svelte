<script>
    let { errors = [] } = $props();
<<<<<<< HEAD

    let nonEmptyErrors = $derived(
        errors.length > 0
            ? errors.filter((err) => err && err.trim().length > 0)
            : [],
    );
=======
    let nonEmptyErrors = $derived(errors.filter((err) => err && err.trim().length > 0));
>>>>>>> team-700/dev
</script>

<div>
    {#if nonEmptyErrors.length > 0}
<<<<<<< HEAD
        <div class="invalid-feedback d-block mb-0">
            {#each nonEmptyErrors as err}
                <div>{err}</div>
            {/each}
        </div>
=======
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
>>>>>>> team-700/dev
    {/if}
</div>
