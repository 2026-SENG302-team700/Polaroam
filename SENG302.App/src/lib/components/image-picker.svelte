<script lang="ts">
    let pfpInput: HTMLInputElement;
    
    let {
        files = $bindable(),
    }: {
        files: FileList | null,
    } = $props();
</script>

<div class="d-flex flex-column mb-2">
    <button
            type="button"
            class="btn btn-sm btn-primary"
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
</div>

