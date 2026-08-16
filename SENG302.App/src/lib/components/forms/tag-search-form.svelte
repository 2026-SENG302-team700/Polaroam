<script lang="ts">
    import {resolve} from "$app/paths";

    let {
        query = $bindable(""),
        performSearch,
        placeholder = "search your images",
        selectedTag = $bindable(""),
        getSuggestionsUrl,
    }: {
        query?: string;
        performSearch?: () => void;
        placeholder?: string;
        selectedTag?: string;
        getSuggestionsUrl: string;
    } = $props();

    let isOpen = $state(false);
    let items = $state<string[]>([]);
    let loading = $state(false);
    let error = $state("");

    /**
     * Fetches tag suggestions from the caller-provided function
     */
    async function fetchSuggestions() {
        if (loading) return;

        const trimmed = query.trim();
        if (!trimmed) {
            items = [];
            isOpen = false;
            return;
        }

        try {
            loading = true;
            error = "";
            console.log(getSuggestionsUrl);
            const response = await fetchWithCsrf(
                resolve(getSuggestionsUrl as `/${string}`),      
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            if (!response.ok) {
                error = "Failed to fetch tag suggestions";
            }
            items = await response.json();
            isOpen = true;
        } catch (err) {
            error = "Failed to fetch tag suggestions" + err.message;
            items = [];
        } finally {
            loading = false;
        }
    }
    
    function handleInput() {
        selectedTag = "";
        error = "";
        fetchSuggestions();
    }

    /**
     * Triggers search on Enter key
     * @param event - the keyboard event
     */
    function handleKeydown(event: KeyboardEvent) {
        if (event.key !== "Enter") return;
        event.preventDefault();
        isOpen = false;
        performSearch = true;
    }

    /**
     * Sets the selected tag and closes the dropdown
     * @param tag - the selected tag string
     */
    function selectItem(tag: string) {
        selectedTag = tag;
        query = tag;
        error = "";
        isOpen = false;
    }
</script>

<div class="search-container">
    <div class="input-group search-group">
        <input
                type="search"
                class="form-control"
                class:is-invalid={Boolean(error)}
                {placeholder}
                bind:value={query}
                oninput={handleInput}
                onkeydown={handleKeydown}
                onblur={() => setTimeout(() => (isOpen = false), 200)}
        />
        <button
                type="button"
                title="search your trips using the provided query."
                class="btn btn-outline-secondary"
                onclick={() => (performSearch = true)}
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
                        <span class="item-display-name">{item}</span>
                    </button>
                </li>
            {/each}
        </ul>
    {:else if isOpen}
        <ul class="dropdown-list no-results">
            <li>No tags found</li>
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

    .search-group {
        display: flex;
        align-items: center;
        gap: 2px;
        margin-bottom: 20px;
    }

    .dropdown-list {
        position: absolute;
        top: 100%;
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
</style>