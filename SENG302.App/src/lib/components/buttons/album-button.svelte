<script>
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    let {tripid} = $props();
</script>


<button class="btn btn-secondary bi-images" onclick={() => goto(resolve(`/home/trips/${tripid}/album`))}> Album</button>