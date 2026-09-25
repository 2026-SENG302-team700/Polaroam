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
            osmType: "",
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
    let searchContainer: HTMLDivElement;

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
        const nextElement = event.relatedTarget as Node | null;

        if (!nextElement || !searchContainer.contains(nextElement)) {
            isOpen = false;
        }
    }

    /**
     * Set the base variable to defaults
     */
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
     * Handles keys pressed in the search input
     * @param event - the keyboard event
     */
    function handleKeydown(event: KeyboardEvent) {
        if (event.key === "Enter") {
            event.preventDefault();
            getSearchResults();
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
        searchQuery = item.name == "" ? item.displayName : item.name;
        error = "";
        isOpen = false;
        searchContainer.querySelector("input")?.focus();
    }
</script>

<div
        class="position-relative mb-3"
        bind:this={searchContainer}
        onfocusout={handleFocusOut}
>
    <div class="input-group gap-1">
        <input
                type="text"
                class="form-control"
                class:is-invalid={Boolean(error)}
                placeholder="Search location... *"
                bind:value={searchQuery}
                oninput={handleInput}
                onkeydown={handleKeydown}
                readonly={loading}
                aria-busy={loading}
        />
        <button
                type="button"
                title="search locations using the provided query."
                class="btn btn-outline-secondary"
                onclick={getSearchResults}
                aria-disabled={loading}
        >
            {#if loading}
                <div
                        class="spinner-border spinner-border-sm text-primary"
                        role="status"
                ></div>
            {:else}
                <i class="bi bi-search"></i>
            {/if}
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
                        {item.name}<br />
                        <span class="small fst-italic text-body-secondary">
                            {item.displayName}
                        </span>
                    </button>
                </li>
            {:else}
                <li>
                    <span
                            class="dropdown-item-text fst-italic text-body-secondary"
                    >
                        No items found
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