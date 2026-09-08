<script lang="ts">
    let {
        notificationCount = 0,
        pressFunction = null,
    }: {
        notificationCount: number;
        pressFunction: Function | null;
    } = $props();
</script>

<button class="notification-parent" aria-label="notifications">
    <i class="bi bi-bell icon-centered"></i>
    {#if notificationCount > 0}
        <div class="notificationCounter">
            {notificationCount > 9 ? "9+" : notificationCount.toString()}
        </div>
    {/if}
</button>

<style>
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
        border: 0;
    }

    .notificationCounter {
        position: absolute;
        transform: translate(15px, -40px);
        background-color: red;
        color: white;
        border-radius: 50%;
        width: 22px;
        height: 22px;
        font-size: smaller;
    }
</style>
