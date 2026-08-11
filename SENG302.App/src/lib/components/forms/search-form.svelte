<script lang="ts">
    let { 
        query = $bindable(""), 
        performSearch,
        placeholder = "search your trips"
    } = $props();
</script>

<div class="input-group search-group">
    <input
            type="search"
            class="form-control"
            {placeholder}
            bind:value={query}
    />
    <button
            type="button"
            title="search your trips using the provided query."
            class="btn btn-outline-secondary"
            onclick={performSearch}
    >
        <i class="bi bi-search"></i>
    </button>
</div>

<style>
    .search-group {
        display: flex;
        align-items: center;
        gap: 2px;
    }
</style>