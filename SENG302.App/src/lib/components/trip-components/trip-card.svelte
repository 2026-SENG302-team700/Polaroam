<script lang="ts">
    import { resolve } from "$app/paths";
    import { goto } from "$app/navigation";
    import { formatDate } from "$lib/datepicker/formatDate";
    import type { trip } from "./trip-tools";

    let { tripObject }: { tripObject: trip } = $props();
</script>

<div
    tabindex="0"
    role={"button"}
    class="trip-container"
    onclick={() => goto(resolve(`/home/trips/${tripObject.id}`))}
    onkeydown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
            goto(resolve(`/home/trips/${tripObject.id}`));
        }
    }}
>
    <h1>{tripObject.Title}</h1>
    <span
        >{formatDate(tripObject.StartDate.toDateString())} - {formatDate(
            tripObject.EndDate.toDateString(),
        )}</span
    >
    <p>{tripObject.Description}</p>
</div>

<style>
    .trip-container {
        border-color: black;
        border-style: solid;
        border-width: 2px;
    }
</style>
