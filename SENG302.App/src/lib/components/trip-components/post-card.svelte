<script>
    let { title, content, date } = $props();
</script>

<div class="card mb-3">
    <div class="card-body">
        <h5 class="card-title fw-bold">{title}</h5>
        <h6 class="card-subtitle mb-2 text-muted">{date}</h6>
        <p class="card-text">{content}</p>
    </div>
</div>