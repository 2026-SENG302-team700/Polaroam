<script lang="ts">
    import SectionTitle from "$lib/components/content/section-title.svelte";
    import PostCard from "$lib/components/trip-components/post-card.svelte";
    import AddButton from "../buttons/add-button.svelte";

    let {
        posts,
        tripId,
        selectedPost = null,
        onPostHover,
        onPostLeave,
    }: {
        posts: any[];
        tripId: number;
        selectedPost: number | null;
        onPostHover: (postId: number) => void;
        onPostLeave: () => void;
    } = $props();

    let displayedPosts = $derived(
        selectedPost
            ? [...posts].sort((a, b) =>
                  a.postId === selectedPost
                      ? -1
                      : b.postId === selectedPost
                        ? 1
                        : 0,
              )
            : posts,
    );
</script>

<div class="post-section wrapper">
    <div class="post-section-flex">
        <SectionTitle title="Posts" />
        <div class="button-container">
            <AddButton path={`/home/trips/${tripId}/posts/create-post`} />
        </div>
    </div>

    <div class="post-grid">
        {#each displayedPosts as post (post.postId)}
            <PostCard
                {post}
                isSelected={post.postId === selectedPost}
                onHover={() => onPostHover(post.postId)}
                onLeave={onPostLeave}
            />
        {/each}
    </div>
</div>

<style>
    .post-section-flex {
        margin-top: 10px;
        margin-bottom: 10px;
        display: flex;
        align-items: center;
        gap: 15px;
        width: 100%;
    }

    .post-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 1.25rem;
        align-items: stretch;
    }

    @media (max-width: 767px) {
        .post-grid {
            grid-template-columns: 1fr;
        }
    }
</style>
