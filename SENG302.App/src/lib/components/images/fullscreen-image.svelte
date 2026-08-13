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

   onMount(() => {
       fetchImageTags(photoId)
   });

    function moveToLeftImage()
    {
       index -= 1
       let newImage = images?.[index];
       if (newImage !== undefined) {
          image = newImage
       }
       fetchImageTags(photoIds[index])
    }

    function moveToRightImage()
    {
       index += 1
       let newImage = images?.[index];
       if (newImage !== undefined) {
          image = newImage
       }
       fetchImageTags(photoIds[index])
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
                if (response.status !== 404) {
                    error = (await response.text()) || "Failed to fetch tags.";
                }
                return;
            }

            tags = await response.json();
        } catch (err) {
            error = "Failed to fetch tags: " + (err as Error).message;
        } finally {
            loading = false;
        }
      }
</script>

<svelte:window onkeydown={(e) => {
         if (e.key === "ArrowLeft" && index!==0) {
            moveToLeftImage()
         } else if (e.key === "ArrowRight" && images !== null && index!==images.length-1) {
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
         {#if tags.length > 0}
            <div class="tags-wrapper">
               {#each tags as tag}
                  <TagPill {tag}/>
               {/each}
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
    position: relative;
    max-height: 70vh;
    max-width: 70vw;
 }
 .close-button-wrapper {
   position:relative;
 }
 .tags-wrapper {
    position: absolute;
    bottom: 0.5rem;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
    justify-content: center;
    max-width: 90%;
 }

</style>