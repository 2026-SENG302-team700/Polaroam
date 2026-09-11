<script lang="ts">
    
    let {class: className = "", 
        image = $bindable(),
        index = $bindable(),
        images = $bindable(),
        moveToRightImage,
    } : {class?:string, 
        image: File | string,
        index: number,
        images: (File | string)[] | null
        moveToRightImage: () => void,
    } = $props();
    
</script>

<button class="btn btn-secondary bi-arrow-right {className}" 
        title="right-arrow" 
        onclick={moveToRightImage}
></button>
