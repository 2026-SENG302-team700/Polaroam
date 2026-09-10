<script lang="ts">
    import NotificationList from "./notification-list.svelte";

    let toggled = $state(false);

    let {
        notificationCount = 0,
    }: {
        notificationCount: number;
    } = $props();
</script>

<div>
    <button
        class="notification-parent nav-link-button-colours"
        aria-label="notifications"
        onclick={() => {
            toggled = !toggled;
        }}
    >
        <i class="bi bi-bell icon-centered"></i>
        {#if notificationCount > 0}
            <div class="notificationCounter">
                {notificationCount > 9 ? "9+" : notificationCount.toString()}
            </div>
        {/if}
    </button>
    {#if toggled || true}
        <div class="notification-list-parent">
            <NotificationList></NotificationList>
        </div>
    {/if}
</div>

<style>
    .notification-list-parent {
        display: none;
        position: absolute;
        padding: 5px 5px;
    }

    .icon-centered {
        width: 100%;
        height: 100%;
        font-size: 1.4rem;
    }

    .notification-parent {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        align-content: center;
        border: 2px solid black;
    }

    .notificationCounter {
        position: absolute;
        transform: translate(15px, -40px);
        background-color: red;
        color: white;
        border-radius: 50%;
        width: 22px;
        height: 22px;
        font-size: smaller;
    }

    .nav-link-button-colours {
        background-color: white;
    }

    .nav-link-button-colours:hover {
        background-color: rgb(230, 228, 228);
    }

    .nav-link-button-colours:active {
        background-color: rgb(207, 207, 207);
    }
</style>
