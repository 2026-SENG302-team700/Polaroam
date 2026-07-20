<script lang="ts">
    import AddButton from "$lib/components/buttons/add-button.svelte";
    import SearchForm from "$lib/components/forms/search-form.svelte";

    let trips = [];
</script>

<div class="container">
    <div style="display: flex; flex-direction: row; ">
        <h1 class="text-center mb-4" style="flex: 1; justify-content: center">
            Your Trips
        </h1>
    </div>

    <div class="card-body d-flex justify-content-between align-items-center">
        <AddButton path="/home/trips/create-trip" />
        <div>
            <SearchForm />
        </div>
    </div>
    {#each trips as trip, i}
        {#if i % 3 == 0}
            <div class="grid grid-cols-3 h-screen">
                <!-- placeholder -->
                <div class="border-black border"></div>
            </div>
        {/if}
    {/each}
</div>
