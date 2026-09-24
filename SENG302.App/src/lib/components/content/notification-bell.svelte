<script lang="ts">
    import { onMount } from "svelte";
    import NotificationList from "./notification-list.svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";

    let toggled = $state(false);

    const NOTIFICATION_SHOWN_CAP = 5;

    let notifications: NotificationData[] = $state([]);
    let notificationUnreadCount: number = $state(0);
    let moreNotificationsExist: boolean = $state(false);
    let notificationsError: string = $state("");

    let offset: number = $state(0);

    let notificationListElement: HTMLDivElement | null = $state(null);
    let notificationButtonElement: HTMLButtonElement | null = $state(null);

    /**
     * Create a notification data based on a notification response
     */
    function createNotificationData(response: NotificationResponse) {
        var data: NotificationData = {
            id: response.id,
            isRead: response.readStatus,
            title: response.headerText,
            text: response.bodyText,
            readToggle: () => {
                toggleNotificationReadStatus(response.id);
            },
        };

        if (!response.handled) {
            if (response.notificationType == "TripInvitation") {
                data.leftAction = {
                    function: () => console.log("Accept"),
                    text: "Accept",
                    colour: "btn-success",
                };

                data.rightAction = {
                    text: "Decline",
                    function: () => console.log("Decline"),
                    colour: "btn-danger",
                };
            }
        }

        return data;
    }

    /**
     * Toggles the read status of a notification
     * @param notificationId the id of the notification to set
     * @param newStatus the new status for that notification
     */
    async function toggleNotificationReadStatus(notificationId: number) {
        const notification = notifications.find((n) => n.id == notificationId);
        if (notification == null) {
            return;
        }

        const newStatus = !notification.isRead;
        if (newStatus != notification.isRead) {
            notificationUnreadCount += newStatus ? -1 : 1;
        }
        notification.isRead = newStatus;

        try {
            const response = await fetchWithCsrf(
                resolve(
                    `/api/user/notifications/${notificationId}/status` as any,
                ),
                {
                    method: "PUT",
                    credentials: "include",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({ status: newStatus }),
                },
            );

            if (!response.ok) {
                notificationsError = await response.text();
            }
        } catch (err) {
            notificationsError = (err as Error).message;
        }
    }

    /**
     * Refresh's the notification list
     */
    async function refreshNotifications() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/user/notifications` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );

            if (!response.ok) {
                notificationsError = await response.text();
                return
            }

            var getAllNotificationsResponse =
                (await response.json()) as GetAllNotificationsResponse;

            notifications = [];
            notificationUnreadCount = getAllNotificationsResponse.unreadCount;
            moreNotificationsExist = getAllNotificationsResponse.moreExist;
            getAllNotificationsResponse.notificationResponses.forEach((n) =>
                notifications.push(createNotificationData(n)),
            );
        } catch (err) {
            console.log("Error happens here")
            notificationsError = (err as Error).message;
        }
    }

    /**
     * Get a new segment of notifications from the backend
     */
    async function getNotificationSegment() {
        try {
            let count = notifications.length;
            const response = await fetchWithCsrf(
                resolve(`/api/user/notifications?offset=${count}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );

            if (!response.ok) {
                notificationsError = await response.text();
                return
            }

            var getAllNotificationsResponse =
                (await response.json()) as GetAllNotificationsResponse;

            notificationUnreadCount = getAllNotificationsResponse.unreadCount;
            moreNotificationsExist = getAllNotificationsResponse.moreExist;
            getAllNotificationsResponse.notificationResponses.forEach((n) =>
                notifications.push(createNotificationData(n)),
            );
        } catch (err) {
            notificationsError = (err as Error).message;
        }
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

        refreshNotifications();
    });
</script>

<div class="notification-main">
    <button
        class="notification-bell-button"
        aria-label="notifications"
        bind:this={notificationButtonElement}
        onclick={() => {
            toggled = !toggled;
            refreshNotifications();
        }}
    >
        <i class="bi bi-bell icon-centered"></i>
        {#if notificationUnreadCount > 0}
            <div class="notification-counter">
                {notificationUnreadCount > 9
                    ? "9+"
                    : notificationUnreadCount.toString()}
            </div>
        {/if}
    </button>

    <div
        class="notification-list-parent"
        hidden={!toggled}
        bind:this={notificationListElement}
    >
        <div class="notification-topbar">
            <div style="display: flex; flex-direction: column;">
                <span class="notification-title">Notifications</span>
                <span style="color: red;">{notificationsError}</span>
            </div>

            <div class="notification-topbar-buttons">
                <button
                    class="btn notification-topbar-button"
                    aria-label="refresh notifications"
                    onclick={() => refreshNotifications()}
                >
                    <i class="bi bi-arrow-clockwise"></i>
                </button>
                <button
                    class="btn notification-topbar-button"
                    aria-label="exit"
                    onclick={() => {
                        toggled = false;
                        refreshNotifications();
                    }}
                >
                    <i class="bi bi-x"></i>
                </button>
            </div>
        </div>

        <NotificationList
            notifications={notifications}
        />

        {#if moreNotificationsExist}
            <div class="notification-footer">
                <button
                        class="btn btn-outline notification-footer-button"
                        onclick={getNotificationSegment}>
                    See more
                </button> 
            </div>
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

    .notification-footer {
        margin: 0.5rem;
    }
    
    .notification-footer-button {
        width: 100%;
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
        transition: transform 120ms ease box-shadow 120ms ease background-color
            120ms ease;
    }

    .notification-bell-button:hover {
        background-color: rgb(230, 228, 228);
        border-color: #1f1f1f;
        transform: translate(1px, 1px);
        box-shadow: 2px 2px 0 #1f1f1f;
        transition: transform 120ms ease box-shadow 120ms ease background-color
            120ms ease;
    }

    .notification-bell-button:active,
    .notification-bell-button:focus:active {
        background-color: rgb(207, 207, 207);
        transform: translate(3px, 3px);
        box-shadow:
            0px 0px 0 #1f1f1f,
            1px 1px 0 #1f1f1f inset;
        transition: transform 120ms ease box-shadow 120ms ease background-color
            120ms ease;
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
