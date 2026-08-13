<script lang="ts">
    import type { Snippet } from "svelte";

    let {
        heading,
        children,
    }: {
        heading: string;
        children: Snippet;
    } = $props();
</script>

<div class="container post-form-page">
    <div class="post-form">
        <div class="post-form-header">
            <span class="header-icon bi-journal-richtext"></span>
            <h1>{heading}</h1>
        </div>

        <div class="post-form-body">
            {@render children()}
        </div>
    </div>
</div>

<style>
    .post-form-page {
        max-width: 700px;
        padding-top: 2rem;
        padding-bottom: 3rem;
    }

    .post-form {
        position: relative;
        border: 2px solid #686868;
        border-radius: 0.35rem;
        background: #ffffff;
        box-shadow: 7px 7px 0 #686868;
    }

    /* tape at the top of the form */
    .post-form::before {
        position: absolute;
        z-index: 1;
        top: -14px;
        left: 50%;
        width: 110px;
        height: 28px;
        background: rgb(255 220 140 / 80%);
        content: "";
        transform: translateX(-50%) rotate(-2deg);
    }

    .post-form-header {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 1.25rem 1.5rem;
        border-bottom: 2px solid #686868;
        background: #83beda;
    }

    .post-form-header h1 {
        margin: 0;
        color: #686868;
        font-size: 1.75rem;
        font-weight: 700;
        letter-spacing: 0.02em;
    }

    .header-icon {
        color: #686868;
        font-size: 1.5rem;
    }

    .post-form-body {
        padding: 1.75rem;
    }

    @media (max-width: 650px) {
        .post-form-page {
            padding-top: 1.5rem;
        }

        .post-form {
            box-shadow: 4px 4px 0 #686868;
        }

        .post-form-header,
        .post-form-body {
            padding-right: 1rem;
            padding-left: 1rem;
        }
    }
</style>
