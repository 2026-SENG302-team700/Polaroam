<script lang="ts">
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";

    let {
        query = $bindable(""),
        placeholder = "search your images",
        selectedTag = $bindable(""),
        performSearch,
        getSuggestionsUrl,
        onClear,
    }: {
        query?: string;
        placeholder?: string;
        selectedTag?: string;
        performSearch?: () => void;
        getSuggestionsUrl?: string;
        onClear?: () => void;
    } = $props();

    let isOpen = $state(false);
    let items = $state<string[]>([]);
    let loading = $state(false);
    let error = $state("");
    let searchContainer: HTMLDivElement;

    let debounceTime = 250;
    let debounceTimer: ReturnType<typeof setTimeout>;

    /**
     * Fetches tag suggestions matching the given query for the current user's trips
     * @param query - the partial tag text to search for
     */
    async function fetchTagSuggestions(query: string) {
        const url =
            getSuggestionsUrl != null
                ? getSuggestionsUrl
                : `/api/image/tags?query=${encodeURIComponent(query)}`;

        const response = await fetchWithCsrf(resolve(url as any), {
            method: "GET",
            credentials: "include",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch tag suggestions.");
        }

        const data = await response.json();
        return data;
    }

    /**
     * Fetches tag suggestions from the caller-provided function
     */
    async function fetchSuggestions() {
        const trimmed = query.trim();
        if (!trimmed) {
            items = [];
            isOpen = false;
            return;
        }

        try {
            loading = true;
            error = "";
            items = await fetchTagSuggestions(trimmed);
            isOpen = true;
        } catch (err) {
            error = "Failed to fetch tag suggestions: " + (err as Error).message;
            items = [];
        } finally {
            loading = false;
        }
    }

    function handleInput() {
        selectedTag = "";
        error = "";

        // Debounce
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            fetchSuggestions();
            performSearch?.();
        }, debounceTime);
    }

    /**
     * Returns all suggestion buttons currently in the dropdown
     */
    function getButtons() {
        return Array.from(
            searchContainer.querySelectorAll<HTMLButtonElement>(
                ".dropdown-list li button",
            ),
        );
    }

    /**
     * Close the dropdown only when focus leaves the whole component
     * @param event - the focus event
     */
    function handleFocusOut(event: FocusEvent) {
        const next = event.relatedTarget as Node | null;
        if (!next || !searchContainer.contains(next)) {
            isOpen = false;
        }
    }

    /**
     * Handles keys pressed in the search input
     * @param event - the keyboard event
     */
    function handleKeydown(event: KeyboardEvent) {
        if (event.key === "Enter") {
            event.preventDefault();
            isOpen = false;
            performSearch?.();
        } else if (event.key === "ArrowDown" && isOpen) {
            event.preventDefault();
            getButtons()[0]?.focus();
        } else if (event.key === "Escape") {
            isOpen = false;
        }
    }

    /**
     * Handles arrow keys and Escape while focus is inside the dropdown list
     * @param event - the keyboard event
     */
    function handleListKeydown(event: KeyboardEvent) {
        const buttons = getButtons();
        const i = buttons.indexOf(document.activeElement as HTMLButtonElement);

        if (event.key === "ArrowDown") {
            event.preventDefault();
            buttons[Math.min(i + 1, buttons.length - 1)]?.focus();
        } else if (event.key === "ArrowUp") {
            event.preventDefault();
            if (i <= 0) searchContainer.querySelector("input")?.focus();
            else buttons[i - 1].focus();
        } else if (event.key === "Escape") {
            isOpen = false;
            searchContainer.querySelector("input")?.focus();
        }
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
        searchContainer.querySelector("input")?.focus();
        performSearch?.();
    }

    /**
     * Clears the search and notifies the parent
     */
    function handleClear() {
        query = "";
        selectedTag = "";
        error = "";
        isOpen = false;
        if (onClear) {
            onClear();
        } else {
            performSearch?.();
        }
    }
</script>

<div
        class="position-relative"
        bind:this={searchContainer}
        onfocusout={handleFocusOut}
>
    <div class="input-group gap-1">
        <input
                type="search"
                class="form-control search-input"
                class:is-invalid={Boolean(error)}
                {placeholder}
                bind:value={query}
                oninput={handleInput}
                onkeydown={handleKeydown}
        />
        <button
                type="button"
                title="search your trips using the provided query."
                class="btn btn-outline-secondary"
                onclick={handleClear}
        >
            Clear
        </button>
    </div>

    {#if isOpen}
        <!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
        <ul
                class="dropdown-menu show w-100 top-100 mt-1 p-1 shadow overflow-y-auto dropdown-list"
                onkeydown={handleListKeydown}
        >
            {#each items as item}
                <li>
                    <button
                            type="button"
                            class="dropdown-item text-wrap border rounded mb-1 item-highlight"
                            onclick={() => selectItem(item)}
                    >
                        {item}
                    </button>
                </li>
            {:else}
                <li>
                    <span
                            class="dropdown-item-text fst-italic text-body-secondary"
                    >
                        No images found matching your search
                    </span>
                </li>
            {/each}
        </ul>
    {/if}

    {#if error}
        <div class="invalid-feedback d-block">{error}</div>
    {/if}
</div>

<style>
    .search-input {
        background-color: #fffdf7;
    }

    .dropdown-list {
        max-height: 200px;
    }

    .item-highlight {
        --bs-dropdown-link-hover-bg: var(--bs-primary-bg-subtle);
        --bs-dropdown-link-hover-color: var(--bs-primary-text-emphasis);
    }

    .item-highlight:hover,
    .item-highlight:focus {
        border-color: var(--bs-primary) !important;
    }
</style>