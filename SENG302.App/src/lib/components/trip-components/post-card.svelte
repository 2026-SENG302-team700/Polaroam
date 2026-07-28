<script>
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { formatDate } from "$lib/datepicker/formatDate";

    let { post } = $props();

    function goToPost() {
        goto(resolve(`/home/trips/${post.tripId}/posts/${post.postId}`));
    }
</script>

<div
    class="card mb-3"
    tabindex="0"
    role="button"
    onclick={goToPost}
    onkeydown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
            goToPost();
        }
    }}
>
    <div class="card-body">
        <h5 class="card-title fw-bold">{post.title}</h5>
        <h6 class="card-subtitle mb-2 text-muted">{formatDate(post.date)}</h6>
        <p class="card-text">{post.content}</p>
    </div>
</div>
