<script lang="ts">
    let {
        notifications = [],
    }: {
        notifications: NotificationData[];
    } = $props();
</script>

<!-- All this is completely temporary -->

<div class="notification-list">
    {#each notifications as notification}
        <div class="notification">
            <h5>{notification.userDisplayName}</h5>
            <div>{notification.text}</div>
        </div>
    {/each}
</div>

<style>
    .notification-list {
        background-color: white;
        width: max-content;
    }

    .notification {
        border-top: 1px solid black;
        height: 60px;
    }
</style>
