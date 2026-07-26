<script lang="ts">
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    
    let pfpInput: HTMLInputElement;
    
    let {
        files = $bindable(),
        errors = []
    }: {
        files: FileList | null,
        errors: string[],
    } = $props();
</script>

<div class="d-flex flex-column mb-2">
    <button
            type="button"
            class="btn btn-sm btn-primary"
            class:is-invalid={errors.length > 0}
            on:click={() => {
          pfpInput.value = "";
          pfpInput.click();
        }}
    >
        <i class="bi-images"></i>
    </button>
    <input
            accept="image/webp, image/jpeg, image/png, image/gif, image/svg+xml"
            type="file"
            bind:files
            bind:this={pfpInput}
            class="d-none"
            multiple
    />
    <ErrorDisplay {errors}/>
</div>

