<script lang="ts">
	import "bootstrap/dist/css/bootstrap.min.css";
	import "bootstrap-icons/font/bootstrap-icons.css";
	import "../app.css";
	import Toast from "$lib/toast/toast.svelte";
	import { onMount } from "svelte";
	import { page } from "$app/state";
    import NavBarMenuButton from "$lib/components/buttons/nav-bar-menu-button.svelte";
	import maplibregl from 'maplibre-gl';

	interface Props { children?: import("svelte").Snippet }

	let { children }: Props = $props();

	maplibregl.workerUrl = new URL(
			'maplibre-gl/dist/maplibre-gl-csp-worker.js',
			import.meta.url
	).toString();

	// added to ensure we can make use of bootstrap js for things like dropdowns
	onMount(async () => {
		// @ts-ignore
		await import("bootstrap");
	});
</script>

<Toast />

{#key page.url.pathname}
	<div
		class="container-fluid bg-light min-vh-footer py-2"
	>{@render children?.()}</div>

	<footer class="footerBar menu-bar ">
		<div class="footerContent display: flex; flex-direction: row; align-items: center; padding-top: 5px; "
    >
			<NavBarMenuButton path="/policies/content-policy" text= {"Content Policy"}/>
			<NavBarMenuButton path="/policies/privacy-policy" text= {"Privacy Policy"}/>
			<NavBarMenuButton path="/policies/ai-privacy-policy" text= {"AI Privacy Policy"}/>
		</div>
	</footer>
{/key}

<style>
	.min-vh-footer {
		min-height: 93vh;
	}

	.footerBar {
		margin-left: 12px;
		margin-right: 12px;
		margin-top: auto;
		margin-bottom: 12px;
		background-color: white;
		height: 60px;
		border-radius: 10px;
		display: flex;
		align-items: center;
	}

	.footerContent {
		display: flex;
		gap: 12px;
		padding: 0 15px;
		align-items: center;
	}
</style>
