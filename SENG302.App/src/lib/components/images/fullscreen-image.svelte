<script lang="ts">
    import CloseButton from "../buttons/close-button.svelte";
    import RightArrowButton from "../buttons/right-arrow-button.svelte"
    import LeftArrowButton from "../buttons/left-arrow-button.svelte"

    // Allow use of file paths (Via the api) or actual File objects.
    type ImageSource = File | string;

    let {
        image,
        enlarged = $bindable(false),
        index = $bindable(0),
        images
    }: {
        image: ImageSource;
        enlarged: boolean;
        index: number;
        images: ImageSource[] | null;
    } = $props();

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
    }

    function moveToRightImage() {
        if (images === null || index === images.length - 1) return;
        const newImage = images[index + 1];
        if (newImage !== undefined) {
            index += 1;
            image = newImage;
        }
    }
</script>

<svelte:window onkeydown={(e) => {
        if (e.key === "ArrowLeft" && images !== null && index !== 0) {
            moveToLeftImage()
        } else if (e.key === "ArrowRight" && images !== null && index !== images.length - 1) {
            moveToRightImage()
        } else if (e.key === "Escape") {
            enlarged = false
        }
    }}
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