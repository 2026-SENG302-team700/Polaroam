<script lang="ts">
    import SectionTitle from "$lib/components/content/section-title.svelte";
    import PostCard from "$lib/components/trip-components/post-card.svelte";
    import AddButton from "../buttons/add-button.svelte";

    let { posts, tripId } = $props();
</script>

<div class="post-section wrapper">
    <div class="post-section-flex">
        <SectionTitle title="Posts" />
        <div class="button-container">
            <AddButton path="/home/trips/{tripId}/posts/create-post" />
        </div>
    </div>
        
    {#each posts as post (post.postId)}
        <PostCard {post} />
    {/each}
</div>
<style>
    .post-section-flex {
        margin-top: 10px;
        margin-bottom: 10px;
        display: flex;
        justify-content: space-between;
        width: 100%;
    }


</style>

