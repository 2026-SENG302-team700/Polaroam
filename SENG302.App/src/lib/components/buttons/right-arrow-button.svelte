<script lang="ts">
    
    let {class: className = "", 
        image = $bindable(),
        index = $bindable(),
        images = $bindable(),
        moveToRightImage,
    } : {class?:string, 
        image: File,
        index: number,
        images: File[] | null
        moveToRightImage: () => void,
    } = $props();
    
</script>

<button class="btn btn-secondary bi-arrow-right {className}" 
        title="right-arrow" 
        onclick={moveToRightImage}
></button>


<style>
    .btn{
        top: 50%;
        left: 102%;
    }
</style>