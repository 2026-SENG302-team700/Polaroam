<script lang="ts">
    import CloseButton from "../buttons/close-button.svelte";

    let {
        image,
        enlarged = $bindable(false),
    }: { image: File,
         enlarged: boolean; } = $props();


</script>

<div class="fullscreen-image-wrapper">
   <div class="image-container">
      <div class="close-button-wrapper">
         <img src={URL.createObjectURL(image)} alt="Image" class="image" />
         <CloseButton bind:close={enlarged} class="position-absolute btn-sm" />
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