<script lang="ts">
    let {
        notifications = [],
    }: {
        notifications: NotificationData[];
    } = $props();
</script>

<div class="notification-list">
    <!-- temporary block until notifications are implemented -->
    {#each notifications as notification}
        <div class="notification">
            <h5>{notification.userDisplayName}</h5>
            <div>{notification.text}</div>
        </div>
    {/each}
    <!-- end of temp block -->
</div>

<style>
    .notification-list {
        background-color: white;
        width: 100%;
    }

    .notification {
        border-top: 1px solid black;
        height: 60px;
    }
</style>
