<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    let { path = `/` } = $props();
</script>

<button class="btn btn-primary bi-plus" onclick={() => goto(resolve(path))} />
