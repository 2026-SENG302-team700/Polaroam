<script lang="ts">
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import { page } from "$app/state";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";
    
    let {
        show = $bindable(false),
        isOwner = $bindable(false),
        tripOwner = "",
        tripCollaborators = [],
        error = "",
    }: {
        show: boolean;
        isOwner: boolean;
        tripOwner: string;
        tripCollaborators: CollaboratingUser[];
        error: string;
    } = $props();
    
    // Leave Collaboration
    let leaveConfirmationModalShown = $state(false);
    let leaveCollaborationError = $state("");

    // Invite Collaboraters
    let inviteShown = $state(false);
    let invite = $state("");
    let emailMessage = $state("")
    let emailMessageStyle = $state("")
    
    let kickUserConfirmationModalShown = $state(false)
    let kickUserMessage = $state("")
    let UserIdOfKicked = $state<number | null>(null)
    let kickUserError = $state("")
    

    /**
     * Clear fields of input
     */
    function clearFields() {
        invite = "";
        emailMessage = ""
        leaveCollaborationError = ""
        inviteShown = false
        emailMessageStyle = ""
    }
    
    /**
     * Go back to trip
     */
    function backToTrip() {
        show = false
        clearFields()
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
        await inviteCollaborators()
    }
    

    /**
     * Sends API call to invite collaborators to the trip
     */
    async function inviteCollaborators() {
        const emailInvite = invite;
        const tripId = page.params.tripId;
        
        try {
            const response = await fetchWithCsrf(resolve(`/api/trip/${tripId}/collaboration` as any), {
                method: "POST",
                headers: {
                    "Content-Type": "application/json", 
                },
                credentials: "include",
                body: JSON.stringify({
                    email: emailInvite
                })
            });
            
            
            if (!response.ok) {
                const data = await response.json();
                emailMessageStyle = `invite-message-${data.type}`;
                emailMessage = data.message;
            } else {
                emailMessageStyle = "invite-message-success"
                emailMessage = "Invite sent."
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
    function showKickUserConfirmationModal(collaboratorName: string, collaboratorId: number) {
        kickUserConfirmationModalShown = true
        kickUserMessage =  `Are you sure you want to kick ${collaboratorName}?`
        UserIdOfKicked = collaboratorId
    }

    /**
     * Hide the kick user confirmation modal
     */
    function hideKickUserConfirmationModal() {
        kickUserConfirmationModalShown = false
    }

    /**
     * Kick the user
     */
    async function kickUser() {
        kickUserMessage = "";
        const tripId = page.params.tripId;
        
        try {
            const response = await fetchWithCsrf(resolve(`/api/trip/${tripId}/collaboration/kick/${UserIdOfKicked}` as any), {
                method: "DELETE",
                credentials: "include"
            });

            if (!response.ok) {
                kickUserError = await response.text() 
            } else {
                tripCollaborators = await response.json()
            }

        } catch (e: any) {
            kickUserError = e.message;
        } finally {
            hideKickUserConfirmationModal()
        }
    }

    /**
     * Calls backend to remove the user as a collaborator on the trip.
     * If successful the user will no longer have access to the trip so 
     * hides the modal and navigates them to the MyTrips page
     */
    async function leaveCollaboration() {
        const tripId = page.params.tripId;
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
            }
        } catch {
            leaveCollaborationError = "Failed to leave collaboration";
        }
    }
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
                    <h1 class="card-subtitle mb-4 h2 user-name">Owner: {tripOwner}</h1>
                    {#each tripCollaborators as collaborator}
                        <div class="collaborators-list">
                            <span class="collaborator-info">Collaborator: {collaborator.displayName}</span>
                            {#if isOwner}
                                <button class="btn btn-secondary"
                                        onclick={() => showKickUserConfirmationModal(collaborator.displayName, collaborator.userId)}>Kick</button>
                            {/if}
                        </div>
                        {#if kickUserError !== ""}
                            <div class="invalid-feedback d-block mb-0">
                                <span>{kickUserError}</span>
                            </div>
                        {/if}
                    {/each}
                    {#if inviteShown}
                        <div class="invite-wrapper">
                            <span>Invite Collaborators to Trip:</span>
                            <div class="invite-form input-group d-flex flex-nowrap">
                                <input
                                        type="email"
                                        class="form-control"
                                        placeholder="john@example.com"
                                        bind:value={invite}
                                        onkeydown={handleInviteKeydown}
                                />
                                <button class="btn btn-primary" onclick="{inviteCollaborators}">
                                    <i class="bi bi-envelope-arrow-up" />
                                </button>
                            </div>
                            <span class={`${emailMessageStyle}`}>{emailMessage}</span>
                        </div>
                    {/if}
                    <div class="modal-buttons">
                        <button class="btn btn-secondary modal-footer-button" onclick={backToTrip}>Back to trip</button>
                        <button class="btn btn-primary modal-footer-button"
                                onclick={() => inviteShown = !inviteShown}
                        >{inviteShown ? "Close Invite field": "Invite"}</button>
                        {#if !isOwner}
                            <button class="btn btn-danger modal-footer-button" onclick={showLeaveConfirmationModal}>Leave</button>
                        {/if}
                    </div>
                    {#if leaveCollaborationError !== ""}
                        <div class="invalid-feedback d-block">
                            <span>{leaveCollaborationError}</span>
                        </div>
                    {/if}
                </div>
            </div>
        </div>
    </div>
{/if}

<ConfirmationModal
        bind:show={leaveConfirmationModalShown}
        message="Are you sure you want to stop collaborating and lose access to this trip?"
        option1Text="Confirm"
        option2Text="Cancel"
        onOption1={leaveCollaboration}
        onOption2={hideLeaveConfirmationModal}
/>

<ConfirmationModal
        message={kickUserMessage}
        option1Text="Cancel"
        option2Text="Confirm to kick user"
        option2Style="danger"
        onOption1={hideKickUserConfirmationModal}
        onOption2={async () => {
        await kickUser();
    }}
        bind:show={kickUserConfirmationModalShown}
/>


<style>
    .modal {
        display: flex;
        width: 100vw;
        height: 100vh;
        align-items: center;
        vertical-align: center;
    }
    
    .modal-content {
        width: 33vw;
    }
    
    .modal-buttons {
        display: flex;
        justify-content: space-between;
        gap: 0.5rem;
        margin-top: 1rem;
    }
    
    .modal-footer-button {
        width: 100%
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
