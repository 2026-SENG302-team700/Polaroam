<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";

    import { Button } from "$lib/components/ui/button";
    import * as Card from "$lib/components/ui/card";
</script>

<svelte:head>
    <title>Welcome | Polaroam</title>
</svelte:head>

<main class="flex min-h-screen items-center justify-center bg-[#f7f3e8] p-6">
    <Card.Root class="w-full max-w-md">
        <Card.Header class="text-center">
            <img
                src={resolve("/polaroam-logo.png")}
                alt="Polaroam"
                class="mx-auto -mb-18 h-60 w-60 object-contain"
            />

            <Card.Title class="text-3xl font-black tracking-tight">
                Polaroam
            </Card.Title>

            <Card.Description class="mt-2 text-base text-black">
                Sign in to continue or create a new account.
            </Card.Description>
        </Card.Header>

        <Card.Content class="flex flex-col gap-4">
            <Button class="w-full" onclick={() => goto(resolve("/register"))}>
                Register
            </Button>

            <Button
                variant="outline"
                class="w-full"
                onclick={() => goto(resolve("/login"))}
            >
                Log in
            </Button>
        </Card.Content>
    </Card.Root>
</main>
