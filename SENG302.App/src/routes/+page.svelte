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
        <p class="subheader">
            <i>Your trips captured, collected and collaged.</i>
        </p>
        <div class="post-form button-wrapper">
            <div class="post-form-actions">
                <NavigationButton path="/login" text="Login" />
                <NavigationButton path="/register" text="Register" />
            </div>
        </div>
        <div class="selling-points">
                <div class="section-divider"></div>
                <div class="topic-grid">
                    <div class="info-box card">
                        <p class="header">
                            Organise your memories into dedicated Trips.
                        </p>
                        <p class="content">
                            Organise all of your photos, posts and maps into a
                            single trip for easy access to each holiday!
                        </p>
                    </div>
                    <div class="info-box card">
                        <p class="header">
                            Highlight specific moments, locations and photos
                            with Posts.
                        </p>
                        <p class="content">
                            Visited the Eiffel Tower, or just landed in Paris?
                            Create a post to curate photos, document your
                            adventures and pin locations on a map!
                        </p>
                    </div>
                    <div class="info-box card">
                        <p class="header">
                            Search through your photos using our AI Image
                            Tagging System.
                        </p>
                        <p class="content">
                            Use our AI system to intelligently tag your images
                            as you upload them so that you can search for the
                            Eiffel Tower by searching for the Eiffel Tower!
                        </p>
                    </div>
                    <div class="info-box card">
                        <p class="header">
                            Create collages of your favourite memories with
                            Scrapbooks.
                        </p>
                        <p class="content">
                            Create collages of all of your favourite moments
                            from each trip, alongside a suite of creative tools
                            to add the finishing touches to your memory.
                        </p>
                    </div>
                </div>
            </div>
    </div>
</main>

<style>
    .landing-page-flex {

        margin-top: 60px;
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

    .section-divider {
        width: 100%;
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
    .selling-points {
        width: 80%;
        display: flex;
        flex-direction: column;
        align-items: center;
    }
    .topic-grid {
        width: 80%;
        display: grid;
        grid-template-columns: 50% 50%;
        gap: 10px;
        margin-bottom: 10px;
    }
    .info-box {
        padding: 5px;
    }
    .header {
        font-size: 0.8vw;
        font-weight: 500;
    }
</style>
