<script lang="ts">
    import type { MouseEventHandler } from "svelte/elements";

    interface Props {
        trigger?: boolean;
        text: string;
        onClick?: MouseEventHandler<HTMLButtonElement>;
    }

    let {
        trigger = $bindable(false),
        text = "",
        onClick = () => {},
    }: Props = $props();
</script>

<button class="btn btn-primary" onclick={onClick}>
    {text}
</button>
