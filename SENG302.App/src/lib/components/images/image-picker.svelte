<script lang="ts">
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";

    let pfpInput: HTMLInputElement;
    let files: FileList | undefined = $state();
    let {
        filesList = $bindable<File[]>([]),
        error = ""
    }: { filesList: File[]; error: string } = $props();

    /**
     * Added the selected files to the given file list
     */
    function appendImages() {
        if (!files) return;
        for (const file of files) {
            filesList.push(file);
        }
    }
</script>

<div class="d-flex flex-column mb-2">
    <button
            type="button"
            class="btn btn-sm btn-primary"
            class:is-invalid={error.length > 0}
            onclick={() => {
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
            id="image-picker"
            multiple
            onchange={appendImages}
    />
    <ErrorDisplay errors={[error]}/>
</div>