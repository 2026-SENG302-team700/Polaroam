<script lang="ts">
    import CloseButton from "../buttons/close-button.svelte";
    import RightArrowButton from "../buttons/right-arrow-button.svelte"
    import LeftArrowButton from "../buttons/left-arrow-button.svelte"

    let {
        image,
        enlarged = $bindable(false),
       index,
       images  
    }: { image: File,
         enlarged: boolean, 
       index: number, 
       images: File[] | null; } = $props();

    function moveToLeftImage()
    {
       index -= 1
       let newImage = images?.[index];
       if (newImage !== undefined) {
          image = newImage
       }
    }

    function moveToRightImage()
    {
       index += 1
       let newImage = images?.[index];
       if (newImage !== undefined) {
          image = newImage
       }
    }
    
    

</script>

<svelte:window onkeydown={(e) => {
         if (e.key === "ArrowLeft" && index!==0) {
            moveToLeftImage()
         } else if (e.key === "ArrowRight" && images !== null && index!==images.length-1) {
            moveToRightImage()
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

</style>