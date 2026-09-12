<script lang="ts">
    import type {Snippet} from "svelte";

    let { notification }: { notification: NotificationData } = $props();
</script>

<div class={`notification-wrapper ${notification.isRead ? "notification-read" : ""}`}>
    <span class="notification-title">{notification.title}</span>
    <span class="notification-description">{notification.text}</span>
    <div class="notification-actions">
        {#if notification.leftAction}
            <button
                    class={`btn action-buttons ${notification.leftAction.colour}`}
                    onclick={notification.leftAction.function}
            >
                {notification.leftAction.text}
            </button>
        {/if}
        {#if notification.rightAction}
            <button
                    class={`btn action-buttons ${notification.rightAction.colour}`}
                    onclick={notification.rightAction.function}
            >
                {notification.rightAction.text}
            </button>
        {/if}
        <button
                class="btn btn-secondary"
                onclick={notification.readToggle}
        >
            {#if notification.isRead}
                <i class="bi bi-envelope" />
            {:else}
                <i class="bi bi-envelope-open" />
            {/if}
        </button>
    </div>
</div>

<style>
    .notification-wrapper {
        background-color: #ceebfd;
        padding: 0.5rem;
    }
    
    .notification-actions {
        display: flex;
        width: 100%;
        gap: 0.3rem;
    }
    
    .notification-title {
        font-weight: bold;
        display: block;
    }
    
    .notification-description {
        display: block;
        margin-bottom: 0.35rem;
    }
    
    .notification-read {
        background-color: #ffffff;
    }
    
    .action-buttons {
        width: 100%
    }
</style>