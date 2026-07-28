import {fetchWithCsrf} from "$lib/csrf";
import { goto } from "$app/navigation";
import { page } from "$app/state";
import { resolve } from "$app/paths";

/**
 * Function for uploading photos to the backend for a particular post
 * @param postId Id of the post
 * @param tripId Id of the trip
 * @param files the files to upload
 */
 export async function uploadPhotos(postId: number, tripId: number, files: File[] | null | undefined): Promise<string | undefined> {
    if (files != null) {
        try {
            const formData = new FormData();
            for (let i = 0; i < files.length; i++) {
                formData.append("files", files[i]);
                formData.append("postId", postId.toString());
                formData.append("tripId", tripId.toString());
            }

            const response = await fetchWithCsrf(
                resolve(`/api/post/images` as any),
                {
                    method: "PUT",
                    body: formData,
                },
            );
            
            if (!response.ok) {
                if (response.status == 500) {
                    return "Failed to upload picture";
                } else {
                    return await response.text();
                }
            } 
        } catch (err) {
            return (err as Error).message;
        }
    }
    goto(resolve(`/home/trips/${page.params.tripId}`));
}