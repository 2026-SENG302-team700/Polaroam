<script lang="ts">
    let {
        enabled,
        aiTaggingForUpload = $bindable(),
        loading = false,
    }: {
        enabled: boolean;
        aiTaggingForUpload: boolean;
        loading?: boolean;
    } = $props();
</script>

{#if enabled}
    <div class="ai-tagging-reminder">
        <label class="form-check">
            <input
                class="form-check-input"
                type="checkbox"
                bind:checked={aiTaggingForUpload}
                disabled={loading}
            />

            <span class="form-check-label">
                Use AI tagging for this upload
            </span>
        </label>

        {#if !aiTaggingForUpload}
            <p class="temporary-notice">
                AI tagging is disabled for this upload only. Your account
                setting will not be changed.
            </p>
        {/if}
    </div>
{/if}

<style>
    .ai-tagging-reminder {
        width: 100%;
        margin-top: 0.75rem;
        padding: 0.75rem 1rem;
        border: 1px solid #9ec5fe;
        border-radius: 0.5rem;
        color: #084298;
    }

    .form-check {
        margin: 0;
    }

    .form-check-label {
        white-space: normal;
    }

    .temporary-notice {
        margin: 0.5rem 0 0 1.5rem;
        font-size: 0.9rem;
    }
</style>
