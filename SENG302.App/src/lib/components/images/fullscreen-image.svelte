<script lang="ts">
    import CloseButton from "../buttons/close-button.svelte";
    import RightArrowButton from "../buttons/right-arrow-button.svelte"
    import LeftArrowButton from "../buttons/left-arrow-button.svelte"
    import TagPill from "../content/tag-pill.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";

    let {
       image,
       enlarged = $bindable(false),
       index,
       images,
       photoId,
       photoIds,
    }: { image: File,
        enlarged: boolean,
        index: number,
        images: File[] | null,
        photoId: number,
        photoIds: number[],
       } = $props();

   let tags: string[] = $state([]);
   let error = $state("");
   let loading = $state(false);
   let showRetryTagging = $state(false);
   let aiTaggingPreference: string | null = $state(null)

   onMount(() => {
      getUserPreferences()
   });

    function moveToLeftImage()
    {
       index -= 1
       let newImage = images?.[index];
       if (newImage !== undefined) {
          image = newImage
       }
       showRetryTagging = false
       fetchImageTags(photoIds[index])
    }

    function moveToRightImage()
    {
       index += 1
       let newImage = images?.[index];
       if (newImage !== undefined) {
          image = newImage
       }
       showRetryTagging = false
       fetchImageTags(photoIds[index])
    }
    
    async function getUserPreferences() {
       
       aiTaggingPreference = sessionStorage.getItem("aiTaggingPreference")
       
       if (aiTaggingPreference === null) {
          try {
             loading = true;
             const response = await fetchWithCsrf(
                     resolve(`/api/user` as any), {
                        method: "GET",
                        credentials: "include",
                     },
             );
             const data = await response.json();
             if (!response.ok) {
                error = data.error
                return;
             }
             
             sessionStorage.setItem("aiTaggingPreference", data.aiTagging)
          }
          catch (err) {
             console.log("Cannot get user ai tagging preferences")
          } finally {
             loading = false;
          }
       } else {
          if (aiTaggingPreference === "true") {
             await fetchImageTags(photoId)
          }
       }
    }

    
    async function fetchImageTags(id: number) {
        try {
            loading = true;
            tags = [];
            const response = await fetchWithCsrf(
                resolve(`/api/image/${id}/tags` as any), {
                    method: "GET",
                    credentials: "include",
                },
            );
           
            if (!response.ok) {
               if (response.status === 500) {
                  const data = await response.json()
                  error = "Failed to fetch tags: " + data?.detail
                  
                  if (error === "Failed to fetch tags: Queue was full") {
                     showRetryTagging = true
                  }
                  
                  return
               } else {
                  error = (await response.text()) || "Failed to fetch tags.";
                  return
               }
            }

           if (response.status === 204) {
              error = "Tags not available for image";
           } else if (response.status == 202) {
              error = "Tags are being generated."
           } else {
              tags = await response.json();
           }

        } catch (err) {
            error = "Failed to fetch tags: " + (err as Error).message;
        } finally {
            loading = false;
        }
      }
      
      async function retryImageTagging(id: number) {
         loading = true;
         try {
            tags = [];
            const response = await fetchWithCsrf(
                    resolve(`/api/image/${id}/tags` as any), {
                       method: "PATCH",
                       credentials: "include",
                    },
            );

            if (!response.ok) {
               const data = await response.json()
               error = "Failed to fetch tags: " + data.errors;
            }
            error = "Tags are being generated"
         } catch (err) {
            error = "Failed to retry tagging: " + (err as Error).message;
         } finally {
            loading = false;
         }
      }     
      
</script>

<svelte:window onkeydown={(e) => {
         if (e.key === "ArrowLeft" && index!==0) {
            showRetryTagging = false
            moveToLeftImage()
         } else if (e.key === "ArrowRight" && images !== null && index!==images.length-1) {
            showRetryTagging = false
            moveToRightImage()
         } else if (e.key === "Escape") {
            enlarged = false
         }
      }
   }
/>
<div class="fullscreen-image-wrapper">
   <div class="image-container">
      <div class="close-button-wrapper">
         <img src={URL.createObjectURL(image)} alt="Image" class="image" />
         <CloseButton bind:close={enlarged} class="position-absolute btn-sm" />
         {#if images !== null && images?.length > 1 && index !== images?.length-1}
            <RightArrowButton class="position-absolute" bind:image={image} bind:index={index} bind:images={images} {moveToRightImage}/>
         {/if}
         {#if images !== null && images?.length > 1 && index !== 0}
            <LeftArrowButton class="position-absolute" bind:image={image} bind:index={index} bind:images={images} {moveToLeftImage}/>
         {/if}
         {#if aiTaggingPreference === "true"}
            <div class="tags-wrapper">
               {#if tags.length > 0}
               {#each tags as tag}
                  <TagPill {tag}/>
               {/each}
               {:else}
               <TagPill tag={error}/>
               {/if}
            </div>
            <div class="tags-wrapper">
               {#if showRetryTagging}
                  <button class="btn btn-primary" onclick={() => {
                  showRetryTagging = false
                  retryImageTagging(photoIds[index])
               }}
                  >
                     Retry Tagging
                  </button>
               {/if}
            </div>
         {/if}
      </div>
   </div>
</div>

<style>
 .fullscreen-image-wrapper {
    z-index: 100;
    background-color: rgba(0, 0, 0, 0.7);
    position:fixed;
    top: 0;
    left: 0;
    height: 100vh;
    width: 100vw;  
 }
 .image-container {
    position: relative;
    width: 100%;
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
 }
 .image {
   border-radius: 5px;
    position: relative;
    min-width: 20vw;
    max-height: 70vh;
    max-width: 70vw;
 }
 .close-button-wrapper {
   position:relative;
 }
 .tags-wrapper {
    position: relative;
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
    justify-content: center;
    margin-top: 2%;
 }

</style>