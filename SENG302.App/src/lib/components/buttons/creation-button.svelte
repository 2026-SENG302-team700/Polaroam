<script lang="ts">
    import type { MouseEventHandler } from "svelte/elements";

    let { trigger = $bindable(false), text = "", onClick = () => {} } :
        {onClick : MouseEventHandler<HTMLButtonElement>, text : string, trigger? : any}
        = $props();
</script>

<button class="btn btn-primary" onclick={onClick}>
    {text}
</button>
