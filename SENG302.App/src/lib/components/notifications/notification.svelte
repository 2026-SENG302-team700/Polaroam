<script lang="ts">
    import { addToast, type ToastType } from "$lib/toast/toast";

    let { notification }: { notification: NotificationData } = $props();
    let handled = $state(false);
    

    let handler = (func:()=>void, toastText: string, toastType: ToastType = "success") => {
        func();
        handled = true;
        addToast(toastText, toastType);
    }
</script>

<div
    class={`notification-wrapper ${notification.isRead ? "notification-read" : ""}`}
>
    <span class="notification-title">{notification.title}</span>
    <span class="notification-description">{notification.text}</span>
    <div class="notification-actions">
        {#if notification.leftAction && !handled}
            <button
                class={`btn action-buttons ${notification.leftAction.colour}`}
                onclick={() => handler(notification.leftAction.function,notification.leftAction.toastText)}
            >
                {notification.leftAction.text}
            </button>
        {/if}
        {#if notification.rightAction && !handled}
            <button
                class={`btn action-buttons ${notification.rightAction.colour}`}
                onclick={() => handler(notification.rightAction.function,notification.rightAction.toastText,"error")}
            >
                {notification.rightAction.text}
            </button>
        {/if}
        <button class="btn btn-secondary" onclick={notification.readToggle}>
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
        justify-content: end;
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
        width: 100%;
    }
</style>
