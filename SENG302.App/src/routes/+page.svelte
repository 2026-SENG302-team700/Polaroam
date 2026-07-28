<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
</script>

<svelte:head>
    <title>Welcome | Polaroam</title>
</svelte:head>

<main class="landing-page-wrapper">
    <div class="card text-center" style="width: 30vw; font-size:x-large;">
        <div class="card-header">Welcome to Polaroam</div>
        <div
            class="card-body"
            style="display: grid; grid-template-columns: auto; row-gap: 10px;"
        >
            <NavigationButton path="/register" text="Register" />
            <NavigationButton path="/login" text="Log In" />
        </div>
    </div>
</main>

<style>
    .landing-page-wrapper {
        height: 80vh;
        display: flex;
        justify-content: center;
        align-items: center;
    }
</style>
