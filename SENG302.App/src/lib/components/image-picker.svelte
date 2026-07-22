<script lang="ts">
    let files: FileList;
    let pfpInput: HTMLInputElement;
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
            type="file"
            bind:files
            bind:this={pfpInput}
            class="d-none"
    />
</div>

