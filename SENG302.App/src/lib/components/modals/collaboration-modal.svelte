<script lang="ts">

    let {
        show = $bindable(false),
        tripOwner = "",
    }: {
        show: boolean;
        tripOwner: string;
    } = $props();
    
    
</script>

{#if show}
    <div class="modal-backdrop show"></div>
    <div class="modal d-block" tabindex="-1" role="dialog">
        <div class="modal-dialog ...">
            <div class="modal-content">
                <div class="p-4">
                    Owner: {tripOwner}
                </div>
            </div>
        </div>
    </div>
{/if}
