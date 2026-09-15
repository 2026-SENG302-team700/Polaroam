<script lang="ts">
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    let {
        show = $bindable(false),
        tripOwner = "",
        tripCollaborators = [],
        error = "",
    }: {
        show: boolean;
        tripOwner: string;
        tripCollaborators: CollaboratingUser[];
        error: string;
    } = $props();
    
    // Leave Collaboration
    let leaveConfirmationModalShown = $state(false);
    let leaveCollaborationError = $state("");

    let inviteShown = $state(false);
    let invite = "";
    
    /**
     * Go back to trip
     */
    function backToTrip() {
        show = false
    }
    
    function handleKeydown(e: KeyboardEvent) {
        if (e.key !== "Enter") {
            return;
        }
        
        e.preventDefault();
        inviteCollaborators()
    }
    
    function inviteCollaborators() {
        console.log(invite);
    }

    /**
     * Opens the leave collaboration confirmation modal
     */
    function showLeaveConfirmationModal() {
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
    <div class="modal d-block" tabindex="-1" role="dialog">
        <div class="modal-dialog ...">
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
                                    onkeydown={handleKeydown}
                            />
                            <button class="btn btn-primary" onclick="{inviteCollaborators}">
                                <i class="bi bi-envelope-arrow-up" />
                            </button>
                        </div>
                    {/if}
                    <div class="modal-buttons">
                        <button class="btn btn-secondary modal-footer-button" onclick={backToTrip}>Back</button>
                        <button class="btn btn-secondary modal-footer-button"
                                onclick={() => inviteShown = !inviteShown}
                        >Invite Collaborator</button>
                    </div>
                </div>
                <button class="btn btn-secondary mb-2 ms-4 me-4" onclick={backToTrip}>Back</button>
                <button class="btn danger mb-2 ms-4 me-4" onclick={showLeaveConfirmationModal}>Leave</button>
                {#if leaveCollaborationError !== ""}
                    <div class="invalid-feedback d-block mb-2 ms-4 me-4">
                        <div>{leaveCollaborationError}</div>
                    </div>
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
    .modal-background {
        background-color: #22CACF;
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
</style>
