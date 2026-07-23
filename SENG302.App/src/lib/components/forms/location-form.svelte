<script lang="ts">
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";

    let {
        location = $bindable(""),
        error = "",
        loading = false
    } = $props();

    let isOpen = $state(false);
    let items = $state([]);
    let searchQuery = $state("");
    async function getSearchResults(event: KeyboardEvent) {
        if (event.key !== "Enter") return;
        event.preventDefault();
        
        if (loading) return;
        
        try {
            loading = true;
            error = "";
            const response = await fetchWithCsrf(
                resolve(
                    `/api/geo/search?query=${encodeURIComponent(searchQuery.trim())}` as any,
                ),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            console.log(Date.now())

            const data = await response.json();
            if (!response.ok) {
                error = data || "Failed to search locations.";
                return;
            }
            items = data;
            isOpen = true;
        } catch (err) {
            error = "Failed to search locations: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    function selectItem(item) {
        location = item.osm_id;
        searchQuery = item.name;
        isOpen = false;
    }
</script>

<div class="search-container mb-3">
    <div class="input-group">
        <input
                type="text"
                class="form-control"
                class:is-invalid={error}
                placeholder="Search location..."
                bind:value={searchQuery}
                onkeydown={getSearchResults}
                onblur={() => setTimeout(() => (isOpen = false), 200)}
        />
        <button
                type="button"
                title="search your trips using the provided query."
                class="btn btn-outline-secondary"
                onclick={getSearchResults}
        >
            {#if loading}
                <div class="spinner-border spinner-border-sm text-primary" role="status" />
            {:else}
                 <i class="bi bi-search"></i>
            {/if}
            
    </div>
    

    {#if isOpen && items.length > 0}
        <ul class="dropdown-list">
            {#each items as item}
                <li>
                    <button type="button" onclick={() => selectItem(item)}>
                        {item.name}<br>
                        <span class="item-display-name">{item.display_name}</span>
                    </button>
                </li>
            {/each}
        </ul>
    {:else if isOpen}
        <ul class="dropdown-list no-results">
            <li>No items found</li>
        </ul>
    {/if}

    {#if error}
        <div class="invalid-feedback d-block">
            {error}
        </div>
    {/if}
</div>

<style>
    .search-container {
        position: relative;
    }

    .dropdown-list {
        position: absolute;
        width: 100%;
        max-height: 200px;
        overflow-y: auto;
        margin: 4px 0 0 0;
        padding: 0;
        list-style: none;
        background: white;
        border: 1px solid #ccc;
        border-radius: 4px;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        z-index: 10;
    }

    .dropdown-list li button {
        width: 100%;
        padding: 10px;
        text-align: left;
        background: none;
        border: none;
        cursor: pointer;
    }

    .dropdown-list li button:hover {
        background-color: #f0f0f0;
    }

    .no-results li {
        padding: 10px;
        color: #666;
        font-style: italic;
    }
    
    .item-display-name {
        font-weight: italics;
        color: gray;
    }
</style>