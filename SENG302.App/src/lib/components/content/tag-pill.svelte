<script lang="ts">
    let { 
        tag = "",
        tagColour = "secondary"
    }: {
        tag: string;
        tagColour?: string;
    } = $props();
</script>

<span class="badge rounded-pill text-bg-{tagColour}">{tag}</span>