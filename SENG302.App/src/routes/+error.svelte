<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
</script>

<div class="container text-center py-5">
    <h1>{page.status}</h1>

    {#if page.status === 404}
        <h2>Page not found</h2>
        <p>The page may not exist, or it may have been deleted.</p>
    {:else}
        <h2>Something went wrong</h2>
        <p>{page.error?.message ?? "An unexpected error occurred."}</p>
    {/if}

    <button
        type="button"
        class="btn btn-primary"
        onclick={() => goto(resolve("/home"))}
    >
        Return home
    </button>
</div>
