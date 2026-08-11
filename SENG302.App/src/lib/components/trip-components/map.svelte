<script lang="ts">
    import { MapLibre, Marker, Popup } from 'svelte-maplibre';
    import {resolve} from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf.js";
    import { json } from 'node:stream/consumers';
    import { onMount } from 'svelte';
    
    let { locations } = $props();

    let map = $state();
    let mapVisible = $state(false);
    let mapToggleError = $state("");
    const mapStyle = 'https://tiles.openfreemap.org/styles/liberty';

    onMount(async () => {
        mapVisible = await getMapVisibility();
    })

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
        
        // Get min/max coordinates to fit all locations/
        let minLng = coords[0][0], maxLng = coords[0][0];
        let minLat = coords[0][1], maxLat = coords[0][1];
        for (const [lng, lat] of coords) {
            minLng = Math.min(minLng, lng);
            maxLng = Math.max(maxLng, lng);
            minLat = Math.min(minLat, lat);
            maxLat = Math.max(maxLat, lat);
        }
        
        // Fit the map to the locations with max zoom to prevent it being to close when locations are
        // very close together and time to recenter of 1 second
        map.fitBounds(
            [[minLng, minLat], [maxLng, maxLat]],
            { padding: 50, maxZoom: 15, duration: 1000 }
        );
    }
    

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
                    body: JSON.stringify({mapVisible})
                },
            );
            const data = await response.json();

            if (!response.ok) {
                mapToggleError = data.message || "Failed to toggle map";
                return;
            }

        } catch (err) {
            mapToggleError = "Failed to toggle map: " + (err as Error).message;
        }
    }

    async function getMapVisibility() {
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/user/preference/mapvisible` as any),
                {
                    method: "PUT",
                    credentials: "include",
                    body: JSON.stringify({mapVisible})
                },
            );
            const data = await response.json();

            if (!response.ok) {
                mapToggleError = data.message || "Failed to toggle map";
                return;
            }

        } catch (err) {
            mapToggleError = "Failed to toggle map: " + (err as Error).message;
        }
    }

    $effect(() => {
        recenterMap();
    });
    
</script>

<div class = "container">
    <div class="position-relative rounded overflow-hidden" style="height: 480px;">
        <MapLibre style={mapStyle} bind:map class="map">
            {#each locations as location (location.osmId)}
                <Marker lngLat={[parseFloat(location.lon), parseFloat(location.lat)]}>
                    <i class="bi bi-geo-fill text-danger fs-2"></i>
                    <Popup offset={[0, -10]}>
                        <div class="small" style="max-width: 220px;">
                            <strong>{location.displayName}</strong>
                        </div>
                    </Popup>
                </Marker>
            {/each}
        </MapLibre>

        <div class="position-absolute top-0 end-0 m-2">

            <button
                class="btn btn-secondary"
                style="width: 34px; height: 34px; padding: 0;"
                onclick={toggleMapVisibility}
                type="button"
                aria-label="Recenter map"
            >
                <i class="bi bi-map"></i>
            </button>

            <button
                    class="btn btn-secondary m-1"
                    style="width: 34px; height: 34px; padding: 0;"
                    onclick={recenterMap}
                    type="button"
                    aria-label="Recenter map"
            >
                <i class="bi bi-arrows-angle-expand"></i>
            </button>
        
        </div>
    </div>
</div>


<style>
    :global(.map) {
        width: 100%;
        height: 100%;
    }

    .marker-pin {
        width: 24px;
        height: 24px;
        cursor: pointer;
    }
</style>