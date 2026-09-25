<script lang="ts">
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import { page } from "$app/state";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";

    import { onMount } from "svelte";
    import { user } from "$lib/stores/user";
    import { goto } from "$app/navigation";

    let {
        show = $bindable(false),
    }: {
        show: boolean;
    } = $props();

    // Modal State
    let tripId = page.params.tripId;
    let tripOwner = $state<null | UserResponse>(null);
    let isOwner = $derived(
        tripOwner !== null && $user.id !== null && tripOwner.id === $user.id,
    );
    let tripCollaborators = $state<{ userId: number; displayName: string }[]>(
        [],
    );
    let error = $state("");

    // Leave Collaboration
    let leaveConfirmationModalShown = $state(false);
    let leaveCollaborationError = $state("");

    // Invite Collaboraters
    let inviteShown = $state(false);
    let invite = $state("");
    let emailMessage = $state("");
    let emailMessageStyle = $state("");

    let kickUserConfirmationModalShown = $state(false);
    let kickUserMessage = $state("");
    let UserIdOfKicked = $state<number | null>(null);
    let kickUserError = $state("");

    // Swap Ownership
    let swapOwnershipConfirmationModalShown = $state(false);
    let swapOwnershipError = $state("");
    let newOwnerId = $state<number | null>(null);
    let swapOwnershipMessage = $state("");

    let loading = $state(false);

    /**
     * Gets a list of everyone who is a collaborator on the trip
     */
    async function getCollaborators() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${tripId}/collaboration` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            tripCollaborators = await response.json();
        } catch (err: any) {
            error = err.toString();
        } finally {
            loading = false;
        }
    }

    /**
     * Gets the owner of the trip. If the user is the owner, then the isOwner variable is set to true.
     */
    async function getOwner() {
        loading = true;
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${tripId}/user` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            tripOwner = await response.json();
        } catch (err: any) {
            error = err.toString();
        } finally {
            loading = true;
        }
    }

    /**
     * Clear fields of input
     */
    function clearFields() {
        invite = "";
        emailMessage = "";
        leaveCollaborationError = "";
        inviteShown = false;
        emailMessageStyle = "";
    }

    /**
     * Go back to trip
     */
    function backToTrip() {
        show = false;
        clearFields();
    }

    /**
     * Makes enter key work for inviting others to collaborate
     * @param e
     */
    async function handleInviteKeydown(e: KeyboardEvent) {
        if (e.key !== "Enter") {
            return;
        }

        e.preventDefault();
        await inviteCollaborators();
    }

    /**
     * Sends API call to invite collaborators to the trip
     */
    async function inviteCollaborators() {
        const emailInvite = invite;
        const tripId = page.params.tripId;

        try {
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${tripId}/collaboration` as any),
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    credentials: "include",
                    body: JSON.stringify({
                        email: emailInvite,
                    }),
                },
            );

            if (!response.ok) {
                const data = await response.json();
                emailMessageStyle = `invite-message-${data.type}`;
                emailMessage = data.message;
            } else {
                emailMessageStyle = "invite-message-success";
                emailMessage = "Invite sent.";
            }
        } catch (e) {
            console.error(e);
        }
    }

    /**
     * Opens the leave collaboration confirmation modal
     */
    function showLeaveConfirmationModal() {
        leaveCollaborationError = "";
        leaveConfirmationModalShown = true;
    }

    /**
     * Closes the leave collaboration confirmation modal
     */
    function hideLeaveConfirmationModal() {
        leaveConfirmationModalShown = false;
    }

    /**
     * Closes the Kick user confirmation modal
     * @param collaboratorName
     * @param collaboratorId
     */
    function showKickUserConfirmationModal(
        collaboratorName: string,
        collaboratorId: number,
    ) {
        kickUserConfirmationModalShown = true;
        kickUserMessage = `Are you sure you want to kick ${collaboratorName}?`;
        UserIdOfKicked = collaboratorId;
    }

    /**
     * Hide the kick user confirmation modal
     */
    function hideKickUserConfirmationModal() {
        kickUserConfirmationModalShown = false;
    }

    /**
     * Kick the user from the current trip
     */
    async function kickUser() {
        kickUserMessage = "";
        const tripId = page.params.tripId;

        try {
            const response = await fetchWithCsrf(
                resolve(
                    `/api/trip/${tripId}/collaboration/kick/${UserIdOfKicked}` as any,
                ),
                {
                    method: "DELETE",
                    credentials: "include",
                },
            );

            if (!response.ok) {
                kickUserError = await response.text();
            }

            await getCollaborators();
        } catch (e: any) {
            kickUserError = e.message;
        } finally {
            hideKickUserConfirmationModal();
        }
    }

    /**
     * Calls backend to remove the user as a collaborator on the trip.
     * If successful the user will no longer have access to the trip so
     * hides the modal and navigates them to the MyTrips page
     */
    async function leaveCollaboration() {
        try {
            leaveCollaborationError = "";
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${tripId}/collaboration/leave` as any),
                {
                    method: "POST",
                    credentials: "include",
                },
            );
            if (!response.ok) {
                leaveCollaborationError = "Failed to leave collaboration";
                return;
            }

            goto(resolve("/home"));
        } catch {
            leaveCollaborationError = "Failed to leave collaboration";
        }
    }

    /**
     * Opens the transfer ownership confirmation modal
     * @param collaboratorName
     * @param collaboratorId
     */
    function showSwapOwnerConfirmationModal(
        collaboratorName: string,
        collaboratorId: number,
    ) {
        swapOwnershipConfirmationModalShown = true;
        swapOwnershipMessage = `Are you sure you want to transfer trip ownership to ${collaboratorName}?`;
        newOwnerId = collaboratorId;
    }

    /**
     * Hide the swap ownership confirmation modal
     */
    function hideSwapOwnershipConfirmationModal() {
        swapOwnershipConfirmationModalShown = false;
    }

    /**
     * Calls backend to transfer ownership of the trip to the user.
     * @param userId
     */
    async function swapOwnership(userId: number | null) {
        try {
            const response = await fetchWithCsrf(
                resolve(
                    `/api/trip/${tripId}/collaboration/transfer/${newOwnerId}` as any,
                ),
                {
                    method: "PATCH",
                    credentials: "include",
                },
            );
            if (!response.ok) {
                swapOwnershipError = "Failed to transfer ownership";
            }
            await getOwner();
            await getCollaborators();
        } catch {
            swapOwnershipError = "Failed to transfer ownership";
        }
    }

    onMount(async () => {
        await getOwner();
        await getCollaborators();
        console.log(`Owner: ${isOwner}`);
    });

    // Lock scrolling only while the modal is visible
    $effect(() => {
        if (!show) return;

        const previousBodyOverflow = document.body.style.overflow;
        const previousHtmlOverflow = document.documentElement.style.overflow;

        document.body.style.overflow = "hidden";
        document.documentElement.style.overflow = "hidden";

        // Runs when `show` becomes false or the component is destroyed
        return () => {
            document.body.style.overflow = previousBodyOverflow;
            document.documentElement.style.overflow = previousHtmlOverflow;
        };
    });
