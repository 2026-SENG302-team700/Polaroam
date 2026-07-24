<script lang="ts">
    let {
        images = [],
    }: { images?: File[] | null } = $props();
</script>

<div class="row row-cols-2 row-cols-md-3 row-cols-xl-4 g-2 container">
    {#each images ?? [] as image}
        <div class="col">
            <img src={URL.createObjectURL(image)} alt="Image" class="img-thumbnail w-100" style="aspect-ratio: 1; object-fit: cover;" />
        </div>
    {:else}
        <p>No image selected</p>
    {/each}
</div>