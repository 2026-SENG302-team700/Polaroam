<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";


    let { 
        path,
        class: className = ""
        }: { 
        path: string
        class?: string
    } = $props();
</script>

<button
    type="button"
    class="btn btn-primary bi-pencil {className}"
    title="Edit"
    aria-label="Edit"
    onclick={() => goto(resolve(path))}
>
</button>
