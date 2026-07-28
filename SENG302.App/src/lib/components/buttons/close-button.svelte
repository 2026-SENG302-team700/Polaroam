<script lang="ts">
    let { close = $bindable(false), class: className = "" }:
        { close: boolean, class?:string }
        = $props();

        function clickClose() {
            close =!close;
            console.log(close);
        }
</script>

<button class="btn btn-secondary bi-x {className}" title="Close" onclick={clickClose}></button>

<style>
     .btn{
        top: 5px;
        right: 5px;
        }
</style>