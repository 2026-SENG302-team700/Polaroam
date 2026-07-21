<script>
    let { errors = [] } = $props();
</script>

<div>
    {#if errors.length > 0}
        <div class="invalid-feedback d-block">
            {#each errors as err}
                <div>{err}</div>
            {/each}
        </div>
    {/if}
</div>
