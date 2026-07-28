<script lang="ts">
    let {class: className = "",
        image = $bindable(),
        index = $bindable(),
        images = $bindable(),
        moveToLeftImage,
    } : {class?:string,
        image: File,
        index: number,
        images: File[] | null,
        moveToLeftImage: () => void,
    } = $props();
    
</script>

<button class="btn btn-secondary bi-arrow-left {className}" 
        title="left-arrow" 
        onclick={moveToLeftImage}
></button>


<style>
    .btn{
        top: 50%;
        right: 102%;
    }
</style>