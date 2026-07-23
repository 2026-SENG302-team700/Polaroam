<script lang="ts">
    import TitleForm from "$lib/components/forms/title-form.svelte";
    import DatePicker from "$lib/datepicker/datepicker.svelte";
    import SearchForm from "$lib/components/forms/search-form.svelte";
    import DescriptionForm from "$lib/components/forms/description-form.svelte";
    import CreationButton from "$lib/components/buttons/creation-button.svelte";
    import ImagePicker from "$lib/components/image-picker.svelte";
    import {fetchWithCsrf} from "$lib/csrf";
    
    let title = $state("")
    let date = $state(new Date())
    let searchQuery = $state("")
    let content = $state("")
    let files: FileList | null = $state(null);
    
    
    async function createPost() {
        // Code for sending image to backend
        const response = await fetchWithCsrf(resolve(`/api/user/pfp` as any), {
            method: "PUT",
            body: formData,
        });
    }
</script>



<div class="container d-flex justify-content-center align-items-center">
    <div class="w-100" style="max-width: 600px;">
        <TitleForm bind:name={title}/>
        <DatePicker bind:value={date}/>
        <SearchForm bind:query={searchQuery} placeholder="Location *"/>
        <DescriptionForm placeholder="Content" bind:content={content}/>
        <ImagePicker files={files}></ImagePicker>
        <div class="d-grid">
            <CreationButton onClick={createPost} text="Create Post" />
        </div>
    </div>
</div>