<script lang="ts">
    
    let {
        show = $bindable(false),
    }: {
        show: boolean
    } = $props();
    
</script>


{#if show}
    <div class="modal-backdrop show"></div>
    <div class="modal d-block" tabindex="-1" role="dialog">
        <div class="modal-dialog ...">
            <div class="modal-content">
                
            </div>
        </div>
    </div>
{/if}


