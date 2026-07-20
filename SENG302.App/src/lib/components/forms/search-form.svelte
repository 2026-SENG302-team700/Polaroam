<script lang="ts">
    let { query = $bindable(" "), performSearch = $bindable(false) } = $props();
</script>

<div class="grid grid-cols-5 gap-2">
    <input
        class="col-span-4 border-solid border-2"
        type="search"
        placeholder="search your trips"
        bind:value={query}
    />
    <button
        type="button"
        title="search your trips using the provided query."
        class="border-solid border-2"
        onclick={() => (performSearch = true)}
    >
        <i class="bi bi-search"></i>
    </button>
</div>
