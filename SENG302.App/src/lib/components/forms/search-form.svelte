    <script lang="ts">
        let { 
            query = $bindable(""), 
            performSearch = $bindable(false),
            placeholder = "search your trips"
        } = $props();
    
    </script>

    <div class="mb-3">
        <div class="input-group">
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
                    onclick={() => (performSearch = true)}
            >
                <i class="bi bi-search"></i>
            </button>
        </div>
    </div>
