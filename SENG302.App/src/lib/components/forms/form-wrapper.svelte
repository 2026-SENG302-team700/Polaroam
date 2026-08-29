<script lang="ts">
    import type { Snippet } from "svelte";

    let {
        heading,
        body,
        footer,
    }: {
        heading: string;
        body: Snippet;
        footer?: Snippet;
    } = $props();
</script>

<div class="container post-form-page">
    <div class="post-form">
        <div class="post-form-header">
            <span class="header-icon bi-journal-richtext"></span>
            <h1>{heading}</h1>
        </div>

        <div class="post-form-body">
            {@render body()}
            {#if footer != null}
                <div class="section-divider"></div>
                {@render footer()}
            {/if}
        </div>
    </div>
</div>
