<script lang="ts">
    let { trigger = $bindable(false), text = "", onClick = () => {} } = $props();
</script>

<button class="btn btn-primary" onclick={onClick}>
    {text}
</button>
