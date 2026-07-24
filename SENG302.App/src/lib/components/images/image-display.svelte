<script lang="ts">
    let {
        images = null,
    } = $props();
</script>

<div>
    {#each images as image}
        <img src={URL.createObjectURL(image)} alt="Image" />
        {:else}
        <p>No image selected</p>
    {:/each}/}
</div>
