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
    <h1>{tripObject.title}</h1>
    <span
        >{formatDate(tripObject.startDate.toDateString())} - {formatDate(
            tripObject.endDate.toDateString(),
        )}</span
    >
    <p>{tripObject.description}</p>
</div>

<style>
    .trip-container {
        border-color: black;
        border-style: solid;
        border-width: 2px;
    }
</style>
