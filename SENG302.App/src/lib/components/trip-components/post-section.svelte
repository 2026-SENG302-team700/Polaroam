<script lang="ts">
    import SectionTitle from "$lib/components/content/section-title.svelte";
    import PostCard from "$lib/components/trip-components/post-card.svelte";

    let { posts } = $props();
</script>

<SectionTitle title="Posts" />
{#each posts as post (post.postId)}
    <PostCard {post} />
{/each}
