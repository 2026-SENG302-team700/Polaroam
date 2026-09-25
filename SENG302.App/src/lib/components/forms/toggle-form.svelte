<script lang="ts">
    let {
        id = $bindable(""),
        checked = $bindable(),
        onchange
    } = $props();

    /**
     * For handling enter key on input toggle
     * @param e key being pressed
     */
    function handleKeydown(e: KeyboardEvent) {
        if (e.key === "Enter") {
            checked = !checked;
            onchange();
        }
    }
</script>

<div class="form-check form-switch">
    <input class ="form-check-input"
           type="checkbox"
           role="switch"
           id={id}
           bind:checked
           onkeydown={(e) => handleKeydown(e)}
           {onchange}
    />
    <label class="form-check-label">
        {checked? "Enabled" : "Disabled"}
    </label>
</div>

<style>
    .form-check-label {
        min-width: 4rem;
        text-align: center;
    }
</style>