<script lang="ts">
    import CreationButton from "../buttons/creation-button.svelte";

    let scrapboardCreated: Boolean = false;
</script>

<div class="container">
    {#if !scrapboardCreated}
        <div class="scrapboard-image-wrapper empty-trip-page-element">
            <i class="bi bi-journal-album" aria-hidden="true"></i>
            <p>No Scrapboard yet.</p>
            <CreationButton
                text="Create Scrapboard"
                onClick={() => (scrapboardCreated = true)}
            />
            <!-- scrapboard created is a placeholder and please replace it with a call to the backend -->
        </div>
    {:else}
        <img
            src="../../../static/scrapboard-placeholder.png"
            alt="scrapboard"
            class="scrapboard-image card"
        />
    {/if}
</div>

<style>
    .scrapboard-image {
        width: 100%;
    }
</style>
