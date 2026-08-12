<script lang="ts">
    import SectionTitle from "$lib/components/content/section-title.svelte";
    import PostCard from "$lib/components/trip-components/post-card.svelte";

    let {
        posts,
        onPostHover,
        onPostLeave,
    }: {
        posts: any[];
        onPostHover: (postId: number) => void;
        onPostLeave: () => void;
    } = $props();
</script>

<SectionTitle title="Posts" />
{#each posts as post (post.postId)}
    <PostCard
        {post}
        onHover={() => onPostHover(post.postId)}
        onLeave={onPostLeave}
    />
{/each}
