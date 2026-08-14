<script>
    import TripCard from "$lib/components/trip-components/trip-card.svelte";
    import ImageDisplay from "$lib/components/images/image-display.svelte";
    
    let { 
        trips,
        photos,
        photoIds
    } = $props();
    
</script>
<div>
    <h1>Search Results</h1>
    <h2>Trips</h2>
    <div class="trip-object-wrapper">
        {#each trips as trip}
            <TripCard tripObject={trip} />
        {/each}
    </div>
    <h2>Photos</h2>
    <ImageDisplay images={photos} photoIds={photoIds}/>
</div>

<style>
    .trip-object-wrapper {
        padding-top: 3px;
        display: grid;
        grid-template-columns: 33% 33% 33%;
        align-items: center;
    }
</style>