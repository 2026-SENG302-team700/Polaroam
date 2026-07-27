<script lang="ts">
    import { page } from "$app/state";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import {fetchWithCsrf} from "$lib/csrf"
    
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import SearchForm from "$lib/components/forms/search-form.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import MultipleErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import ImagePicker from "$lib/components/image-picker.svelte";
    
    let loading = $state(false);
    const tripId = page.params.tripId;
    
    let title = $state("")
    let date = $state(new Date())
    let searchQuery = $state("")
    let location = $state("Temporary")
    let content = $state("")
    let files: FileList | null = $state(null);
    
    let errors = $state({
        title: [],
        description: [],
        date: [],
        generic: "",
        photos: [],
    })

    /**
     * Function for creating a post
     */
    async function createPost() {
        title = title.trim();
        content = content.trim();
        
        try {
            loading = true;

            const form = new FormData();
            form.append("TripId", tripId);
            form.append("Title", title);
            form.append("Location", location);
            form.append("Content", content);
            form.append("Date", date.toISOString());
            if (files != null) {
                for (let i = 0; i < files.length; i++) {
                    form.append("files", files[i]);
                }
            }
            
            const response = await fetchWithCsrf(`/api/post`, {
                method: "POST",
                body: form,
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                errors.generic = data.message;
                errors.photos = data.errors?.photos ?? [];
                return;
            }
            
            goto(resolve(`/home/trips/${tripId}`));
        } catch (err) {
            console.error("Failed to create post:", err);
            errors.generic = "Failed to create post: " + (err as Error).message;
        } finally {
            loading = false;
        }
        
    }
</script>



<div class="container d-flex justify-content-center align-items-center">
    <div class="w-100" style="max-width: 600px;">
        <TitleForm bind:name={title}/>
        <DatePicker bind:value={date}/>
        <SearchForm bind:query={searchQuery} placeholder="Location *"/>
        <DescriptionForm placeholder="Content" bind:description={content}/>
        <ImagePicker bind:files errors={errors.photos}></ImagePicker>
        <MultipleErrorDisplay errors={[errors.generic]}/>
        <div class="d-grid">
            <CreationButton onClick={createPost} text="Create Post" />
        </div>
    </div>
</div>