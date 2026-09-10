<script lang="ts">
    import { onMount } from "svelte";
    import NotificationList from "./notification-list.svelte";

    let toggled = $state(false);

    let {
        notifications = [],
    }: {
        notifications: NotificationData[];
    } = $props();

    let notificationListElement: HTMLDivElement | null = $state(null);
    let notificationButtonElement: HTMLButtonElement | null = $state(null);

    onMount(() => {
        document.addEventListener("click", (e) => {
            if (
                notificationListElement == null ||
                notificationButtonElement == null
            ) {
                throw new Error("Cannot find the required elements");
            }
            const clickedInsideList = e
                .composedPath()
                .includes(notificationListElement);

            const clickedOnNotification = e
                .composedPath()
                .includes(notificationButtonElement);

            if (!clickedInsideList && !clickedOnNotification) {
                toggled = false;
            }
        });
    });
</script>

<div class="notification-main">
    <button
        class="notification-parent nav-link-button-colours"
        aria-label="notifications"
        bind:this={notificationButtonElement}
        onclick={() => {
            toggled = !toggled;
        }}
    >
        <i class="bi bi-bell icon-centered"></i>
        {#if notifications.length > 0}
            <div class="notification-counter">
                {notifications.length > 9
                    ? "9+"
                    : notifications.length.toString()}
            </div>
        {/if}
    </button>

    <div
        class="notification-list-parent"
        hidden={!toggled}
        bind:this={notificationListElement}
    >
        <NotificationList {notifications}></NotificationList>
    </div>
</div>

<style>
    .notification-list-parent {
        position: absolute;
        padding: 5px 5px;
        right: -5px;
        top: 47px;
        background-color: white;
        border: 3px solid black;
        border-top: 0;

        border-radius: 10px;
        border-top-left-radius: 0;
        border-top-right-radius: 0;
        z-index: 10;
    }

    .notification-main {
        position: relative;
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

    .notification-counter {
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
