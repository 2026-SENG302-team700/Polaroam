<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";

</script>

<div onclick={() => goto(resolve("/home"))}>
        <h2 class="logo">Polaroam</h2>
</div>

<style>
	.logo {
		font-family: "Syne",sans-serif;
		font-weight: 800;
		text-align: center;
		margin-bottom: 0;
		margin-left: 5px;
		margin-right: 10px;
		cursor: pointer;
	}
</style>