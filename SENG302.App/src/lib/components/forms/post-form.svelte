<script lang="ts">
    import type { Snippet } from "svelte";

    let {
        heading,
        children,
    }: {
        heading: string;
        children: Snippet;
    } = $props();
</script>

<div class="container post-form-page">
    <div class="post-form">
        <div class="post-form-header">
            <span class="header-icon bi-journal-richtext"></span>
            <h1>{heading}</h1>
        </div>

        <div class="post-form-body">
            {@render children()}
        </div>
    </div>
</div>
