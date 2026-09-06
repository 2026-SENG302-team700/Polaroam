<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import LandingLogo from "$lib/components/logo/landing-logo.svelte";
</script>

<svelte:head>
    <title>Welcome | Polaroam</title>
</svelte:head>

<main class="landing-page-flex">
    <div class="landing-page-wrapper">
        <LandingLogo />
        <div class="card text-center landing-card" style="font-size:x-large;">
            <div class="card-header">Welcome to Polaroam</div>
            <div
                class="card-body"
                style="display: grid; grid-template-columns: auto; row-gap: 10px;"
            >
                <NavigationButton path="/register" text="Register" />
                <NavigationButton path="/login" text="Log In" />
            </div>
        </div>
    </div>
</main>

<style>
    .landing-page-flex {
        height: 80vh;
        display: flex;
        justify-content: center;
        align-items: center;
    }
    .landing-card {
        width: 90vw;
    }

    @media (min-width: 576px) {
        .landing-card {
            width: 70vw;
        }
    }

    @media (min-width: 768px) {
        .landing-card {
            width: 50vw;
        }
    }

    @media (min-width: 992px) {
        .landing-card {
            width: 30vw;
        }
    }
</style>
