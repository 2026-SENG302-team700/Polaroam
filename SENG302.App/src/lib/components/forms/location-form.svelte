<script lang="ts">
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";

    type LocationResult = {
        osmId: number;
        osmType: string;
        lat: string;
        lon: string;
        name: string;
        displayName: string;
    };

    let {
        searchQuery = $bindable(""),
        location = $bindable({
            osmId: 0,
            lat: 0,
            lon: 0,
            name: "",
            displayName: "",
        }),
        error = $bindable(""),
        loading = $bindable(false),
    } = $props();

    let isOpen = $state(false);
    let items = $state<LocationResult[]>([]);

    function handleInput() {
        location = {
            osmId: 0,
            osmType: "",
            lat: 0,
            lon: 0,
            name: "",
            displayName: "",
        };

        error = "";
    }

    /**
     * Gets the search results from the backend
     * @param event - the keyboard event
     */
    async function getSearchResults() {
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

            const data = await response.json();

            if (!response.ok) {
                error = "Failed to search locations.";
                return;
            }

            items = data.map(
                (item: any): LocationResult => ({
                    osmId: item.osm_id,
                    osmType: item.osm_type,
                    lat: item.lat,
                    lon: item.lon,
                    name: item.name,
                    displayName: item.display_name,
                }),
            );
            isOpen = true;
        } catch (err) {
            error = "Failed to search locations: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }

    /**
     * Triggers search on Enter key
     * @param event - the keyboard event
     */
    function handleKeydown(event: KeyboardEvent) {
        if (event.key !== "Enter") return;
        event.preventDefault();
        getSearchResults();
    }

    /**
     * Sets the selected item to the location object
     * @param item - location object
     */
    function selectItem(item: LocationResult) {
        location = {
            osmId: item.osmId,
            osmType: item.osmType,
            lat: item.lat,
            lon: item.lon,
            name: item.name,
            displayName: item.displayName,
        };
        searchQuery = item.name;
        error = "";
        isOpen = false;
    }
</script>

<div class="search-container mb-3">
    <div class="input-group" style="gap:2px;">
        <input
            type="text"
            class="form-control"
            class:is-invalid={Boolean(error)}
            placeholder="Search location... *"
            bind:value={searchQuery}
            oninput={handleInput}
            onkeydown={handleKeydown}
            onblur={() => setTimeout(() => (isOpen = false), 200)}
            disabled={loading}
        />
        <button
            type="button"
            title="search your trips using the provided query."
            class="btn btn-outline-secondary"
            onclick={getSearchResults}
            disabled={loading}
        >
            {#if loading}
                <div
                    class="spinner-border spinner-border-sm text-primary"
                    role="status"
                />
            {:else}
                <i class="bi bi-search"></i>
            {/if}
        </button>
    </div>

    {#if isOpen && items.length > 0}
        <ul class="dropdown-list">
            {#each items as item}
                <li>
                    <button type="button" onclick={() => selectItem(item)}>
                        {item.name}<br />
                        <span class="item-display-name">{item.displayName}</span
                        >
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
