<script lang="ts">
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
            </div>
        </div>
    </div>
{/if}

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
