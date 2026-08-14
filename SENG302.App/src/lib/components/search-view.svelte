<script>
    import TripCard from "$lib/components/trip-components/trip-card.svelte";
    
    let { 
        trips
    } = $props();
</script>

<div>
    <h2>Trips</h2>
    <div class="trip-object-wrapper">
        {#each trips as trip}
            <TripCard tripObject={trip} />
        {/each}
    </div>
    <h2>Photos</h2>
    <p>to be implemented...</p>
</div>

<style>
    .trip-object-wrapper {
        padding-top: 3px;
        display: grid;
        grid-template-columns: 33% 33% 33%;
        align-items: center;
    }
</style>