<script lang="ts">
    import { onMount } from "svelte";
    import type { Modal } from "bootstrap";
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import { addToast } from "$lib/toast/toast";
    import { user } from "$lib/stores/user";
    import regexPatterns from "../../../../../../SENG302.Shared/regexPatterns.json";
    import ProfilePic from "$lib/profilepic/profilepic.svelte";
    import ImageEditor from "$lib/image-editor/image-editor.svelte";
    import CancelButton from "$lib/components/buttons/cancel-button.svelte";
    import DisplayNameForm from "$lib/components/forms/display-name-form.svelte";
    import EmailForm from "$lib/components/forms/email-form.svelte";
    import CountrySelectForm from "$lib/components/forms/country-select-form.svelte";
    import AuthenticatorButton from "$lib/components/buttons/authenticator-button.svelte";
    import PasswordForm from "$lib/components/forms/password-form.svelte";
    import ToggleForm from "$lib/components/forms/toggle-form.svelte";

    let displayName = $state("");
    let email = $state("");
    let country = $state("");
    let files: FileList | null = $state(null);
    let pfpInput: HTMLInputElement;
    let modalElement: HTMLElement | undefined = $state();
    let pfpModalElement: HTMLElement | undefined = $state();
    let authModal: Modal | undefined;
    let pfpModal: Modal | undefined;
    let resendTimer = $state(0);
    let isSending = $state(false);
    let currentModalStep = $state("verify");
    let newPassword = $state("");
    let confirmPassword = $state("");
    let oldPassword = $state("");
    let digit1 = $state("");
    let digit2 = $state("");
    let digit3 = $state("");
    let digit4 = $state("");
    let digit5 = $state("");
    let digit6 = $state("");
    let userCode = $derived(
        digit1 + digit2 + digit3 + digit4 + digit5 + digit6,
    );
    let updatingPassword = $state(false);
    let aiToggle = $state();

    let codeError = $state("");
    let imageEditor: ImageEditor;
    let pfpCancelButton: HTMLButtonElement;
    
    let errors = $state({
        email: "",
        displayName: "",
        oldPassword: "",
        newPassword: "",
        confirmPassword: "",
        image: "",
    });
    let imageError = $state("");
    // automatically trigger the checkCode when the length reaches 6
    $effect(() => {
        if (userCode.length === 6) {
            checkCode();
        }
    });
    
    let userAcceptedPrivacyPolicy;

    onMount(async () => {
        retrieveUserData();

        const { Modal: BootstrapModal } = await import("bootstrap");

        if (modalElement) {
            authModal = new BootstrapModal(modalElement);
        }
        if (pfpModalElement) {
            pfpModal = new BootstrapModal(pfpModalElement);
        }
    });

    /**
     * Clears all currently set errors
     */
    function clearErrors() {
        errors.email = "";
        errors.displayName = "";
        errors.oldPassword = "";
        errors.newPassword = "";
        errors.confirmPassword = "";
        errors.image = "";
        imageError = "";
    }

    /**
     * Clears all the modal data
     */
    function clearModalData() {
        codeError = "";
        digit1 = "";
        digit2 = "";
        digit3 = "";
        digit4 = "";
        digit5 = "";
        digit6 = "";
    }

    /**
     * Clears the all fields in the update password form
     */
    function clearPasswordFields() {
        oldPassword = "";
        newPassword = "";
        confirmPassword = "";
    }

    /**
     * Start a new timer for the resend button to ensure the user cant spam their email
     */
    function startResendCountdown() {
        resendTimer = 30;
        const interval = setInterval(() => {
            resendTimer--;
            if (resendTimer <= 0) clearInterval(interval);
        }, 1000);
    }

    /**
     * Method used to send a new code to the user. Check the conditions are right and send a PUT request to the backend
     */
    async function requestPasswordChange() {
        clearModalData();
        currentModalStep = "verify";

        authModal?.show();

        if (!isSending && resendTimer == 0) {
            isSending = true;
            resendTimer = 0;
            try {
                const response = await fetchWithCsrf(
                    resolve(`/api/user/password/update/code/generation`),
                    {
                        method: "PUT",
                        headers: {
                            "Content-Type": "application/json",
                        },
                        body: JSON.stringify({
                            email: email,
                        }),
                    },
                );

                if (response.ok) {
                    addToast("Verification code sent!", "success");
                    startResendCountdown();
                } else {
                    const data = await response.json().catch(() => null);
                    codeError = data?.message || "Failed to send code.";
                }
            } catch (err) {
                codeError = "Failed to send email: " + (err as Error).message;
            } finally {
                isSending = false;
            }
        }
    }

    /// <summary>
    /// Sends GET request for the users data and
    /// sets the feilds the the retrieved data
    /// </summary>
    async function retrieveUserData() {
        try {
            const response = await fetchWithCsrf(resolve(`/api/user`), {
                method: "GET",
                credentials: "include",
            });

            const data = await response.json();
            if (!response.ok) {
                email = data.message || "Failed to fetch email.";
                displayName = data.message || "Failed to fetch username.";
                goto(resolve("/"));
                return;
            }
            email = data.email;
            displayName = data.displayName;
            country = data.country;
        } catch (err) {
            email = "Failed to fetch email: " + (err as Error).message;
            displayName = "Failed to fetch username: " + (err as Error).message;
            goto(resolve("/"));
        }
    }

    /// <summary>
    /// Automatically refocus on the next input box
    /// </summary>
    function handleInput(e: Event) {
        const input = e.target as HTMLInputElement;
        if (input.value && input.nextElementSibling) {
            (input.nextElementSibling as HTMLInputElement).focus();
        }
    }
    /// <summary>
    /// Move the focus back one box when backspace is clicked and the input box is empty
    /// </summary>
    function handleKeyDown(e: KeyboardEvent) {
        const input = e.target as HTMLInputElement;
        if (
            e.key === "Backspace" &&
            !input.value &&
            input.previousElementSibling
        ) {
            (input.previousElementSibling as HTMLInputElement).focus();
        }
    }

    /// <summary>
    /// Updates the users information with the provided information
    /// </summary>
    async function updateUser() {
        try {
            const response = await fetchWithCsrf(resolve(`/api/user`), {
                method: "PUT",
                credentials: "include",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    email,
                    displayName,
                    country,
                }),
            });

            if (response.ok) {
                addToast("Profile edited successful");
                const updatedUser = await response.json();
                user.update((u) => ({
                    ...u,
                    displayName: updatedUser.displayName,
                }));
                goto(resolve("/home"));
            } else {
                const data = await response.json().catch(() => null);
                if (data?.errors) {
                    errors.email = data.errors.email ?? "";
                    errors.displayName = data.errors.displayName ?? "";
                } else {
                    addToast(
                        data?.message || "Internal server error occurred.",
                        "error",
                    );
                }
                return;
            }
        } catch (err) {
            addToast((err as Error).message);
        }
    }

    /**
     * Validated the inputs to a change password request and sets the appropriate errors
     * the new passwords must match and be of the correct form
     * returns whether result is valid or not
     */
    function validateChangePasswordInputs() {
        clearErrors();
        var isValid = true;

        // Check not empty
        if (!oldPassword) {
            errors.oldPassword = "field is required";
            isValid = false;
        }
        if (!newPassword) {
            errors.newPassword = "field is required";
            isValid = false;
        }
        if (!confirmPassword) {
            errors.confirmPassword = "field is required";
            isValid = false;
        }

        // checks passwords match
        if (newPassword !== confirmPassword) {
            isValid = false;
            errors.confirmPassword = "Passwords do not match";
            confirmPassword = "";
        }

        // check password is valid
        const passwordRegex = new RegExp(regexPatterns.user.password);
        if (!passwordRegex.test(newPassword)) {
            isValid = false;
            errors.newPassword =
                "Password must be at least 8 characters long including at least one of each " +
                "uppercase, lowercase, numbers and special characters";
            newPassword = "";
            confirmPassword = "";
        }

        return isValid;
    }
    /**
     * Check the code the user supplied when the user clicks the verify button. Send a post request to the backend with the provided code.
     */
    async function checkCode() {
        if (userCode.length < 6) return;
        try {
            codeError = "";
            const response = await fetchWithCsrf(
                resolve(`/api/user/password/update/code/validation`),
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        Email: email,
                        Code: userCode,
                    }),
                },
            );

            if (!response.ok) {
                const data = await response.json().catch(() => null);
                codeError =
                    data?.message || `Error ${response.status}: Invalid code.`;
                digit1 = digit2 = digit3 = digit4 = digit5 = digit6 = "";
                const firstInput = document.querySelector(
                    "#code-input input",
                ) as HTMLInputElement;
                firstInput?.focus();
            } else {
                currentModalStep = "update";
            }
        } catch (err) {
            codeError = "Connection error. Please try again later.";
        }
    }

    /**
     * validates the input data and sends a request to the backend to update the users password
     */
    async function updatePassword() {
        const valid = validateChangePasswordInputs();

        updatingPassword = true;
        try {
            const response = await fetchWithCsrf(
                resolve(`/api/user/password/update`),
                {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        oldPassword: oldPassword,
                        newPassword: newPassword,
                        newPasswordConfirm: confirmPassword,
                    }),
                },
            );
            if (response.ok && valid) {
                addToast("New password updated successfully");
                authModal.hide();
                goto(resolve("/home/profile"));
                return;
            }

            const data = await response.json().catch(() => null);
            if (response.status === 400) {
                switch (data.message) {
                    case "Old password does not match password on file":
                        if (oldPassword) {
                            errors.oldPassword =
                                "Old password does not match password on file";
                        }
                        oldPassword = "";
                        break;
                    case "Passwords do not match":
                        errors.confirmPassword = "Password does not match";
                        confirmPassword = "";
                        break;
                    case "Password must be at least 8 characters long including at least one of each uppercase, lowercase, numbers and special characters":
                        errors.newPassword =
                            "Password must be at least 8 characters long including at least one of each uppercase, lowercase, numbers and special characters";
                        newPassword = "";
                        confirmPassword = "";
                        break;
                    case "New password can't be the same as old password":
                        errors.newPassword =
                            "New password can't be the same as old password";
                        newPassword = "";
                        confirmPassword = "";
                        break;
                }
            } else {
                addToast("Failed to update password ");
            }
        } catch (err) {
            addToast("Failed to update password");
        } finally {
            updatingPassword = false;
        }
    }

    /**
     * Sends an image to the image editor
     */
    async function sendToEditor() {
        clearErrors();
        imageEditor.highlightError(false);
        if (!files || files.length === 0) {
            return;
        }
        if (await imageEditor.validateImg(files[0])) {
            imageEditor.setImg(files[0]);
            pfpModal.show();
        }
    }

    /**
     * Updates the profile picture on the back end
     * @param imageData the x, y and zoom of the new profile picture
     * @param imageFile the file to upload
     */
    async function updatePfp() {
        const data = imageEditor.exportData();

        if (!data) {
            if (!imageError) {
                imageError = "No file Selected";
            }
        }
        errors.image = imageError;

        if (imageError) {
            imageEditor.highlightError(true);
            return;
        }

        let imageData = data.data;
        let imageFile = data.file;

        try {
            const formData = new FormData();
            formData.append("file", imageFile);
            formData.append("x", imageData.offsetX.toString());
            formData.append("y", imageData.offsetY.toString());
            formData.append("zoom", imageData.zoom.toString());

            const response = await fetchWithCsrf(
                resolve(`/api/user/pfp` as any),
                {
                    method: "PUT",
                    body: formData,
                },
            );

            if (!response.ok) {
                if (response.status == 500) {
                    throw new Error("Failed to upload picture");
                } else {
                    throw new Error(await response.text());
                }
            } else {
                user.update((u) => ({
                    ...u,
                    pfpData: imageData,
                }));
                pfpModal.hide();
            }
        } catch (err) {
            errors.image = (err as Error).message;
            imageEditor.highlightError(true);
        }
    }
    
    async function updateAiToggle(aiToggleState: boolean) {
        try {
            // implement api call to backend.
        } catch (error) {
            addToast("Failed to update AI tagging preference.", "error");
            aiToggle = !aiToggleState;
        }
    }
    
