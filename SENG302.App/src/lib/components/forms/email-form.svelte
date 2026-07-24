<script lang="ts">
    let { email = $bindable(""), error = "", loading = false } = $props();
</script>

<div class="mb-3">
    <input 
            id="email"
        type="email"
        class="form-control"
        class:is-invalid={error}
        autocomplete="email"
        placeholder={"Email *"}
        bind:value={email}
        disabled={loading}
    />
    {#if error}
        <div class="invalid-feedback">{error}</div>
    {/if}
</div>
