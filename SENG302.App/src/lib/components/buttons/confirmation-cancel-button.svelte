<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import ConfirmationModal from "$lib/components/modals/confirmation-modal.svelte";
    let { 
        path = `/`,
        option1Text = "Cancel",
        option2Text = "Confirm"
    } = $props();
    let cancelConfirmModalShown = $state(false);
</script>

<button type="button" class="btn btn-secondary" onclick={() => {cancelConfirmModalShown = true;}}
    >Cancel</button
>

<ConfirmationModal
    bind:show={cancelConfirmModalShown}
    message="If you leave this page all progress will be lost"
    option1Text={option1Text}
    option2Text={option2Text}
    option2Style="danger"
    onOption1={() => goto(resolve(path))}
    onOption2={() => {cancelConfirmModalShown = false;}}
/>