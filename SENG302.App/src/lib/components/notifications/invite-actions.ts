import { resolve } from "$app/paths";
import { fetchWithCsrf } from "$lib/csrf";

/**
 * Sends a request to the backend to accept collaboration with the trip whose Id
 * matches the Id in the parameter of this function
 * @param tripId 
 */
export async function acceptInvite(tripId:number, handled:boolean) {
    try {
        const response = await fetchWithCsrf(resolve(`/api/trip/${tripId}/collaboration/action` as any), {
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
        handled = true;
        goto(resolve(`/home/trips/${tripId}`))

    } catch (err) {

    }
}

/**
 * Sends a request to the backend to accept collaboration with the trip whose Id
 * matches the Id in the parameter of this function
 * @param tripId 
 */
export async function rejectInvite(tripId:number, handled:boolean) {
    try {
        const response = await fetchWithCsrf(resolve(`/api/trip/${tripId}/collaboration/action` as any), {
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
        handled = true;

    } catch (err) {

    }
}