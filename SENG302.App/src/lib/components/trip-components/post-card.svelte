<script>
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { formatDate } from "$lib/datepicker/formatDate";

    import ImageCarousel from "../images/image-carousel.svelte";

    let { post, onHover = () => {}, onLeave = () => {} } = $props();

    function goToPost() {
        goto(resolve(`/home/trips/${post.tripId}/posts/${post.postId}`));
    }
</script>

<div class="card mb-3">
    <div class="card-body" onmouseenter={onHover} onmouseleave={onLeave}>
        <div
            class="card-body"
            tabindex="0"
            role="button"
            onclick={goToPost}
            onfocus={() => onHover()}
            onblur={() => onLeave()}
            onkeydown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                    goToPost();
                }
            }}
        >
            <h5 class="card-title fw-bold">{post.title}</h5>
            <h6 class="card-subtitle mb-2 text-muted">
                {formatDate(post.date)}
            </h6>
            <p class="card-text">{post.content}</p>
        </div>
    </div>
    {#if post.photoIds.length > 0}
        <div class="card-footer">
            <ImageCarousel {post} />
        </div>
    {/if}
</div>
