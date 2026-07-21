<script lang="ts">
    let { deleteAction } :
        { deleteAction : Function }
        = $props();
</script>

<button class="btn btn-danger bi-trash" title="Delete" onclick={async () => { deleteAction() }}></button>
