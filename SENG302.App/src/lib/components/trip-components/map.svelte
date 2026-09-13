<script lang="ts">
    import { MapLibre, Marker, Popup } from "svelte-maplibre";
    import { resolve } from "$app/paths";
    import { formatDate } from "$lib/datepicker/formatDate";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { onMount } from "svelte";

    let {
        locations,
        posts,
        hoveredPostId = null,
        selectedPost = $bindable(null),
    } = $props();

    let map = $state();
    let mapLoaded = $state(false);
    let mapVisible = $state(false);
    let mapToggleError = $state("");
    let mapPreferenceFetched = $state(false);
    const mapStyle = "https://tiles.openfreemap.org/styles/liberty";

    /**
     * Get the posts for a given location
     * @param postId - the id of the post to get
     */
    function getPosts(postId: number) {
        return posts.find((post: { postId: number }) => post.postId === postId);
    }

    function postExcerpt(content: string) {
        if (!content) return "No description";
        return content.length > 100 ? `${content.slice(0, 100)}…` : content;
    }

    onMount(() => {
        fetchMapVisibility();
    });

    /**
     * gets the post from a pin + centres map on pin
     */
    function pinClick(location) {
        selectedPost = location.postId;
        map?.flyTo({
            center: [parseFloat(location.lon), parseFloat(location.lat)],
            zoom: 8,
            duration: 1000,
        });
    }

    /**
     * Recenter the map to fit all the locations
     */
    function recenterMap() {
        if (!map || locations.length === 0) return;

        const coords = locations
            .map((loc) => [parseFloat(loc.lon), parseFloat(loc.lat)])
            .filter(([lng, lat]) => !isNaN(lng) && !isNaN(lat));

        // Don't zoom if there are no locations
        if (coords.length === 0) return;

        // Center on a single location with zoom level 12
        if (coords.length === 1) {
            map.flyTo({ center: coords[0], zoom: 12, duration: 1000 });
            return;
        }

        let minLng = coords[0][0],
            maxLng = coords[0][0];
        let minLat = coords[0][1],
            maxLat = coords[0][1];
        for (const [lng, lat] of coords) {
            minLng = Math.min(minLng, lng);
            maxLng = Math.max(maxLng, lng);
            minLat = Math.min(minLat, lat);
            maxLat = Math.max(maxLat, lat);
        }
        map.fitBounds(
            [
                [minLng, minLat],
                [maxLng, maxLat],
            ],
            { padding: 50, maxZoom: 15, duration: 1000 },
        );
    }

    $effect(() => {
        if (!mapLoaded || !map) return;

        locations
            .map((location) => `${location.lon},${location.lat}`)
            .join("|");

        const timeout = setTimeout(recenterMap, 0);
        return () => clearTimeout(timeout);
    });

    $effect(() => {
        const postId = hoveredPostId;
        if (!map || postId === null) return;

        const timeout = setTimeout(() => {
            const location = locations.find((item) => item.postId === postId);

            if (!location) return;

            const longitude = parseFloat(location.lon);
            const latitude = parseFloat(location.lat);

            if (isNaN(longitude) || isNaN(latitude)) return;

            map.stop();

            map.easeTo({
                center: [longitude, latitude],
                zoom: 8,
                duration: 1500,
            });
        }, 200);
    });

    /**
     * Toggle the map visibility, sending a request to the backend to save the user's preference
     */
    async function toggleMapVisibility() {
        mapVisible = !mapVisible;

        try {
            const response = await fetchWithCsrf(
                resolve(`/api/user/preference/mapvisible` as any),
                {
                    method: "PUT",
                    credentials: "include",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({ mapVisible }),
                },
            );

            if (!response.ok) {
                mapToggleError = "Failed to toggle map";
            }
        } catch (err) {
            mapToggleError = "Failed to toggle map: " + (err as Error).message;
        }
    }

    /**
     * Fetch the map visibility and set mapVisible to that
     */
    async function fetchMapVisibility() {
        try {
            await fetchWithCsrf(
                resolve(`/api/user/preference/mapvisible` as any),
                {
                    method: "GET",
                    credentials: "include",
                },
            ).then(async (response) => {
                mapPreferenceFetched = true;

                const data = await response.json();

                if (!response.ok) {
                    mapToggleError =
                        data.message ||
                        "Failed to get map visiblitiy preference";
                    return false;
                }

                mapVisible = data.mapVisible;
            });
        } catch (err) {
            mapToggleError =
                "Failed to get map visiblitiy preference: " +
                (err as Error).message;
        }
    }
</script>