</script>

<div class="container d-flex flex-column flex-md-row">
    <div
        class="d-flex flex-column align-items-center justify-content-center m-3"
    >
        <div class="position-relative d-inline-block">
            <ProfilePic pfpData={$user.pfpData} size="xl" />

            <button
                type="button"
                class="btn btn-sm btn-primary rounded-circle position-absolute bottom-0 end-0 p-4 lh-1 d-flex align-items-center justify-content-center"
                onclick={() => {
                    pfpInput.value = "";
                    pfpInput.click();
                }}
            >
                <i class="bi bi-pencil-square fs-2"></i>
            </button>
        </div>
    </div>

    <div class="container d-flex flex-column flex-md-row">
        <div class="flex-grow-1 m-3">
            <form onsubmit={updateUser} novalidate>
                <div class="m-3" style="display: flex; ">
                    <CancelButton path={"/home/profile"} />
                    <div class="ms-auto">
                        <AuthenticatorButton buttonType={"update"} />
                    </div>
                </div>
                <div class="mb-4">
                    <h5 class="text-muted mb-2">Personal Information</h5>
                    <hr class="mt-0" style="opacity: 0.15;" />
                    <div class="mb-3">
                        <label for="displayName" class="form-label"
                            >Display Name</label
                        >
                        <DisplayNameForm
                            bind:displayName
                            error={errors.displayName}
                            registering={false}
                        />
                    </div>
                    <div class="mb-3">
                        <label for="userEmail" class="form-label">Email</label>
                        <EmailForm bind:email error={errors.email} />
                    </div>
                    <div class="mb-3">
                        <label for="country" class="form-label">Country</label>
                        <CountrySelectForm bind:selectedCountryCode={country} />
                    </div>
                </div>
                <div class="mb-3"></div>
                <div class="mt-5 mb-4">
                    <h5 class="text-muted mb-2">Preferences</h5>
                    <hr class="mt-0" style="opacity: 0.15;" />
                    <div
                            class="d-flex align-items-center justify-content-between"
                    >
                        <label for="aiToggle" class="form-label">Photo Tagging w/ Artifical Intelligence</label>
                        <ToggleForm id="aiToggle" bind:checked={aiToggle} onchange="{
                        () => updateAiToggle(aiToggle)
                        }"/>
                    </div>
                </div>

                <div class="mt-5 mb-4">
                    <h5 class="text-muted mb-2">Account Security</h5>
                    <hr class="mt-0" style="opacity: 0.15;" />
                    <div
                        class="d-flex align-items-center justify-content-between"
                    >
                        <p class="small text-secondary mb-0">
                            Change your password to keep your account secure.
                        </p>
                        <button
                            type="button"
                            class="btn btn-outline-primary btn-sm"
                            onclick={requestPasswordChange}
                        >
                            Update Password
                        </button>
                    </div>
                </div>
            </form>
        </div>
    </div>
