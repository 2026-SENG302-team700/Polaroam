<script lang="ts">
    import ErrorDisplay from "$lib/components/forms/multiple-error-display.svelte";
    import mimeMagicNumbers from "../../../../../SENG302.Shared/mimeTypes.json";

    const MAX_IMAGE_SIZE = 10000000;
    const mimeTypes = [
        "image/webp",
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/svg+xml",
    ];

    let pfpInput: HTMLInputElement;
    let files: FileList | undefined = $state();
    let clientErrors: string[] = $state([]);
    let {
        filesList = $bindable<File[]>([]),
        errors = [],
        newUploadsCount = $bindable(0),
    }: { filesList: File[] | null | undefined; errors: string[]; newUploadsCount?: number } = $props();

    let displayedErrors = $derived([...errors, ...clientErrors]);

    /**
     * Check the start of the file to get the mime type
     * and return it if it is a valid type
     * @param file the image file
     */
    async function getRealValidMime(file: File) {
        const magicNumber = new Uint8Array(await file.slice(0, 12).arrayBuffer());
        const dict = Object.entries(mimeMagicNumbers);

        for (let mimeI = 0; mimeI < dict.length; mimeI++) {
            const mimeMagicNumber = dict[mimeI][0];
            let valid = true;

            for (let i = 0; i < magicNumber.length; i++) {
                const fileByteStr = magicNumber[i].toString(16);
                const mimeByteStr = mimeMagicNumber.slice(i * 2, i * 2 + 2);

                if (mimeByteStr === "" || fileByteStr === "") break;

                if (!(mimeByteStr === "??" || fileByteStr === mimeByteStr)) {
                    valid = false;
                    break;
                }
            }

            if (valid) {
                return dict[mimeI][1];
            }
        }
    }

    /**
     * Validates a file's size and mime type against the same rules the backend enforces
     * @param file the image file
     * @returns every error message the file fails, prefixed with the file name (empty if valid)
     */
    async function validateImage(file: File): Promise<string[]> {
        const fileErrors: string[] = [];

        if (file.size > MAX_IMAGE_SIZE) {
            fileErrors.push(`${file.name}: Image too large, maximum file size is 10MB`);
        }

        const realMimeType = await getRealValidMime(file);
        if (!mimeTypes.includes(file.type) || realMimeType === undefined || file.type !== realMimeType) {
            fileErrors.push(`${file.name}: Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp`);
        }

        return fileErrors;
    }

    /**
     * Validates and adds the selected files to the given file list, rejecting any
     * file that fails validation with every error it fails, not just the first one
     */
    async function appendImages() {
        if (!files) return;

        clientErrors = [];
        for (const file of files) {
            const fileErrors = await validateImage(file);
            if (fileErrors.length > 0) {
                clientErrors.push(...fileErrors);
            } else {
                newUploadsCount += 1
                filesList.push(file);
            }
        }
    }
    
</script>
<div class="d-flex flex-column mb-2">
    <button
            type="button"
            class="btn btn-sm btn-primary"
            class:is-invalid={displayedErrors.length > 0}
            onclick={() => {
          pfpInput.value = "";
          pfpInput.click();
        }}
    >
        <i class="bi-plus"></i>
        <i class="bi-images"></i>
    </button>
    <input
            accept="image/webp, image/jpeg, image/png, image/gif, image/svg+xml"
            type="file"
            bind:files
            bind:this={pfpInput}
            class="d-none"
            id="image-picker"
            multiple
            onchange={appendImages}
    />
    <ErrorDisplay errors={displayedErrors}/>
</div>