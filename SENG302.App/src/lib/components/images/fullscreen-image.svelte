<script lang="ts">
    let {
        image,
    }: { image: File; } = $props();


</script>

<div class="fullscreen-image-wrapper">
 <div class="image-container">

    <img src={URL.createObjectURL(image)} alt="Image" class="image" />
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
    position: absolute;
    max-width: 70%;
    max-height: 70%;
 }
</style>