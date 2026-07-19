<script lang="ts">
    let { trigger = $bindable(false), text = "" } = $props();
</script>

<button class="btn btn-primary" onclick={() => trigger = true}>
    {text}
</button>
