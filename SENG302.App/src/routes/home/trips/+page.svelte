<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import SearchForm from "$lib/components/forms/search-form.svelte";
</script>

<div class="container">
    <div style="display: flex; flex-direction: row; ">
        <h1 class="text-center mb-4" style="flex: 1; justify-content: center">
            Your Trips
        </h1>
    </div>

    <div class="card-body d-flex justify-content-between align-items-center">
        <NavigationButton text="+" path="/home/trips/create-trip" />
        <div>
            <SearchForm />
        </div>
    </div>
</div>

<style>
    .cursor-pointer {
        cursor: pointer;
    }

    .task-item-box {
        background-color: transparent;
        transition: background-color 0.2s ease;
    }

    .task-item-box:hover {
        background-color: #f8f8f8;
    }
</style>
