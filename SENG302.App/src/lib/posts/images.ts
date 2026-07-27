/**
 * Function for uploading photos to the backend for a particular post
 * @param postId Id of the post
 */
export async function uploadPhotos(postId: number) {
    if (files != null) {
        try {
            const formData = new FormData();
            for (let i = 0; i < files.length; i++) {
                console.log("File ", files[i]);
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

            let customImageIds: String[];
            if (!response.ok) {
                if (response.status == 500) {
                    errors.photos = "Failed to upload picture";
                } else {
                    errors.photos = await response.text();
                }
            } else {
                customImageIds = await response.json();
                goto(resolve(`/home/trips/${tripId}`));
                console.log(customImageIds.toString());
            }
        } catch (err) {
            errors.photos = (err as Error).message;
        }
    }
}