<script>
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    
    let searchQuery = $state("");
    let isOpen = $state(false);
    let selectedItem = $state("");
    let items = $state([]);

    async function handleKeydown(event) {
        if (event.key === "Enter") {
            try {
                const url = new URL(resolve(`/api/geo/search`), window.location.origin);
                url.searchParams.set("query", searchQuery);
                
                const response = await fetchWithCsrf(
                    url.toString(),
                    {
                        method: "GET",
                    })
                const data = await response.json();
                items = data;
            } catch (err) {
                console.error("Error fetching data:", err);
            }
        }
    }

    function selectItem(item) {
        selectedItem = item;
        searchQuery = item; // Fill input with selected item
        isOpen = false;     // Hide dropdown after selection
    }
</script>

<div class="search-container">
    <!-- Search Input Field -->
    <input
            type="text"
            placeholder="Search location..."
            bind:value={searchQuery}
            onfocus={() => isOpen = true}
            onblur={() => setTimeout(() => isOpen = false, 200)}
            onkeydown={handleKeydown}
    />

    <!-- Dropdown Menu -->
    {#if isOpen && items.length > 0}
        <ul class="dropdown-list">
            {#each items as item}
                <li>
                    <button type="button" onclick={() => selectItem(item.name)}>
                        {item.name}
                    </button>
                </li>
            {/each}
        </ul>
    {:else if isOpen}
        <ul class="dropdown-list no-results">
            <li>No items found</li>
        </ul>
    {/if}
    
    <label>{selectedItem}</label>
</div>

<style>
    .search-container {
        position: relative;
        width: 300px;
        font-family: sans-serif;
    }

    input {
        width: 100%;
        padding: 10px;
        box-sizing: border-box;
        border: 1px solid #ccc;
        border-radius: 4px;
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
        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
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
</style>
