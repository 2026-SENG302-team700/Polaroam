<script lang="ts">
    let {class: className = "",
        image = $bindable(),
        index = $bindable(),
        images = $bindable()} : {class?:string,
        image: File,
        index: number,
        images: File[] | null} = $props();

    function moveToLeftImage()
    {
        index -= 1
        let newImage = images?.[index];
        if (newImage !== undefined) {
            image = newImage
        }
    }
</script>

<button class="btn btn-secondary bi-arrow-left {className}" title="left-arrow" onclick={moveToLeftImage}></button>


<style>
    .btn{
        top: 50%;
        right: 102%;
    }
</style>