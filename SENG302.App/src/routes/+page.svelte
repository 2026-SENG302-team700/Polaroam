<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";

</script>

<svelte:head>
    <title>Welcome | Polaroam</title>
</svelte:head>

<main class="flex min-h-screen items-center justify-center bg-[#f7f3e8] p-6">
<button class="w-full" onclick={() => goto(resolve("/register"))}>
                Register
</button>

            <button
                
                class="w-full"
                onclick={() => goto(resolve("/login"))}
            >
                Log in
</button>
    
</main>
