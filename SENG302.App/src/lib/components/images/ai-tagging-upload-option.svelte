<script lang="ts">
    let {
        enabled,
        aiTaggingForUpload = $bindable(),
        loading = false,
        compact = false,
        floating = false,
        showTemporaryNotice = true,
    }: {
        enabled: boolean;
        aiTaggingForUpload: boolean;
        loading?: boolean;
        compact?: boolean;
        floating?: boolean;
        showTemporaryNotice?: boolean;
    } = $props();
</script>

{#if enabled}
    <div class="ai-tagging-reminder" class:compact class:floating>
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

        {#if !aiTaggingForUpload && showTemporaryNotice}
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

    .ai-tagging-reminder.compact {
        margin-top: 0;
        padding: 0.35rem 0.55rem;
        font-size: 0.8rem;
    }

    .ai-tagging-reminder.compact .temporary-notice {
        margin-top: 0.25rem;
        font-size: 0.75rem;
        line-height: 1.2;
    }

    .ai-tagging-reminder.floating {
        position: absolute;
        bottom: calc(100% + 0.35rem);
        left: 0;
        z-index: 1;
    }
</style>
