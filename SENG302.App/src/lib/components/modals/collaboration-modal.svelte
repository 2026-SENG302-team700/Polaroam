<script lang="ts">
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
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
    let emailMessage = $state("Completed")
    let emailMessageStyle = $state("")
    
    /**
     * Go back to trip
     */
    function backToTrip() {
        show = false
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
                emailMessageStyle = "invite-message-fail"
                emailMessage = await response.text();
            } else {
                emailMessageStyle = "invite-message-success"
                emailMessage = await "Invite sent."
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
                    <h1 class="card-subtitle mb-2 h5 user-name">Owner: {tripOwner}</h1>
                    {#each tripCollaborators as collaborator}
                        <div>
                            <p>Collaborator: {collaborator.displayName} ({collaborator.userId})</p>
                        </div>
                    {/each}
                    {#if inviteShown}
                        <div class="invite-form">
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
                    {/if}
                    <div class="modal-buttons">
                        <button class="btn btn-secondary modal-footer-button" onclick={backToTrip}>Back</button>
                        <button class="btn btn-primary modal-footer-button"
                                onclick={() => inviteShown = !inviteShown}
                        >Invite</button>
                        <button class="btn btn-danger modal-footer-button" onclick={showLeaveConfirmationModal}>Leave</button>
                    </div>
                    {#if leaveCollaborationError !== ""}
                        <div class="invalid-feedback d-block">
                            <span>{leaveCollaborationError}</span>
                        </div>
                    {/if}
                </div>
                <button class="btn btn-secondary mb-2 ms-4 me-4" onclick={backToTrip}>Back</button>
                {#if !isOwner}
                    <button class="btn danger mb-2 ms-4 me-4" onclick={showLeaveConfirmationModal}>Leave</button>
                    {#if leaveCollaborationError !== ""}
                        <div class="invalid-feedback d-block mb-2 ms-4 me-4">
                            <div>{leaveCollaborationError}</div>
                        </div>
                    {/if}
                {/if}    
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
    
    .invite-form {
        width: 100%;
        display: flex;
        justify-content: space-between;
        gap: 0.5rem;
        margin-top: 1rem;
        margin-bottom: 0.5rem;
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
    
    .invite-message-fail {
        color: #8e1519;
    }
</style>
