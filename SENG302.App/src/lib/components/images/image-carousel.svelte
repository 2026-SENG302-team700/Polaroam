<script lang="ts">
    import RightArrowButton from "../buttons/right-arrow-button.svelte"
    import LeftArrowButton from "../buttons/left-arrow-button.svelte"
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";

    let {post} = $props();

   let index = $state(0);
   let photos:File[] = $state([]);
   let image = <File>$state();
   let imageUrl = $state('');
   onMount(()=>{fetchImages();});

   function moveToLeftImage()
    {
       index -= 1
       let newImage = photos[index];
       if (newImage !== undefined) {
          image = newImage
          updateImageUrl();
       }
    }

   function moveToRightImage()
    {
       index += 1
       let newImage = photos[index];
       if (newImage !== undefined) {
          image = newImage;
          updateImageUrl();
       }
    }

   function updateImageUrl() {
    if (imageUrl) {
      URL.revokeObjectURL(imageUrl); // Clean up old URL
    }
    imageUrl = image ? URL.createObjectURL(image) : '';
  }
    
    /**
     * Fetch Images
     */
    async function fetchImages() {
        try {
            
            const response = await fetchWithCsrf(
                resolve(`/api/post/${post.postId}/images` as any), {
                    method: "GET",
                    credentials: "include",
                },
            );
            if (!response.ok) {
                error = (await response.text()) || "Failed to fetch images.";
                return;
            }

            const metadata: {
                photoId: number;
                fileName: string;
                mimeType: string;
            }[] = await response.json();

            photos = await Promise.all(
                metadata.map(async (meta) => {
                    const res = await fetchWithCsrf(
                        resolve(`/api/post/${post.postId}/images/${meta.photoId}` as any),
                        {
                            method: "GET",
                            credentials: "include"
                        }
                    );
                    const blob = await res.blob();
                    return new File([blob], meta.fileName, { type: meta.mimeType });
                }),
            )
            console.log(photos);
            image = photos[0];
            updateImageUrl();
      
        
        } catch (err) {
            error = "Failed to fetch images: " + (err as Error).message;
        }
    }


</script>

<svelte:window onkeydown={(e) => {
         if (e.key === "ArrowLeft" && index!==0) {
            moveToLeftImage()
         } else if (e.key === "ArrowRight" && photos !== null && index!==photos.length-1) {
            moveToRightImage()
            }
      }
   }
/>
<div class="fullscreen-image-wrapper">
   <div class="image-container">
      <div class="close-button-wrapper">
         <img src={imageUrl} alt="Image" class="image" />
         {#if photos !== null && photos?.length > 1 && index !== photos?.length-1}
            <RightArrowButton class="position-absolute" bind:image={image} bind:index={index} bind:images={photos} {moveToRightImage}/>
         {/if}
         {#if photos !== null && photos?.length > 1 && index !== 0}
            <LeftArrowButton class="position-absolute" bind:image={image} bind:index={index} bind:images={photos} {moveToLeftImage}/>
         {/if}
      </div>    
   </div>  
</div>

<style>

 .image-container {
    position: relative;
    width: 30%;
    height: 30%;
    display: flex;
    justify-content: center;
    align-items: center;
 }
 .image {
    position: relative;
    max-height: 100%;
    max-width: 100%;
 }
 .close-button-wrapper {
   position:relative;
 }
 .fullscreen-image-wrapper {
   display: flex;

   justify-content: center;
 }

</style>