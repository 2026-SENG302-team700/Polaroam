<script lang="ts">
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import SearchForm from "$lib/components/forms/search-form.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import LocationForm from "$lib/components/forms/location-form.svelte";
    
    let title = $state("")
    let date = $state(new Date())
    let searchQuery = $state("")
    let content = $state("")
    let location = $state({
        osmId : 0,
        lat : 0,
        lon : 0,
        name : "",
        displayName : "",
    })
    
    
    async function createPost() {
        console.log(title)
        console.log(date)
        console.log(location)
        console.log(content)
    }
</script>



<div class="container d-flex justify-content-center align-items-center">
    <div class="w-100" style="max-width: 600px;">
        <TitleForm bind:name={title}/>
        <DatePicker bind:value={date}/>
        <LocationForm bind:searchQuery bind:location/>
        <DescriptionForm placeholder="Content" bind:content={content}/>
        <div class="d-grid">
            <CreationButton onClick={createPost} text="Create Post" />
        </div>
    </div>
</div>