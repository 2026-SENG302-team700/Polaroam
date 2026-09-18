<script lang="ts">
import { resolve } from "$app/paths";
import { fetchWithCsrf } from "$lib/csrf";

let {type, tripId} = $props();

/**
 * Sends a request to the backend to accept collaboration with the trip whose Id
 * matches the Id in the parameter of this function
 * @param tripId 
 */
async function acceptInvite(tripId:number) {
    try {
        const response = await fetchWithCsrf(resolve(`/home/trips/${tripId}/collaborators`), {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                    Action: 1, //1 for accept
            }),
            credentials: "include",
        });
        const data = await response.json();
        if (response.status == 401) {
            console
        }
    } catch (err) {

    }
}

/**
 * Sends a request to the backend to accept collaboration with the trip whose Id
 * matches the Id in the parameter of this function
 * @param tripId 
 */
async function rejectInvite(tripId:number) {
    try {
        const response = await fetchWithCsrf(resolve(`/home/trips/${tripId}/collaborators`), {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                    Action: 0, //0 for reject
            }),
            credentials: "include",
        });
        const data = await response.json();
        if (response.status == 401) {
            console
        }
    } catch (err) {

    }
}
</script>

{#if type === "leftAction"}
            <button
                    class={`btn action-buttons btn-success`}
                    onclick={() => acceptInvite(tripId)}
            >
                Accept
            </button>
        {/if}
        {#if type = "rightAction"}
            <button
                    class={`btn action-buttons btn-danger`}
                    onclick={() => rejectInvite(tripId)}
            >
                Decline
            </button>
        {/if}