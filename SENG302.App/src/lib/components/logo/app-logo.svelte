<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import Polaroam from "$lib/assets/images/Polaroam.svg";

    function goHome() {
        goto(resolve("/home"));
    }
</script>

<button type="button" class="home-button" onclick={goHome}>
    <img src={Polaroam} class="logo" alt="logo" />
    <div class="vertical-separator"></div>
    <h5 class="text" style="" >My Trips</h5>
</button>

<style>
    .vertical-separator {
        background-color: red;
        border-left: 2px solid black;
        margin: 0px 5px;
        height: 35px;
    }
    
    .home-button {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        background: none;
        border: none;
        padding: 0;
        cursor: pointer;
        margin-left: 15px;
    }

    .logo {
        max-width: 276px;
        height: auto;
        min-width: 0;
    }

    .text {
        margin-bottom: 0;
        padding-bottom: 0;
    }

    .home-button:focus-visible {
        outline: 2px solid var(--bs-primary, #0d6efd);
        outline-offset: 2px;
    }

    @media (min-width: 518px) {
        .home-button {
            margin-right: 25px;
        }
    }
</style>