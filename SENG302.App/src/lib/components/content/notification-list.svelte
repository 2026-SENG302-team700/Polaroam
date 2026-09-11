<script lang="ts">
    import Notification from "$lib/components/content/notification.svelte";
    
    let {
        notifications = [],
    }: {
        notifications: NotificationData[];
    } = $props();
    
    
</script>

<div class="notification-list">
    {#each notifications as notification}
        <Notification
            title={notification.userDisplayName}
            description={notification.text}
        >
            <button>Action Button 1</button>
            <button>Action Button 2</button>
        </Notification>
    {/each}
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
