<script lang="ts">
    import mimeMagicNumbers from "../../../../../SENG302.Shared/mimeTypes.json";

    const MAX_IMAGE_SIZE = 10000000;
    const MAX_FILENAME_LENGTH = 32;

    const mimeTypes = [
        "image/webp",
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/svg+xml",
    ];

    let pfpInput: HTMLInputElement;
    let files: FileList | undefined = $state();
    let {
        filesList = $bindable<File[]>([]),
        errors = [],
        newUploadsCount = $bindable(0),
        fileErrors = $bindable<string[]>([]),
        loading = $bindable(false),
        uploadFunc,
    }: {
        filesList: File[] | null | undefined;
        errors: string[];
        newUploadsCount?: number;
        fileErrors?: string[];
        loading: boolean;
        uploadFunc?: () => void;
    } = $props();

    let displayedErrors = $derived([...errors, ...fileErrors]);

    /**
     * Check the start of the file to get the mime type
     * and return it if it is a valid type
     * @param file the image file
     */
    async function getRealValidMime(file: File) {
        const magicNumber = new Uint8Array(
            await file.slice(0, 12).arrayBuffer(),
        );
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
     * Shortens a filename to 32 characters when exceeding 32 chars.
     * @param name the file name
     */
    function truncateFilename(name: string): string {
        if (name.length <= MAX_FILENAME_LENGTH) return name;
        return `${name.slice(0, MAX_FILENAME_LENGTH - 3)}...`;
    }

    /**
     * Validates a file's size and mime type against the same rules the backend enforces
     * @param file the image file
     * @returns every error message the file fails, prefixed with the file name (empty if valid)
     */
    async function validateImage(file: File): Promise<string[]> {
        const fileErrors: string[] = [];
        const shortName = truncateFilename(file.name);

        if (file.size > MAX_IMAGE_SIZE) {
            fileErrors.push(
                `${shortName}: Image too large, maximum file size is 10MB`,
            );
        }

        const realMimeType = await getRealValidMime(file);
        if (
            !mimeTypes.includes(file.type) ||
            realMimeType === undefined ||
            file.type !== realMimeType
        ) {
            fileErrors.push(
                `${shortName}: Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp`,
            );
        }

        return fileErrors;
    }

    /**
     * Validates and adds the selected files to the given file list, rejecting any
     * file that fails validation with every error it fails, not just the first one
     */
    async function appendImages() {
        if (!files) return;

        const newFileErrors: string[] = [];
        for (const file of files) {
            const fileError = await validateImage(file);
            if (fileError.length > 0) {
                newFileErrors.push(...fileError);
            } else {
                newUploadsCount += 1;
                filesList?.push(file);
            }
        }
        if (uploadFunc != null) {
            uploadFunc();
        }

        fileErrors = newFileErrors;
    }
</script>

<div class="d-flex flex-column mb-2">
    <button
        type="button"
        class="btn image-picker-button"
        class:is-invalid={displayedErrors.length > 0}
        onclick={() => {
            pfpInput.value = "";
            pfpInput.click();
        }}
        disabled={loading}
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
</div>

<style>
    .image-picker-button {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.6rem;
        width: 70%;
        padding: 0.75rem 1rem;
        border: 2px dashed #686868;
        background: #fff8e8;
        color: #686868;
        font-weight: 700;
        align-self: center;
    }

    .image-picker-button:hover {
        border-style: solid;
        background: #f3ead7;
    }

    .image-picker-button i {
        font-size: 1.2rem;
    }

    .image-picker-button.is-invalid {
        border-color: var(--bs-danger);
        color: var(--bs-danger);
    }
</style>
