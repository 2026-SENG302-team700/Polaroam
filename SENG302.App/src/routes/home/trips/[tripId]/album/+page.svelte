    <script lang="ts">
        import ImageGallery from '$lib/components/images/image-gallery.svelte';
        import { onMount } from 'svelte';
        import { resolve } from '$app/paths';
    
        let { params } = $props();
        let tripId = params.tripId;
        let imageMetadata = $state([]);
        
        async function getImageMetadata() {
            const response = await fetch(resolve(`/api/album/${tripId}`),
            {
                method: 'GET',
                credentials: 'include'
            });
            imageMetadata = await response.json();
        }
        
        onMount(getImageMetadata);
    </script>
    
    <ImageGallery {imageMetadata} {tripId}/>
    