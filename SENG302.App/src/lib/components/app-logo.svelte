<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
	import Polaroam from "$lib/assets/images/Polaroam.png"

</script>

<div onclick={() => goto(resolve("/home"))}>
	<img src={Polaroam}  class="logo"/>
</div>

<style>
	.logo{
		margin-left: 15px;
		margin-right: 25px;
		height: 25px;
		cursor: pointer;
	}
</style>