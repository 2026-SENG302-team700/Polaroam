<script lang="ts">
    import type { Snippet } from "svelte";

    let {
        header,
        body,
        footer,
    }: {
        header: Snippet;
        body: Snippet;
        footer?: Snippet;
    } = $props();
</script>

<div class="container post-form-page">
    <div class="post-form">
        <div class="post-form-header">
            {@render header()}
        </div>

        <div class="post-form-body">
            {@render body()}
            {#if footer != null}
                <div class="section-divider"></div>
                <div class="post-form-footer">
                    {@render footer()}
                </div>
            {/if}
        </div>
    </div>
</div>
