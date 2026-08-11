<script lang="ts">
    import { goto } from "$app/navigation";
    import PrivacyPolicy from "$lib/policies/privacy-policy.svelte";

    /**
     * Get's browser history, if not long just takes them to their browser homescreen.
     */
    function goBack() {
        if (history.length > 1) {
            history.back();
        } else {
            goto("/");
        }
    }
</script>

<div class="container py-4">
    <PrivacyPolicy />
    <button
        class="btn btn-secondary mt-3"
        onclick={goBack}
    >
        Back
    </button>
</div>
