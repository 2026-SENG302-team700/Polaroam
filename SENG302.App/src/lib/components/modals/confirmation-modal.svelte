<script lang="ts">
    let {
        message,
        option1Text = "Cancel",
        option2Text = "Confirm",
        option3Text,
        option1Style = "outline-secondary",
        option2Style = "",
        option3Style = "",
        onOption1,
        onOption2,
        onOption3,
        show = $bindable(false),
        size = "default",
    }: {
        message: string;
        option1Text?: string;
        option2Text?: string;
        option3Text?: string;
        option1Style?: "danger" | "primary" | "outline-secondary";
        option2Style?: "danger" | "primary" | "";
        option3Style?: "danger" | "primary" | "";
        onOption1: () => void;
        onOption2: () => void;
        onOption3?: () => void;
        show: boolean;
        size?: "default" | "collaboration";
    } = $props();

    /**
     * Calls the function passed in than closes the modal
     * @param optionFunc The function to be called
     */
    function handleOption(optionFunc: () => void) {
        optionFunc();
        show = false;
    }
</script>

{#if show}
    <div class="modal-backdrop show"></div>
    <div class="modal d-block" tabindex="-1" role="dialog">
        <div
            class="modal-dialog modal-dialog-centered"
            class:collaboration-size={size == "collaboration"}
            role="document"
        >
            <div class="modal-content">
                <div class="modal-body">
                    <p class="mb-0">{message}</p>
                </div>
                <div class="modal-footer d-flex gap-2">
                    <button
                        type="button"
                        class="btn btn-{option1Style} action-button"
                        onclick={() => handleOption(onOption1)}
                    >
                        {option1Text}
                    </button>
                    <button
                        type="button"
                        class="btn btn-{option2Style} action-button"
                        onclick={() => handleOption(onOption2)}
                    >
                        {option2Text}
                    </button>
                    {#if option3Text && onOption3}
                        <button
                            type="button"
                            class="btn btn-{option3Style} action-button"
                            onclick={() => handleOption(onOption3)}
                        >
                            {option3Text}
                        </button>
                    {/if}
                </div>
            </div>
        </div>
    </div>
{/if}

<style>
    .action-button {
        flex: 1 1 0;
    }

    .modal-dialog.collaboration-size {
        width: 33vw;
        max-width: 33vw;
    }

    @media (max-width: 900px) {
        .modal-dialog.collaboration-size {
            width: calc(100vw - 2rem);
            max-width: calc(100vw - 2rem);
        }
    }
</style>
