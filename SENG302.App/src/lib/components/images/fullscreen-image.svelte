<script lang="ts">
    import CloseButton from "../buttons/close-button.svelte";
    import RightArrowButton from "../buttons/right-arrow-button.svelte"
    import LeftArrowButton from "../buttons/left-arrow-button.svelte"
    import TagPill from "../content/tag-pill.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf";
    import { resolve } from "$app/paths";

    // Allow use of file paths (Via the api) or actual File objects.
    type ImageSource = File | string;
    
    let {
       image,
       enlarged = $bindable(false),
        index = $bindable(0),
       images,
       photoId,
       photoIds,
    }: { 
        image: ImageSource,
        enlarged: boolean,
        index: number,
        images: ImageSource[] | null,
        photoId: number,
        photoIds: number[],
       } = $props();

   let tags: string[] = $state([]);
   let error = $state("");
   let loading = $state(false);

   let tagStatus = $state("No tags available");
   let tagColour = $state("secondary");
   
   onMount(() => {
       fetchImageTags(photoId)
   });

    // Only File sources need an object URL; string sources are used as-is.
    let objectUrl = $state<string | null>(null);

    // Create an object URL for File sources.
    $effect(() => {
        if (image instanceof File) {
            const url = URL.createObjectURL(image);
            objectUrl = url;
            return () => URL.revokeObjectURL(url);
        }
        objectUrl = null;
    });

    // Sets scr to url for File sources
    let displaySrc = $derived(image instanceof File ? objectUrl : image);



    function moveToLeftImage() {
        if (images === null || index === 0) return;
        const newImage = images[index - 1];
        if (newImage !== undefined) {
            index -= 1;
            image = newImage;
        }
        fetchImageTags(photoIds[index])
    }

    function moveToRightImage() {
        if (images === null || index === images.length - 1) return;
        const newImage = images[index + 1];
        if (newImage !== undefined) {
            index += 1;
            image = newImage;
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

           
           
            const message = await response.text();
            
            
            if (message === "Tags are being generated.") {
               tagStatus = "Tags are being generated. Come back later";
               tagColour = "info";
               return;
            }
           
            tags = JSON.parse(message);
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
          {#if displaySrc}
              <img src={displaySrc} alt="Image" class="image" />
          {/if}
          <CloseButton bind:close={enlarged} class="position-absolute btn-sm" />
          {#if images !== null && images.length > 1 && index !== images.length - 1}
              <RightArrowButton class="position-absolute" bind:image={image} bind:index={index} bind:images={images} {moveToRightImage}/>
          {/if}
          {#if images !== null && images.length > 1 && index !== 0}
              <LeftArrowButton class="position-absolute" bind:image={image} bind:index={index} bind:images={images} {moveToLeftImage}/>
          {/if}
         <div class="tags-wrapper">
            {#if tags.length > 0}
               {#each tags as tag}
                  <TagPill {tag}/>
               {/each}
            {:else}
               <TagPill {tagColour} tag={tagStatus}/>
            {/if}
         </div>
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
    margin-top: 6px;
 }

</style>