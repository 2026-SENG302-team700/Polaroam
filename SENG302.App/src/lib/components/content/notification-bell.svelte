<script lang="ts">
    import { onMount } from "svelte";
    import NotificationList from "./notification-list.svelte";

    let toggled = $state(false);

    const NOTIFICATION_SHOWN_CAP = 5;

    // remove once persistance is done
    let testNotifications: NotificationData[] = [
        {
            title: "You've received a new trip invitation!",
            text: `You've been invited to join the trip "john burger"`,
            isRead: false,
            leftAction: {
                function: () => console.log("Accept"),
                text: "Accept",
                colour: "btn-success"
            },
            rightAction: { text: "Decline", function: () => console.log("Decline"), colour: "btn-danger" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "Grima Wormtounge",
            text: "Commented: That is a lie!",
            isRead: false,
            leftAction: { text: "Reply", function: () => console.log("Reply"), colour: "btn-primary" },
            rightAction: { text: "View", function: () => console.log("View"), colour: "btn-secondary" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "Grima Wormtounge",
            text: "Commented: That is a lie!",
            isRead: true,
            leftAction: { text: "Reply", function: () => console.log("Reply"), colour: "btn-primary" },
            rightAction: { text: "View", function: () => console.log("View"), colour: "btn-secondary" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "Grima Wormtounge",
            text: "Commented: That is a lie!",
            isRead: false,
            leftAction: { text: "Reply", function: () => console.log("Reply"), colour: "btn-primary" },
            rightAction: { text: "View", function: () => console.log("View"), colour: "btn-secondary" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "Grima Wormtounge",
            text: "Commented: That is a lie!",
            isRead: true,
            leftAction: { text: "Reply", function: () => console.log("Reply"), colour: "btn-primary" },
            rightAction: { text: "View", function: () => console.log("View"), colour: "btn-secondary" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "You've received a new trip invitation!",
            text: `You've been invited to join the trip "john burger"`,
            isRead: false,
            leftAction: { text: "Accept", function: () => console.log("Accept"), colour: "btn-success" },
            rightAction: { text: "Decline", function: () => console.log("Decline"), colour: "btn-danger" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "Grima Wormtounge",
            text: "Commented: That is a lie!",
            isRead: false,
            leftAction: { text: "Reply", function: () => console.log("Reply"), colour: "btn-primary" },
            rightAction: { text: "View", function: () => console.log("View"), colour: "btn-secondary" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "Grima Wormtounge",
            text: "Commented: That is a lie!",
            isRead: true,
            leftAction: { text: "Reply", function: () => console.log("Reply"), colour: "btn-primary" },
            rightAction: { text: "View", function: () => console.log("View"), colour: "btn-secondary" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "Grima Wormtounge",
            text: "Commented: That is a lie!",
            isRead: false,
            leftAction: { text: "Reply", function: () => console.log("Reply"), colour: "btn-primary" },
            rightAction: { text: "View", function: () => console.log("View"), colour: "btn-secondary" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData,
        {
            title: "Grima Wormtounge",
            text: "Commented: That is a lie!",
            isRead: true,
            leftAction: { text: "Reply", function: () => console.log("Reply"), colour: "btn-primary" },
            rightAction: { text: "View", function: () => console.log("View"), colour: "btn-secondary" },
            readToggle: () => console.log("toggle read"),
        } as NotificationData
    ];
    // end of temp blockk
    
    let notificationListElement: HTMLDivElement | null = $state(null);
    let notificationButtonElement: HTMLButtonElement | null = $state(null);

    /**
     * Refresh's the notification list
     */
    function refreshNotifications() {
        // to be implemented
        return;
    }
    
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
        class="notification-bell-button"
        aria-label="notifications"
        bind:this={notificationButtonElement}
        onclick={() => {
            toggled = !toggled;
        }}
    >
        <i class="bi bi-bell icon-centered"></i>
        {#if testNotifications.length > 0}
            <div class="notification-counter">
                {testNotifications.length > 9
                    ? "9+"
                    : testNotifications.length.toString()}
            </div>
        {/if}
    </button>

    <div
        class="notification-list-parent"
        hidden={!toggled}
        bind:this={notificationListElement}
    >
        <div class="notification-topbar">
            <span class="notification-title">Notifications</span>
            <div class="notification-topbar-buttons">
                <button
                        class="btn notification-topbar-button"
                        aria-label="refresh notifications"
                        onclick={refreshNotifications}
                >
                    <i class="bi bi-arrow-clockwise" />
                </button>
                <button
                        class="btn notification-topbar-button"
                        aria-label="exit"
                        onclick={() => {
                            toggled = false;
                        }}
                >
                    <i class="bi bi-x" />
                </button>
            </div>
        </div>
        
        <NotificationList notifications={testNotifications.slice(0, NOTIFICATION_SHOWN_CAP)} />

        {#if testNotifications.length > NOTIFICATION_SHOWN_CAP}
            <span class="see-more">See more...</span>
        {/if}
    </div>
</div>

<style>
    .notification-list-parent {
        width: 23rem;
        max-height: 80vh;
        display: flex;
        flex-direction: column;
        position: absolute;
        right: -14px;
        top: 47px;
        background-color: #ffffff;
        border: 3px solid #000000;
        border-top: 0;
        border-radius: 10px;
        border-top-left-radius: 0;
        border-top-right-radius: 0;
        overflow-y: scroll;
        z-index: 10;
    }

    .notification-main {
        position: relative;
    }

    .see-more {
        text-align: center;
        padding: 0.5rem 0;
        color: #1f1f1f;
    }

    .icon-centered {
        width: 100%;
        height: 100%;
        font-size: 1.4rem;
    }

    .notification-bell-button {
        background-color: white;
        width: 40px;
        height: 40px;
        border-radius: 0.25rem;
        align-content: center;
        border: 2px solid black;
        box-shadow: 3px 3px 0 #1f1f1f;
        transition:
                transform 120ms ease
                box-shadow 120ms ease
                background-color 120ms ease;
    }
    
    .notification-bell-button:hover {
        background-color: rgb(230, 228, 228);           
        border-color: #1f1f1f;
        transform: translate(1px, 1px);
        box-shadow: 2px 2px 0 #1f1f1f;
        transition:
                transform 120ms ease
                box-shadow 120ms ease
                background-color 120ms ease;
    }
    
    .notification-bell-button:active, .notification-bell-button:focus:active {
        background-color: rgb(207, 207, 207);
        transform: translate(3px, 3px);
        box-shadow:
        0px 0px 0 #1f1f1f,
        1px 1px 0 #1f1f1f inset;
        transition:
                transform 120ms ease
                box-shadow 120ms ease
                background-color 120ms ease;
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

    .notification-topbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin: 0.5rem;
    }
    
    .notification-topbar-buttons {
        display: flex;
        gap: 5px;
    }
    
    .notification-title {
        height: 100%;
        font-weight: 500;
        font-size: calc(1.275rem + 0.3vw);
    }
</style>
