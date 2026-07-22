<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { Pencil } from "@lucide/svelte";

    let { path }: { path: string } = $props();
</script>

<button
    type="button"
    class="btn btn-primary"
    title="Edit"
    aria-label="Edit"
    onclick={() => goto(resolve(path))}
>
    <Pencil size={18} />
</button>
