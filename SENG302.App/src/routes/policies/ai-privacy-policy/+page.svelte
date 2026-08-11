<script lang="ts">
    import { goto } from "$app/navigation";
    import AiPrivacyPolicy from "$lib/policies/ai-privacy-policy.svelte";

    function goBack() {
        if (history.length > 1) {
            history.back();
        } else {
            goto("/");
        }
    }
</script>

<div class="container py-4">
    <AiPrivacyPolicy />
    <button
        class="btn btn-secondary mt-3"
        onclick={goBack}
    >
        Back
    </button>
</div>
