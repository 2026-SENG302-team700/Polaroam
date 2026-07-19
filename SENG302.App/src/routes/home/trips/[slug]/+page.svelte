<script lang="ts">
    import type { trip } from "$lib/components/trip-components/trip-tools.ts";
    import { formatDate } from "$lib/datepicker/formatDate";
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    
    let tripObject = {
        id: 0,
        Title: "bob",
        Description: "hello",
        StartDate: new Date(),
        EndDate: new Date(),
    };
</script>

<div class="container">
    <NavigationButton path="/home/trips" text="Back" />
    <h1>{tripObject.Title}</h1>
    <p>{formatDate(tripObject.StartDate.toDateString())} - {formatDate(tripObject.EndDate.toDateString())}</p>
    <div class="border-t-2 border-black"></div>
    <span>{tripObject.Description}</span>
</div>