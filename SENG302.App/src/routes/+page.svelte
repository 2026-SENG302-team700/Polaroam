<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import LandingLogo from "$lib/components/logo/landing-logo.svelte";
</script>

<svelte:head>
    <title>Welcome | Polaroam</title>
</svelte:head>

<main class="landing-page-flex">
    <div class="landing-page-wrapper post-form">
        <LandingLogo />
        <p class="subheader">
            <i>Your trips captured, collected and collaged.</i>
        </p>
        <div class="post-form-actions">
            <NavigationButton path="/login" text="Login" />
            <NavigationButton path="/register" text="Register" />
        </div>
        <div class="selling-points">
            <div class="post-form-footer">
                <div class="section-divider"></div>
                <p>
                    hello hello hello hello hello hello hello hello hello hello
                    hello hello hello hello hello hello hello hello hello hello
                    hello hello hello hello hello hello hello hello hello hello
                    hello hello hello hello hello hello hello hello hello hello
                    hello hello hello hello hello hello hello hello hello hello
                    hello
                </p>
            </div>
        </div>
    </div>
</main>

<style>
    .landing-page-flex {
        height: 80vh;
        margin-top: 60px;
    }
    .landing-page-wrapper {
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
        width: max(50%, 300px);
    }
    .selling-points {
        width: 80%;
    }
</style>
