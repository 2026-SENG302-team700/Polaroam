<script lang="ts">
    import { resolve } from "$app/paths";
    import { goto } from "$app/navigation";
    import { onMount } from "svelte";
    import { formatDate } from "$lib/datepicker/formatDate";
    import { fetchWithCsrf } from "$lib/csrf";
    import { user } from "$lib/stores/user";

    let { tripObject }: { tripObject: Trip } = $props();
    let coverPhotoId: number | null = $state(null);

    onMount(() => {
        loadTrip();
    });

    async function loadTrip() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/album/${tripObject.id}` as any),
                { credentials: "include" },
            );
            if (!response.ok) return;

            const images: { photoId: number }[] = await response.json();
            coverPhotoId = images[0]?.photoId ?? null;
        } catch {
            coverPhotoId = null;
        }
    }

    function openTrip() {
        goto(resolve(`/home/trips/${tripObject.id}`));
    }
</script>

<article
    tabindex="0"
    role="button"
    class="trip-card"
    onclick={openTrip}
    onkeydown={(event) => {
        if (event.key === "Enter" || event.key === " ") openTrip();
    }}
>
    <div class="cover">
        {#if coverPhotoId !== null}
            <img
                src={resolve(
                    `/api/album/${tripObject.id}/images/${coverPhotoId}` as any,
                )}
                alt={`Cover for ${tripObject.title}`}
                onerror={() => (coverPhotoId = null)}
            />
        {:else}
            <div class="cover-placeholder" aria-hidden="true">
                <i class="bi bi-images"></i>
            </div>
        {/if}
    </div>

    <div class="trip-details">
        <h2>{tripObject.title}</h2>
        <p>
            Owned by {tripObject.userId == $user.id
                ? "You"
                : tripObject.ownerName}
        </p>
        <p class="dates">
            <i class="bi bi-calendar3"></i>
            <span>
                {formatDate(tripObject.startDate)} – {formatDate(
                    tripObject.endDate,
                )}
            </span>
        </p>
        {#if tripObject.description}
            <p class="description">{tripObject.description}</p>
        {/if}
        <span class="view-trip"
            >View trip <i class="bi bi-arrow-right"></i></span
        >
    </div>
</article>

<style>
    .trip-card {
        width: 100%;
        overflow: hidden;
        border: 2px solid black;
        border-radius: 0.55rem;
        background: #fffdf7;
        box-shadow: 6px 6px 0 black;
        cursor: pointer;
        transition:
            transform 120ms ease,
            box-shadow 120ms ease;
    }

    .trip-card:hover,
    .trip-card:focus-visible {
        transform: translate(-2px, -2px);
        box-shadow: 8px 8px 0 black;
        outline: none;
    }

    .cover {
        position: relative;
        aspect-ratio: 16 / 10;
        overflow: hidden;
        border-bottom: 2px solid black;
        background: #efe7d6;
    }

    .cover img,
    .cover-placeholder {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }

    .cover-placeholder {
        display: grid;
        place-items: center;
        color: black;
        background: linear-gradient(
                135deg,
                transparent 75%,
                rgba(104, 104, 104, 0.08) 75%
            ),
            #efe7d6;
        background-size: 18px 18px;
    }

    .cover-placeholder i {
        font-size: 3.25rem;
    }

    .trip-details {
        display: flex;
        min-height: 10rem;
        flex-direction: column;
        padding: 1rem 1.1rem 1.1rem;
        background-color: #fff8e8;
    }

    .trip-details h2 {
        margin: 0 0 0.55rem;
        overflow: hidden;
        color: #3f3f3f;
        font-size: 1.3rem;
        font-weight: 800;
        line-height: 1.2;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .dates {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin: 0;
        color: black;
        font-size: 0.9rem;
        font-weight: 600;
    }

    .description {
        display: -webkit-box;
        margin: 0.7rem 0 0;
        overflow: hidden;
        color: black;
        font-size: 0.9rem;
        line-height: 1.35;
        -webkit-box-orient: vertical;
        -webkit-line-clamp: 2;
    }

    .view-trip {
        display: inline-flex;
        align-items: center;
        align-self: flex-end;
        gap: 0.35rem;
        margin-top: auto;
        padding-top: 0.8rem;
        color: black;
        font-size: 0.85rem;
        font-weight: 800;
    }
</style>
