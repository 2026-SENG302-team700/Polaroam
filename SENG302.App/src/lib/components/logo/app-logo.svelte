<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import Polaroam from "$lib/assets/images/Polaroam.svg";
</script>

<img
    src={Polaroam}
    class="logo"
    onclick={() => goto(resolve("/home"))}
    alt="logo"
/>

<style>
    .logo {
        margin-left: 15px;
        max-width: 276px;
        height: auto;
        min-width: 0;
        cursor: pointer;
    }
    @media (min-width: 518px) {
        .logo {
            margin-right: 25px;
        }
    }
</style>
