<script lang="ts">
    let {
        errors = [],
        lines = 5,
    }: {
        errors: string[];
        lines: number;
    } = $props();

    let expanded = $state(false);

    let nonEmptyErrors = $derived(errors.filter((err) => err && err.trim().length > 0));
    let hiddenErrorCount = $derived(Math.max(0, nonEmptyErrors.length - lines));
    let displayErrors = $derived(expanded ? nonEmptyErrors : nonEmptyErrors.slice(0, lines));
</script>

<div>
    {#if nonEmptyErrors.length > 0}
        <ul class="invalid-feedback d-block mb-2 ps-3">
            {#each displayErrors as err}
                <li>{err}</li>
            {/each}
            {#if hiddenErrorCount > 0}
                <li class="list-unstyled">
                    <button
                            type="button"
                            class="error-toggle"
                            onclick={() => (expanded = !expanded)}
                    >
                        {#if expanded}
                            Show fewer errors
                        {:else}
                            + {hiddenErrorCount} more error{hiddenErrorCount === 1 ? "" : "s"}
                        {/if}
                    </button>
                </li>
            {/if}
        </ul>
    {/if}
</div>

<style>
    .error-toggle {
        background: none;
        border: none;
        padding: 0;
        margin: 0;
        font: inherit;
        color: inherit;
        text-decoration: underline;
        cursor: pointer;
    }
</style>