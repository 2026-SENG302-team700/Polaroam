<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    let { path = `/`, text = "" } = $props();
</script>

<button class="nav-link active" onclick={() => goto(resolve(path))}>
    <h5 style="margin-bottom: 0;">
        {text}
    </h5>
</button>

<style>
    .nav-link {
        text-align: center;
    }
</style>
