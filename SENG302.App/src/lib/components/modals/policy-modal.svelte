<script lang="ts">
    import type { Snippet } from "svelte";

    let {
        children,
        option1Text = "Confirm",
        option2Text = "Cancel",
        onOption1,
        onOption2,
        show = $bindable(false),
    }: {
        children: Snippet;
        option1Text?: string;
        option2Text?: string;
        onOption1: () => void;
        onOption2: () => void;
        show: boolean;
    } = $props();

    function handleOption1() {
        onOption1();
        show = false;
    }

    function handleOption2() {
        onOption2();
        show = false;
    }
</script>

{#if show}
    <div class="modal-backdrop show"></div>
    <div class="modal d-block" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-dialog-centered modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-body">
                    {@render children()}
                </div>
                <div class="modal-footer d-flex gap-2">
                    <button
                            type="button"
                            class="btn btn-outline-secondary flex-fill"
                            onclick={handleOption2}
                    >
                        {option2Text}
                    </button>
                    <button
                            type="button"
                            class="btn btn-primary flex-fill"
                            onclick={handleOption1}
                    >
                        {option1Text}
                    </button>
                </div>
            </div>
        </div>
    </div>
{/if}