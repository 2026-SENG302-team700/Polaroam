<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    let { path = `/`, text = "" } = $props();
</script>

<button class="btn btn-primary" onclick={() => goto(resolve(path))}>
    {text}
</button>
