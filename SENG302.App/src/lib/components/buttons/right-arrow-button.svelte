<script lang="ts">
    
    let {class: className = "", 
        image = $bindable(),
        index = $bindable(),
        images = $bindable()} : {class?:string, 
        image: File,
        index: number,
        images: File[] | null} = $props();

    function moveToRightImage()
    {
        index += 1
        let newImage = images?.[index];
        if (newImage !== undefined) {
            image = newImage
        }
    }
</script>

<button class="btn btn-secondary bi-arrow-right {className}" title="right-arrow" onclick={moveToRightImage}></button>


<style>
    .btn{
        top: 50%;
        left: 102%;
    }
</style>