<script>

    import { formatDate } from "$lib/datepicker/formatDate";
    import ImageDisplay from "../images/image-display.svelte";

    let { 
        post, 
        userName, 
        photos, 
        photoIds = [] 
    } = $props();

    
</script>

<div class="card mb-2 post-wrapper">

    <div class="card-header">
        <h6 class="card-subtitle mb-2 text-muted user-name">{userName}</h6>
        <div class="header-content-wrapper"> 
            <div class="header-content-item">
                <div class="bi-calendar-event"></div>
                <h6 class="card-subtitle mb-2 text-muted">{formatDate(post.date)}</h6>
            </div>
            <div class="header-content-item">
                <div class="bi-geo-alt-fill"></div>
                <h6 class="card-subtitle mb-2 text-muted">{post.locationName}</h6>
            </div>
        </div>
    </div>
    <div class="card-body">
        <h5 class="card-title fw-bold">{post.title}</h5>
        <p class="card-text">{post.content}</p>
    </div>
    <div class="card-footer text-muted images">
        {#if photos.length > 0}
                <ImageDisplay images={photos} {photoIds} />
        {/if}
  </div>
</div>

<style>
.card-header {
    display: flex;
    position: relative;
    align-items: center;
}
.header-content-item {
    display: flex;
    gap: 4px;
    align-items:baseline;
    justify-content: end;
}

.header-content-wrapper {
    width: 100%;
}

.user-name {
    max-width: 40%;
    min-width: 20%;
}

.post-wrapper {
    margin-top: 10px;
}

.card-body {
    max-height: 500px;
    overflow: scroll;
}

.images {
    max-height: 800px;
    overflow: scroll;
}

</style>