</script>

{#if show}
    <div class="modal-backdrop show"></div>
    <div class="modal" tabindex="-1" role="dialog">
        <div class="modal-dialog">
            <div class="modal-content modal-background">
                <div class="p-4">
                    {#if error !== ""}
                        Error: {error}
                    {/if}
                    <h1 class="card-subtitle mb-4 h2 user-name">
                        Owner: {tripOwner?.displayName}
                    </h1>
                    {#if !loading}
                        {#each tripCollaborators as collaborator}
                            <div
                                class="d-flex align-items-center justify-content-between p-2 mb-2 border rounded collaborators-list"
                            >
                                <span class="collaborator-info fw-medium"
                                    >{collaborator.displayName}</span
                                >
                                {#if isOwner}
                                    <div
                                        class="btn-group btn-group-sm"
                                        role="group"
                                    >
                                        <button
                                            class="btn btn-outline-secondary"
                                            onclick={() =>
                                                showSwapOwnerConfirmationModal(
                                                    collaborator.displayName,
                                                    collaborator.userId,
                                                )}
                                        >
                                            Grant Ownership
                                        </button>
                                        <button
                                            class="btn btn-danger"
                                            onclick={() =>
                                                showKickUserConfirmationModal(
                                                    collaborator.displayName,
                                                    collaborator.userId,
                                                )}
                                            disabled={loading}
                                        >
                                            Kick
                                        </button>
                                    </div>
                                {/if}
                            </div>
                            {#if kickUserError !== ""}
                                <div class="invalid-feedback d-block mb-0">
                                    <span>{kickUserError}</span>
                                </div>
                            {/if}
                        {/each}
                    {/if}
                    {#if inviteShown}
                        <div class="invite-wrapper">
                            <span>Invite Collaborators to Trip:</span>
                            <div
                                class="invite-form input-group d-flex flex-nowrap"
                            >
                                <input
                                    type="email"
                                    class="form-control"
                                    placeholder="john@example.com"
                                    bind:value={invite}
                                    onkeydown={handleInviteKeydown}
                                    disabled={loading}
                                />
                                <button
                                    class="btn btn-primary"
                                    onclick={inviteCollaborators}
                                    disabled={loading}
                                >
                                    <i class="bi bi-envelope-arrow-up" />
                                </button>
                            </div>
                            <span class={`${emailMessageStyle}`}
                                >{emailMessage}</span
                            >
                        </div>
                    {/if}
                    <div class="modal-buttons">
                        <button
                            class="btn btn-secondary modal-footer-button"
                            onclick={backToTrip}
                            disabled={loading}>Back to trip</button
                        >
                        <button
                            class="btn btn-primary modal-footer-button"
                            onclick={() => (inviteShown = !inviteShown)}
                            disabled={loading}
                            >{inviteShown
                                ? "Close Invite field"
                                : "Invite"}</button
                        >
                        {#if !isOwner}
                            <button
                                class="btn btn-danger modal-footer-button"
                                onclick={showLeaveConfirmationModal}
                                disabled={loading}>Leave</button
                            >
                        {/if}
                    </div>
                    {#if leaveCollaborationError !== ""}
                        <div class="invalid-feedback d-block">
                            <span>{leaveCollaborationError}</span>
                        </div>
                    {/if}
                </div>

                <ConfirmationModal
                    bind:show={leaveConfirmationModalShown}
                    message="Are you sure you want to stop collaborating and lose access to this trip?"
                    option1Text="Cancel"
                    option2Text="Confirm"
                    option2Style="danger"
                    onOption1={hideLeaveConfirmationModal}
                    onOption2={leaveCollaboration}
                    contained
                />

                <ConfirmationModal
                    message={kickUserMessage}
                    option1Text="Cancel"
                    option2Text="Confirm"
                    option2Style="danger"
                    onOption1={hideKickUserConfirmationModal}
                    onOption2={async () => {
                        await kickUser();
                    }}
                    bind:show={kickUserConfirmationModalShown}
                    contained
                />

                <ConfirmationModal
                    message={swapOwnershipMessage}
                    option1Text="Cancel"
                    option2Text="Confirm"
                    option2Style="danger"
                    onOption1={hideSwapOwnershipConfirmationModal}
                    onOption2={async () => {
                        await swapOwnership(newOwnerId);
                    }}
                    bind:show={swapOwnershipConfirmationModalShown}
                    contained
                />
            </div>
        </div>
    </div>
{/if}

<style>
    .modal {
        display: flex;
        width: 100vw;
        height: 100vh;
        align-items: center;
        vertical-align: center;
    }

    .modal-content {
        position: relative;
        width: clamp(320px, 90vw, 500px);
    }

    .modal-buttons {
        display: flex;
        justify-content: space-between;
        gap: 0.5rem;
        margin-top: 1rem;
    }

    .modal-footer-button {
        width: 100%;
    }

    .invite-wrapper {
        margin-top: 1rem;
        margin-bottom: 0.5rem;
    }

    .invite-form {
        width: 100%;
        display: flex;
        justify-content: space-between;
        margin-bottom: 0.2rem;
    }

    .invite-form input {
        width: 100%;
    }

    .invite-form button {
        width: 4rem;
    }

    .invite-message-success {
        color: #0f5323;
    }

    .invite-message-error {
        color: #8e1519;
    }

    .invite-message-info {
        color: #0c7d9d;
    }

    .collaborator-info {
        font-size: 18px;
        font-weight: 500;
    }

    .collaborators-list {
        display: flex;
        gap: 5px;
        justify-content: space-between;
        margin-bottom: 4px;
        margin-top: 4px;
        flex-wrap: wrap;
    }
</style>
