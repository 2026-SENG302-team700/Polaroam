<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
</script>

<div class="container text-center py-5">
    
    <h2>Content not found</h2>
    <p>Ensure you have permission to access the requested content</p>

    <button
            type="button"
            class="btn btn-primary"
            onclick={() => goto(resolve("/home"))}
    >
        Return home
    </button>
</div>
