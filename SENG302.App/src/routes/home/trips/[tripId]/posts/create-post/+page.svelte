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
    import LocationForm from "$lib/components/forms/location-form.svelte";
    
    let loading = $state(false);
    const tripId = page.params.tripId;
    
    let title = $state("")
    let date = $state(new Date())
    let searchQuery = $state("")
    let location = $state({
        osmId : 0,
        lat : 0,
        lon : 0,
        name : "",
        displayName : "",
    })
    let content = $state("")
    
    let errors = {
        title: [],
        description: [],
        date: [],
        generic: "",
    }
    
    async function createPost() {
        title = title.trim();
        content = content.trim();
        
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
                    OsmId: location.osmId,
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
        <LocationForm bind:searchQuery bind:location/>
        <DescriptionForm placeholder="Content" bind:description={content}/>
        <MultipleErrorDisplay errors={[errors.generic]}/>
        <div class="d-grid">
            <CreationButton onClick={createPost} text="Create Post" />
        </div>
    </div>
</div>