<script lang="ts">
    import Input from "$lib/components/ui/input/input.svelte";
    import Button from "../ui/button/button.svelte";
    //import search from "bootstrap-icons/icons";
</script>

<div class="grid grid-cols-5 gap-2">
    <Input class="col-span-4" type="search" placeholder="search your trips" />
    <Button size="icon" variant="outline">
        <img src="" alt="search icon" />
    </Button>
</div>
