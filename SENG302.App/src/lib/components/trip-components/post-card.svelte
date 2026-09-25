<script>
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { formatDate } from "$lib/datepicker/formatDate";

    import ImageCarousel from "../images/image-carousel.svelte";

    let {
        post,
        isSelected = false,
        onHover = () => {},
        onLeave = () => {},
    } = $props();

    function goToPost() {
        goto(resolve(`/home/trips/${post.tripId}/posts/${post.postId}`));
    }
</script>

<article
    class="post-card"
    class:highlighted={isSelected}
    data-testid={`post-card-${post.postId}`}
    tabindex="0"
    role="button"
    onclick={goToPost}
    onmouseenter={onHover}
    onmouseleave={onLeave}
    onfocus={onHover}
    onblur={onLeave}
    onkeydown={(event) => {
        if (event.key === "Enter" || event.key === " ") goToPost();
    }}
>
    <div class="post-heading">
        <span class="post-date">
            <i class="bi bi-calendar3"></i>
            {formatDate(post.date)}
        </span>
    </div>

    <div class="post-details">
        <h3>{post.title}</h3>

        <div>
            <i class="bi bi-person-fill"></i>
            <p class="location">{post.creatorDisplayName}</p>
        </div>
        {#if post.locationName}
            <p class="location">
                <i class="bi bi-geo-alt-fill"></i>{post.locationName}
            </p>
        {/if}

        <p class="content" class:empty={!post.content}>
            {post.content || "No description"}
        </p>
    </div>

    {#if post.photoIds?.length > 0}
        <div class="post-images">
            <ImageCarousel {post} />
        </div>
    {/if}
    <span class="view-post">View post <i class="bi bi-arrow-right"></i></span>
</article>

<style>
    .post-card {
        display: flex;
        min-width: 0;
        height: 100%;
        flex-direction: column;
        overflow: hidden;
        border: 2px solid #686868;
        border-radius: 0.55rem;
        background: #fff8e8;
        box-shadow: 5px 5px 0 #686868;
        cursor: pointer;
        transition:
            transform 120ms ease,
            box-shadow 120ms ease;
    }

    .post-card:hover,
    .post-card:focus-visible {
        transform: translate(-2px, -2px);
        box-shadow: 7px 7px 0 #686868;
        outline: none;
    }

    .post-heading {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        padding: 0.7rem 0.85rem;
        border-bottom: 2px solid #686868;
        background: #f4ead8;
    }

    .post-date,
    .location,
    .view-post {
        display: inline-flex;
        align-items: center;
    }

    .post-date {
        gap: 0.4rem;
        color: #686868;
        font-size: 0.82rem;
        font-weight: 600;
    }

    .post-details {
        padding: 0.9rem 1rem 0.7rem;
    }

    .post-details h3 {
        margin: 0 0 0.45rem;
        overflow: hidden;
        color: #3f3f3f;
        font-size: 1.15rem;
        font-weight: 800;
        line-height: 1.2;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .location {
        gap: 0.35rem;
        margin: 0;
        color: #686868;
        font-size: 0.85rem;
        font-weight: 600;
    }

    .content {
        display: -webkit-box;
        min-height: 2.55rem;
        margin: 0.65rem 0 0;
        overflow: hidden;
        color: #5f5f5f;
        font-size: 0.9rem;
        line-height: 1.4;
        -webkit-box-orient: vertical;
        -webkit-line-clamp: 2;
    }

    .content.empty {
        color: #8a8a8a;
        font-style: italic;
    }

    .post-images {
        overflow: hidden;
        border-top: 1px solid #c9c0b1;
    }

    .view-post {
        align-self: flex-end;
        gap: 0.35rem;
        margin-top: auto;
        padding: 0.75rem 1rem 0.9rem;
        color: #3f3f3f;
        font-size: 0.85rem;
        font-weight: 800;
    }

    .post-card.highlighted {
        border-color: #117a8b;
        background-color: #b9d5df;
        box-shadow: 5px 5px 0 #117a8b;
    }

    @media (max-width: 420px) {
        .post-heading {
            align-items: flex-start;
            flex-direction: column;
            gap: 0.45rem;
        }
    }
</style>
