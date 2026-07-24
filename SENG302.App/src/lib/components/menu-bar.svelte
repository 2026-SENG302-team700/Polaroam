<script lang="ts">

    interface Props {
        children?: import("svelte").Snippet;
    }
    let { children }: Props = $props();

</script>

<nav class="navBar menu-bar">
    <div
        style="display: flex; flex-direction: row; align-items: center; padding-top: 5px; "
    >
        {children}
    </div>
</nav>


<style>
    .profile-image {
        width: 100%;
        height: 100%;
        object-fit: cover;
        border-radius: 50%;
        border-color: black;
    }

    .profile-button {
        width: 40px;
        height: 40px;
        padding: 0;
        background: none;
        border-radius: 50%;
        margin-left: 5px;
        cursor: pointer;
    }

    .navBar {
        top: 0;
        z-index: 1000;
        vertical-align: top;
        background-color: white;
        height: 50px;
        margin-top: 0px;
        padding-top: 0px;
        
        border-radius: 10px;
        margin-bottom: 20px;
    }

    .page-wrapper {
        min-height: 80vh;
        display: flex;
        flex-direction: column;
    }
</style>
