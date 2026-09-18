<script lang="ts">
    type ImageSource = File | string;
    
    let {
        src,
        image
    } : {
        src: string,
        image: ImageSource,
    } = $props()
    
    let objectUrl = $state<string | null>(null);
    let displaySrc = $derived(image instanceof File ? objectUrl : image);
    
    let image = $state<File | null>(null);

    let file = $state<File | null>(null);
    let loading = $state(false);
    let failed = $state(false);
    

    $effect(() => {
        if (image instanceof File) {
            const url = URL.createObjectURL(image);
            objectUrl = url;
            return () => URL.revokeObjectURL(url);
        }

        objectUrl = null;
    });

    function filenameFromSrc(url: string) {
        try {
            const path = new URL(url, window.location.origin).pathname;
            return path.split("/").pop() || "image";
        } catch {
            return "image";
        }
    }

    async function loadFile() {
        if (file || loading) return;
        loading = true;
        failed = false;
        try {
            const res = await fetch(src);
            if (!res.ok)
                throw new Error(`Failed to fetch image: ${res.status}`);
            const blob = await res.blob();
            file = new File([blob], filenameFromSrc(src), { type: blob.type });
        } catch (e) {
            console.error(e);
            failed = true;
        } finally {
            loading = false;
        }
    }
    
    async function clicked() {
        console.log("clicked")
    }
    
</script>

<img
        src={displaySrc}
        onclick={clicked}
        alt="Image"
        class="img-thumbnail clickable-image"
/>

<style>
    .img-thumbnail {
        width: 100%;
        cursor: pointer;
        aspect-ratio: 1;
        object-fit: cover;
    }
</style>