<script lang="ts">
    import ConfirmationModal from "../modals/confirmation-modal.svelte";

    let deletionModalShown = $state(false);
    let errorVisible = $state(false);
    let deleting = $state(false);
    let error = $state();

    let {
        tag = "",
        tagColour = "secondary",
        isRealTag = false,
        deleteFunc
    }: {
        tag: string;
        tagColour?: string;
        isRealTag?: boolean;
        deleteFunc?: Function;
    } = $props();
</script>

<span class="badge rounded-pill text-bg-{tagColour} tag" class:is-invalid={error} class:is-deleting={deleting}>
    {#if errorVisible}
        <div class="tag-error">{error}</div>
    {/if}

    {tag}
    {#if isRealTag}
        {#if error == null}
            <button class="button-x" aria-label="Delete Tag" onclick={() => deletionModalShown=true}>
                {#if deleting}
                    <i class="bi bi-trash"> </i>
                {:else}
                    <i class="bi bi-x"> </i>
                {/if}
            </button>
        {:else}
            <button class="button-x" class:is-invalid={error} aria-label="View Error" onclick={() => errorVisible=!errorVisible}>
                <i class="bi bi-exclamation-circle-fill"></i>
            </button>
        {/if}
    {/if}
</span>

<ConfirmationModal
    bind:show={deletionModalShown}
    message="Are you sure you want to delete this tag?"
    option1Text="Do not delete"
    option2Text="Delete"
    option2Style="danger"
    onOption1={() => deletionModalShown = false}
    onOption2={async () => {
        deletionModalShown = false;

        if (deleteFunc !== undefined) {
            deleting = true;
            error = await deleteFunc();
            deleting = false
        }
    }}
/>


<style>

    .tag-error {
        position: absolute;
        background-color: red;
        transform: translateY(35px);
        padding: 10px 10px;
        color: white;
    }

    .tag-error::before {
        content: "◣";
        position: absolute;
        color: red;
        scale: 1.5;
        transform: translate(-5.7px, -15px);
    }

    .button-x {
        border: 0;
        background: none;
        margin: 0;
        padding: 0;
    }

    .tag.is-invalid {
        color: red !important;
    }

    .tag.is-deleting {
        opacity: 0.2;
        pointer-events: none;
    }

    .button-x.is-invalid {
        color: red;
    }

</style>