</div>

<!-- update password modal -->
<div
    class="modal fade"
    bind:this={modalElement}
    tabindex="-1"
    aria-hidden="true"
>
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content p-4">
            <div class="modal-header border-0">
                <h5 class="modal-title fw-bold">
                    {currentModalStep === "verify"
                        ? "Verify Your Identity"
                        : "Set New Password"}
                </h5>
            </div>
            <div class="modal-body">
                {#if currentModalStep === "verify"}
                    <div class="text-centre">
                        <p class="text-secondary">
                            We've sent a 6-digit verification code to <br />
                            <span class="text-dark fw-bold">{email}</span>
                        </p>
                        <div id="code-input" class="d-flex gap-2 mt-4 mb-4">
                            <input
                                type="text"
                                class="form-control form-control-lg text-center"
                                maxlength="1"
                                bind:value={digit1}
                                oninput={handleInput}
                                onkeydown={handleKeyDown}
                            />
                            <input
                                type="text"
                                class="form-control form-control-lg text-center"
                                maxlength="1"
                                bind:value={digit2}
                                oninput={handleInput}
                                onkeydown={handleKeyDown}
                            />
                            <input
                                type="text"
                                class="form-control form-control-lg text-center"
                                maxlength="1"
                                bind:value={digit3}
                                oninput={handleInput}
                                onkeydown={handleKeyDown}
                            />
                            <input
                                type="text"
                                class="form-control form-control-lg text-center"
                                maxlength="1"
                                bind:value={digit4}
                                oninput={handleInput}
                                onkeydown={handleKeyDown}
                            />
                            <input
                                type="text"
                                class="form-control form-control-lg text-center"
                                maxlength="1"
                                bind:value={digit5}
                                oninput={handleInput}
                                onkeydown={handleKeyDown}
                            />
                            <input
                                type="text"
                                class="form-control form-control-lg text-center"
                                maxlength="1"
                                bind:value={digit6}
                                oninput={handleInput}
                                onkeydown={handleKeyDown}
                            />
                        </div>

                        {#if codeError}
                            <div class="text-danger small mb-3 animate-fade-in">
                                <i class="bi bi-exclamation-circle-fill me-1" />
                                {codeError}
                            </div>
                        {/if}

                        <button
                            class="btn btn-link btn-sm text-decoration-none"
                            onclick={requestPasswordChange}
                            disabled={resendTimer > 0 || isSending}
                        >
                            {#if resendTimer > 0}
                                Resend code in {resendTimer}s
                            {:else if isSending}
                                Sending...
                            {:else}
                                Resend Code
                            {/if}
                        </button>
                    </div>
                {:else}
                    <form onsubmit={() => updatePassword()}>
                        <div class="mb-3">
                            <label
                                for="oldPassword"
                                class="form-label small fw-bold text-secondary"
                                >Current Password *</label
                            >
                            <PasswordForm
                                bind:password={oldPassword}
                                error={errors.oldPassword}
                            />
                        </div>
                        <div class="mb-3">
                            <label
                                for="newPassword"
                                class="form-label small fw-bold text-secondary"
                                >New Password *</label
                            >
                            <PasswordForm
                                bind:password={newPassword}
                                error={errors.newPassword}
                            />
                        </div>
                        <div class="mb-3">
                            <label
                                for="confirmPassword"
                                class="form-label small fw-bold text-secondary"
                                >Confirm New Password *</label
                            >
                            <PasswordForm
                                bind:password={confirmPassword}
                                error={errors.confirmPassword}
                            />
                        </div>
                        <button
                            type="submit"
                            class="btn btn-primary w-100 py-2 mt-3"
                            disabled={updatingPassword}
                            >{updatingPassword
                                ? "Updating..."
                                : "Update Password"}
                        </button>
                    </form>
                {/if}
                <button
                    type="button"
                    class="btn btn-secondary w-100 py-2 mt-3"
                    aria-label="Close"
                    onclick={() => {
                        authModal.hide();
                        clearErrors();
                        clearPasswordFields();
                    }}>Cancel</button
                >
            </div>
        </div>
    </div>
</div>

<!-- Modal for pfp selection -->
<div
    class="modal fade"
    id="pfpInputModal"
    data-bs-backdrop="static"
    data-bs-keyboard="false"
    tabindex="-1"
    bind:this={pfpModalElement}
    aria-labelledby="pfpInputModalLabel"
    aria-hidden="true"
>
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h1 class="modal-title fs-5" id="pfpInputModalLabel">
                    Edit Profile Picture
                </h1>
                <button
                    type="button"
                    class="btn-close"
                    onclick={() => pfpModal.hide()}
                    aria-label="Close"
                ></button>
            </div>
            <div class="modal-body">
                <form onsubmit={() => updatePfp()} novalidate>
                    <input
                        accept="image/webp, image/jpeg, image/png, image/gif, image/svg+xml"
                        bind:files
                        bind:this={pfpInput}
                        id="pfp"
                        name="pfp"
                        type="file"
                        class="d-none"
                        onchange={async () => {
                            imageEditor.reset();
                            clearErrors();
                            await sendToEditor();
                            // Reset the value so that if we select the same image a second time the on:change event is triggered
                            pfpInput.value = "";
                        }}
                    />
                    <ImageEditor bind:this={imageEditor}></ImageEditor>
                    {#if errors.image}
                        <div class="text-danger small mt-1">
                            {errors.image}
                        </div>
                    {/if}
                    <div class="modal-footer">
                        <button type="submit" class="btn btn-primary"
                            >Submit</button
                        >
                        <button
                            type="button"
                            class="btn btn-secondary"
                            onclick={() => pfpModal.hide()}
                            bind:this={pfpCancelButton}>Cancel</button
                        >
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>
