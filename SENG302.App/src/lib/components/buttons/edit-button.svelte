<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";


    let { path }: { path: string } = $props();
</script>

<button
    type="button"
    class="btn btn-primary bi-pencil"
    title="Edit"
    aria-label="Edit"
    onclick={() => goto(resolve(path))}
>
</button>
