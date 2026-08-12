<script lang="ts">
    import type { MouseEventHandler } from "svelte/elements";

    interface Props {
        trigger?: boolean;
        text: string;
        onClick?: MouseEventHandler<HTMLButtonElement>;
        loading: boolean
    }

    let {
        trigger = $bindable(false),
        text = "",
        onClick = () => {},
        loading = $bindable(),
    }: Props = $props();
</script>

<button class="btn btn-primary" onclick={onClick} disabled={loading}>
    {text}
</button>
