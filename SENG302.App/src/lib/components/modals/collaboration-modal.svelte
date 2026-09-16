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

    /**
     * Go back to trip
     */
    function backToTrip() {
        show = false
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
                </div>
                <button class="btn btn-secondary mb-2 ms-4 me-4" onclick={backToTrip}>Back</button>
            </div>
        </div>
    </div>
{/if}

<style>
    .modal-background {
        background-color: #22CACF;
    }
</style>
