<script lang="ts">
    import { onMount } from "svelte";
    import type { Modal } from "bootstrap";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import { addToast } from "$lib/toast/toast";
    import ProfilePic from "$lib/profilepic/profilepic.svelte";
    import { user } from "$lib/stores/user";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";

    let email = $state("");
    let username = $state("");
    let pfpUrl: string | null = $state(null);
    let logoutConfirmationModalShown = $state(false);

    onMount(async () => {
        retrieveUserData();
    });

    /// shows the logout popup
    function showModal() {
        logoutConfirmationModalShown = true;
    }
    
    // Close the logout confirmation modal
    function closeModal() {
        logoutConfirmationModalShown = false;
    }

    /// <summary>
    /// Gets User email and Username from local storage
    /// Checks if they non null before assigning them to the reactive variables
    /// </>summary>
    async function retrieveUserData() {
        try {
            const response = await fetchWithCsrf(resolve(`/api/user`), {
                method: "GET",
                credentials: "include",
            });

            const data = await response.json();
            if (!response.ok) {
                email = data.message || "Failed to fetch email.";
                username = data.message || "Failed to fetch username.";
                goto(resolve("/"));
                return;
            }
            email = data.email;
            username = data.displayName;
        } catch (err) {
            email = "Failed to fetch email: " + (err as Error).message;
            username = "Failed to fetch username: " + (err as Error).message;
            goto(resolve("/"));
        }
    }

    /**
     * Sends a request to delete the session token to the backend
     * Will always return user to landing page UNLESS an internal server error occurs
     * (meaning the session token MAY NOT be deleted)
     */
    async function logoutUser() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/logout` as any),
                {
                    method: "POST",
                    credentials: "include",
                },
            );

            if (response.status === 500) {
                addToast("Failed to logout. Refresh Webpage", "error");
                return;
            } else {
                goto(resolve("/"));
            }
        } catch (err) {
            addToast(err.message, "error");
        }
    }
</script>

<div class="display: flex; flex-direction: row;">
    <div class="profile-box ca">
        <button
            class="btn btn-primary ms-auto"
            onclick={() => goto(resolve("/home/profile/edit-profile"))}
            >Edit Profile</button
        >
        <div class="profile-content card">
            <div class="card-body card-content">
                <ProfilePic pfpData={$user.pfpData} size="large" />
                <p class="username">{username}</p>
                <p class="user_email">Email: {email}</p>
            </div>
            <div class="card-footer card-content">
                <button
                    type="button"
                    class="btn btn-danger"
                    onclick={showModal}
                >
                    Logout
                </button>
            </div>
        </div>
    </div>
</div>

<ConfirmationModal
        message="Are you sure you want to logout?"
        onOption1={closeModal}
        option2Text="Logout"
        onOption2={logoutUser}
        option2Style="danger"
        show={logoutConfirmationModalShown}
/>

<style>
    .card-content {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
    }

    .profile-box {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
    }

    .username {
        margin-top: 20px;
        font-weight: 600;
        font-style: normal;
        font-size: 2rem;
        font-family: Arial;
    }

    .user_email {
        font-weight: 400;
        font-size: 1rem;
        font-family: Arial;
        word-break: break-all;
    }
</style>
