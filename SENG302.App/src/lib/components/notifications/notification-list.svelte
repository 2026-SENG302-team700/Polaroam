<script lang="ts">
    import Notification from "./notification.svelte";
    
    let {
        notifications = [],
    }: {
        notifications: NotificationData[];
    } = $props();
</script>

<div class="notification-list">
    {#if (notifications.length === 0)}
        <span>You have no notifications.</span>
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
</style>
