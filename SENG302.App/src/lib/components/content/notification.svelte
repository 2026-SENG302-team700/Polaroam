<script lang="ts">
    import type {Snippet} from "svelte";

    let {
        title,
        description,
        children
    }: {
        title: string;
        description: string;
        children?: Snippet;
    } = $props();
</script>

<div class="notification-wrapper">
    <span class="notification-title">{title}</span>
    <span class="notification-description">{description}</span>
    <div class="notification-actions">
        {@render children?.()}
    </div>
</div>

<style>
    .notification-wrapper {
        margin-top: 2%;
        margin-bottom: 2%;
    }
    
    .notification-actions {
        display: flex;
    }
    
    .notification-title {
        font-weight: bold;
        display: block;
    }
</style>