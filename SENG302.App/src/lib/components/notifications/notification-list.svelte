<script lang="ts">
    import Notification from "./notification.svelte";

    let {
        notifications = [],
    }: {
        notifications: NotificationData[];
    } = $props();
</script>

<div class="notification-list">
    {#if notifications.length === 0}
        <div class="no-notification">You have no notifications.</div>
    {/if}
    {#each notifications as notification}
        <Notification {notification} />
    {/each}
</div>

<style>
    .notification-list {
        background-color: white;
        width: 100%;
    }

    .no-notification {
        padding: 10px;
        width: 100%;
        text-align: center;
    }
</style>
