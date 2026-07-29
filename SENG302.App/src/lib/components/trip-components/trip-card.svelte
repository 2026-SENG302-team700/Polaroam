<script lang="ts">
    import { resolve } from "$app/paths";
    import { goto } from "$app/navigation";
    import { formatDate } from "$lib/datepicker/formatDate";
    import type { trip } from "./trip-tools";
    import defaultProfile from '$lib/assets/images/defaultProfile.png'

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
    <img class="trip-image" src={defaultProfile} />
    <div class="trip-text-wrapper">
        <h1>{tripObject.title}</h1>
        <span class="dates">
            {formatDate(tripObject.startDate)} - 
            {formatDate(tripObject.endDate,)}
        </span>
    </div>
    
    
</div>

<style>
    .trip-container {

        border: 2px solid #686868;
        border-radius: 10px;
        margin-top: 4px;
        margin-bottom: 4px;
        justify-self: center;
        width: 98%;
        display: grid;
        position: relative;
    }

    .trip-image {
        border-radius: 8px;
        aspect-ratio: 1 / 1;
        display: block;
        object-fit: cover;
        z-index: 0;
        position: relative;
        width: 100%;
        height: 100%;
    }

    .trip-text-wrapper {
        margin-left: 5px;
        position:absolute;
        bottom: 0;
        z-index: 0;
        text-shadow: -1px -1px 0 #ffffff, 1px -1px 0 #ffffff,
          -1px 1px 0 #ffffff, 1px 1px 0 #ffffff;
    }
    .trip-image .trip-image-wrapper {
        grid-column: 1;
        grid-row: 1;
    }
    .dates {
        font-size: larger;
    }
</style>
