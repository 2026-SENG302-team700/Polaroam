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
    
    let errors = {
        title: [],
        description: [],
        date: [],
        generic: "",
        image: "",
    }
    
    async function createPost() {
        title = title.trim();
        content = content.trim();

        if (files != null) {
            try {
                const formData = new FormData();
                for (let i = 0; i < files.length; i++) {
                    formData.append("file", files[i]);
                }
                const response = await fetchWithCsrf(resolve(`/api/post/images` as any), {
                    method: "PUT",
                    body: formData,
                });

                let customImageIds: String[];
                if (!response.ok) {
                    if (response.status == 500) {
                        errors.image = "Failed to upload picture"
                    } else {
                        errors.image = await response.text()
                    }
                } else {
                    customImageIds = await response.json()
                    console.log(customImageIds.toString())
                }
            } catch (err) {
                errors.image = (err as Error).message;
            }
        }
        
        try {
            loading = true;

            const response = await fetchWithCsrf(`/api/post`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    TripId: Number(tripId),
                    Title: title,
                    Location: location,
                    Content: content,
                    Date: date,
                }),
                credentials: "include",
            });
            const data = await response.json();

            if (!response.ok) {
                errors.generic = data.message;
                return;
            }

            goto(resolve(`/home/trips/${tripId}`));
        } catch (err) {
            console.error("Failed to create trip:", err);
            errors.generic = "Failed to create trip: " + (err as Error).message;
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
        <ImagePicker files={files}></ImagePicker>
        <MultipleErrorDisplay errors={[errors.generic]}/>
        <div class="d-grid">
            <CreationButton onClick={createPost} text="Create Post" />
        </div>
    </div>
</div>