<div class="container" data-testid="trip-map">
    {#if posts.length === 0}
        <div class="empty-trip-page-element" data-testid="empty-trip-map">
            <i class="bi bi-map" aria-hidden="true"></i>
            <p>No posts yet. Create a post to view the map</p>
        </div>
    {:else if mapVisible}
        <div
            class="position-relative rounded overflow-hidden"
            style="height: 480px;"
        >
            <MapLibre
                style={mapStyle}
                bind:map
                class="map"
                id="trip-map"
                onload={() => {
                    mapLoaded = true;
                    recenterMap();
                }}
            >
                {#each locations as location, index (location.postId)}
                    {@const post = getPosts(location.postId)}
                    <Marker
                        lngLat={[
                            parseFloat(location.lon),
                            parseFloat(location.lat),
                        ]}
                        anchor="bottom"
                    >
                        <button
                            data-testid={`post-marker-${location.postId}`}
                            class="post-marker"
                            class:if-highlighted={hoveredPostId ===
                                location.postId}
                            type="button"
                            aria-label={`View ${post?.title ?? "post"} at ${location.name}`}
                        >
                            <i class="bi bi-geo-fill"></i>
                            <span>{index + 1}</span>
                        </button>

                        <Popup
                            offset={[0, -20]}
                            open={selectedPost === location.postId}
                            onopen={() => pinClick(location)}
                        >
                            <div class="post-popup">
                                {#if post}
                                    <h3>{post.title}</h3>

                                    {#if post.date}
                                        <p class="post-date">
                                            <i class="bi bi-calendar3"></i>
                                            {formatDate(post.date)}
                                        </p>
                                    {/if}

                                    <p class="post-location">
                                        <i class="bi bi-geo-alt-fill"></i>
                                        {location.name}
                                    </p>

                                    <p class="post-content">
                                        {postExcerpt(post.content)}
                                    </p>

                                    <a
                                        class="post-link"
                                        href={resolve(
                                            `/home/trips/${post.tripId}/posts/${post.postId}` as any,
                                        )}
                                    >
                                        View post
                                        <i class="bi bi-arrow-right"></i>
                                    </a>
                                {:else}
                                    <strong>{location.displayName}</strong>
                                {/if}
                            </div>
                        </Popup>
                    </Marker>
                {/each}
            </MapLibre>

            <div class="position-absolute top-0 end-0 m-2">
                <button
                    class="btn btn-secondary"
                    style="width: 34px; height: 34px; padding: 0;"
                    onclick={recenterMap}
                    type="button"
                    aria-label="Recenter map"
                >
                    <i class="bi bi-arrows-angle-expand"></i>
                </button>

                <button
                    class="btn btn-secondary m-1"
                    style="width: 34px; height: 34px; padding: 0;"
                    onclick={toggleMapVisibility}
                    type="button"
                    aria-label="Recenter map"
                >
                    <i class="bi bi-map"></i>
                </button>
            </div>
        </div>
    {:else}
        <div class="menu-bar p-2 m-0 d-flex flex-row-reverse">
            <button
                class="btn btn-secondary"
                style="width: 34px; height: 34px; padding: 0;"
                onclick={toggleMapVisibility}
                type="button"
                aria-label="Recenter map"
            >
                <i class="bi bi-map"></i>
            </button>

            <i class="m-1">Map hidden</i>
        </div>
    {/if}
</div>

<style>
    :global(.map) {
        width: 100%;
        height: 100%;
    }

    /**
    AI attribution:
    Claude was used to help with the CSS design of the map markers, including the marker shape, rotation, shadows
    and hover styling. The styling was reviewed and then integrated into the project
    */
    .post-marker {
        position: relative;
        display: grid;
        width: 42px;
        height: 42px;
        padding: 0;
        place-items: center;
        border: 2px solid #686868;
        border-radius: 50% 50% 50% 0;
        background: #83beda;
        box-shadow: 3px 3px 0 #686868;
        color: #686868;
        cursor: pointer;
        transform: rotate(-45deg);
        transition:
            transform 120ms ease,
            box-shadow 120ms ease;
    }

    .post-marker i {
        font-size: 1.1rem;
        transform: rotate(45deg);
    }

    .post-marker span {
        position: absolute;
        top: -7px;
        right: -7px;
        display: grid;
        width: 19px;
        height: 19px;
        place-items: center;
        border: 1px solid #686868;
        border-radius: 50%;
        background: #fff8e8;
        font-size: 0.7rem;
        font-weight: 700;
        transform: rotate(45deg);
    }

    .post-marker.if-highlighted {
        z-index: 3;
        background: #ef6a67;
        box-shadow: 5px 5px 0 #686868;
        transform: translateY(-4px) scale(1.12);
    }

    .post-marker:hover {
        box-shadow: 5px 5px 0 #686868;
        transform: rotate(-45deg) translateY(-2px);
    }

    .post-popup {
        width: 220px;
        color: #686868;
    }

    .post-popup h3 {
        margin: 0.3rem 0 0.6rem;
        font-size: 1.1rem;
    }

    .post-date,
    .post-location {
        display: flex;
        gap: 0.4rem;
        margin: 0.25rem 0;
        font-size: 0.85rem;
    }

    .post-content {
        margin: 0.7rem 0;
        font-size: 0.85rem;
    }

    .post-link {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        color: #436f83;
        font-weight: 700;
        text-decoration: none;
    }
</style>
