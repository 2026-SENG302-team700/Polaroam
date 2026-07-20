<script lang="ts">
    import NavigationButton from "$lib/components/buttons/navigation-button.svelte";
    import { onMount } from "svelte";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { resolve } from "$app/paths";

    let loading = $state(false);
    let error = $state("");
    let { params } = $props();

    let tripObject = $state({
        Id: 0,
        Title: "No Title",
        Description: "No Description",
        StartDate: new Date(),
        EndDate: new Date(),
        UserId: 0,
    });

    let errors = $state({
        name: "",
        description: "",
        dueDate: "",
        taskStatus: "",
    });

    onMount(() => {
        fetchTaskItem();
    });

    /**
     * Fetches the task item in order
     * to display the information
     */
    async function fetchTaskItem() {
        try {
            loading = true;
            const response = await fetchWithCsrf(
                resolve(`/api/trip/${params.slug}` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            );
            const data = await response.json();
            if (!response.ok) {
                error = data.message || "Failed to fetch task lists.";
                return;
            }
            tripObject = data;
        } catch (err) {
            error = "Failed to fetch task lists: " + (err as Error).message;
        } finally {
            loading = false;
        }
    }
</script>

<div class="container">
    <NavigationButton path="/home/trips" text="Back" />
    <h1>{tripObject.Title}</h1>
    <p>
        {tripObject.StartDate} - {tripObject.EndDate}
    </p>
    <div class="border-t-2 border-black"></div>
    <span>{tripObject.Description}</span>
</div>
