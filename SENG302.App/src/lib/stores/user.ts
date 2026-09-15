import { writable } from "svelte/store";

export const user = writable<{
    id: number | null;
    displayName: string | null;
    profilePicture: number | null;
    pfpData: PfpData | null;
}>({
    id: null,
    displayName: null,
    profilePicture: null,
    pfpData: null,
});