<script lang="ts">
    let { deleteAction, class: className = "" }:
        { deleteAction: Function; class?: string }
        = $props();
</script>

<button class="btn btn-danger bi-trash {className}" title="Delete" onclick={async () => { deleteAction() }}></button>