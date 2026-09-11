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
</script>

{#if show}
    <div class="modal-backdrop show"></div>
    <div class="modal d-block" tabindex="-1" role="dialog">
        <div class="modal-dialog ...">
            <div class="modal-content">
                <div class="p-4">
                    {#if error != ""}
                        Error: {error}
                    {/if}
                    Owner: {tripOwner}
                    {#each tripCollaborators as collaborator}
                        <div>
                            Collaborator: {collaborator.displayName} ({collaborator.userId})
                        </div>
                    {/each}
                </div>
            </div>
        </div>
    </div>
{/if}
