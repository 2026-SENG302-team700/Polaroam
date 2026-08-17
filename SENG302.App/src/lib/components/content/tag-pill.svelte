<script lang="ts">
    let {
        tag = "",
        tagColour = "secondary",
    }: {
        tag: string;
        tagColour?: string;
    } = $props();
</script>

<span class="badge rounded-pill text-bg-{tagColour}">
    {tag}
    <button class="button-x" aria-label="Delete Tag">
        <i class="bi bi-x"> </i>
    </button>
</span>

<style>
    .button-x {
        border: 0;
        background: none;
        margin: 0;
        padding: 0;
    }
</style>
