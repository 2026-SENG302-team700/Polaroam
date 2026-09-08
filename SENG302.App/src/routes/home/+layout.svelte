<script lang="ts">
    import { onMount } from "svelte";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import { user } from "$lib/stores/user";
    import ProfilePic from "$lib/profilepic/profilepic.svelte";
    import NavBarMenuButton from "$lib/components/buttons/nav-bar-menu-button.svelte";
    import AppLogo from "$lib/components/app-logo.svelte";
    import NotificationBell from "$lib/components/content/notification-bell.svelte";

    let pfpUrl: string | null = null;

    interface Props {
        children?: import("svelte").Snippet;
    }
    let { children }: Props = $props();

    onMount(() => {
        retrieveUser();
    });

    /**
     * Retrieves user information from backend.
     */
    async function retrieveUser() {
        try {
            const response = await fetchWithCsrf(resolve(`/api/user`), {
                method: "GET",
                credentials: "include",
            });

            const data = await response.json();
            if (!response.ok) {
                goto(resolve("/"));
                return;
            }

            user.set(data);

            if (data.profilePicture !== 0) {
                const pfpResponse = await fetchWithCsrf(
                    resolve("/api/user/pfp"),
                    {
                        method: "GET",
                        credentials: "include",
                    },
                );
                const blob = await pfpResponse.blob();
                user.update((u) => ({
                    ...u,
                    pfpData: {
                        imageSource: URL.createObjectURL(blob),
                        offsetX: parseFloat(
                            pfpResponse.headers.get("profile-offset-x") ?? "0",
                        ),
                        offsetY: parseFloat(
                            pfpResponse.headers.get("profile-offset-y") ?? "0",
                        ),
                        zoom: parseFloat(
                            pfpResponse.headers.get("profile-offset-zoom") ??
                                "1",
                        ),
                    },
                }));
            }
        } catch (err) {
            goto(resolve("/"));
        }
    }
</script>

<nav class="navBar menu-bar">
    <div class="content-wrapper">
        <AppLogo />
        <NavBarMenuButton path="/home" text="My Trips" />
        <div class="right">
            <div class="p-2">
                <NotificationBell notificationCount={2} pressFunction={null} />
            </div>
            <h5 class="text" style=" margin-bottom: 0px; padding-bottom: 0px;">
                {$user.displayName}
            </h5>
            <div
                class="profile-button"
                onclick={() => goto(resolve("/home/profile"))}
            >
                <ProfilePic pfpData={$user.pfpData} size="small" />
            </div>
        </div>
    </div>
</nav>

<div class="page-wrapper container-fluid bg-light py-4">
    <div class="content">
        {@render children?.()}
    </div>
</div>

<style>
    .profile-button {
        width: 40px;
        height: 40px;
        padding: 0;
        background: none;
        border-radius: 50%;
        margin-left: 5px;
        cursor: pointer;
    }

    .navBar {
        z-index: 1000;
        vertical-align: top;
        background-color: white;
        height: 60px;
        border-radius: 10px;
        margin-bottom: 20px;
        display: flex;
        align-content: center;
    }
    .right {
        position: relative;
        right: 0;
        display: flex;
        flex-direction: row;
        align-items: center;
        flex: 1;
        justify-content: flex-end;
        margin-right: 5px;
    }

    .page-wrapper {
        min-height: 80vh;
        display: flex;
        flex-direction: column;
    }

    .content-wrapper {
        display: flex;
        flex-direction: row;
        align-items: center;
        width: 100%;
    }
</style>
