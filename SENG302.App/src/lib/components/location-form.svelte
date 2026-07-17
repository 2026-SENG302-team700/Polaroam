<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";

    let {
        location = $bindable(""),
        error = "",
        loading = false
    } = $props();
    
    async function getSearchResults() {
        try {
            loading = true;
            error = "";
            //searchParams = new URLSearchParams({
            //    q: location,
            //    format: "json"
            //});
            console.log("before fetch");
            const response = await fetch(
                `https://nominatim.openstreetmap.org/search?q=mcdonalds&format=json`,
                {
                    method: "GET",
                },
            );
            const data = await response.json();
            console.log("finishe dread");
            console.log(data);
            if (!response.ok) {
                error = data;
                return;
            }
            
        } catch (err) {
            error = "Failed to get tasks: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
    
    
    $effect(() => {
        
    });
    
</script>

<div class="mb-3">
    <input
            type="text"
            class="form-control"
            placeholder={"Location *"}
            bind:value={location}
            on:change={getSearchResults}
    />
    <ul class="dropdown-menu">
        
    </ul>
</div>
