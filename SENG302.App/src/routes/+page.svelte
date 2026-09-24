<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import LandingLogo from "$lib/components/logo/landing-logo.svelte";
</script>

<svelte:head>
    <title>Welcome | Polaroam</title>
</svelte:head>

<div class="landing-page-flex">
    <div class="landing-page-wrapper">
        <LandingLogo />
        <p class="subheader">
            <i>Your trips captured, collected and collaged.</i>
        </p>
        <div class="post-form button-wrapper">
            <div class="post-form-actions">
                <NavigationButton path="/login" text="Login" />
                <NavigationButton path="/register" text="Register" />
            </div>
        </div>
    </div>
</div>

<style>
    .landing-page-flex {
        min-height: 90vh;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .landing-page-wrapper {
        width: 70%;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 60px;
        flex-shrink: 1;
    }
    .subheader {
        font-size: max(18px, 1.3vw);
        margin: 0;
    }
    .post-form-actions {
        margin: 0;
        padding: 10px;
        width: max(300px, 30vw);
    }
</style>
