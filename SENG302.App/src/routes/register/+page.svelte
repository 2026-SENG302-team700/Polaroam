<script lang="ts">
    import { goto } from "$app/navigation";
    import { resolve } from "$app/paths";
    import { fetchWithCsrf } from "$lib/csrf";
    import { addToast } from "$lib/toast/toast";
    import PasswordForm from "$lib/components/forms/password-form.svelte";
    import EmailForm from "$lib/components/forms/email-form.svelte";
    import DisplayNameForm from "$lib/components/forms/display-name-form.svelte";
    import CountrySelectForm from "$lib/components/forms/country-select-form.svelte";
    import AuthenticatorButton from "$lib/components/buttons/authenticator-button.svelte";
    import CancelButton from "$lib/components/buttons/cancel-button.svelte";
    import FormWrapper from "$lib/components/forms/form-wrapper.svelte";

    let email = $state("");
    let displayName = $state("");
    let selectedCountryCode = $state("");
    let password = $state("");
    let passwordConfirm = $state("");
    let loading = $state(false);
    let errors = $state({
        email: "",
        displayName: "",
        country: "",
        password: "",
        passwordConfirm: "",
    });

    /**
     * Handles user registration by sending a POST request to the server with the user's details.
     * Validates that all fields are filled in before making the request. If registration is successful,
     * redirects the user to the home page. If there is an error, displays an appropriate message.
     */
    async function registerUser() {
        try {
            loading = true;

            const response = await fetchWithCsrf(
                resolve(`/api/register` as any),
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        email: email,
                        displayName: displayName,
                        passwordString: password,
                        passwordConfirm: passwordConfirm,
                        country: selectedCountryCode,
                    }),
                },
            );

            const data = await response.json().catch(() => null);

            if (!response.ok) {
                // in case front end form checks were tampered with,
                // we display a toast with the badrequest response
                // from the back end.

                if (data?.errors) {
                    errors.email = data.errors.email ?? "";
                    errors.displayName = data.errors.displayName ?? "";
                    errors.country = data.errors.country ?? "";
                    errors.password = data.errors.password ?? "";
                    errors.passwordConfirm = data.errors.passwordConfirm ?? "";
                } else {
                    addToast(
                        data?.message || "Internal server error occurred.",
                        "error",
                    );
                }

                if (errors.password || errors.passwordConfirm) {
                    password = "";
                    passwordConfirm = "";
                }

                return;
            }
            // set email in local storage for validation page
            localStorage.setItem("email", email);
            goto(resolve(`/register/verification`));
        } catch (err) {
            addToast(
                "Failed to register user: " + (err as Error).message,
                "error",
            );
        } finally {
            loading = false;
        }
    }
</script>

{#snippet header()}
    <h1 class="text-center mb-4">Register</h1>
{/snippet}

{#snippet body()}
    <div class="mb-3">
        <EmailForm error={errors.email} {loading} bind:email />
    </div>
    <div class="mb-3">
        <DisplayNameForm
            bind:displayName
            error={errors.displayName}
            {loading}
            registering={true}
        />
    </div>
    <div class="mb-3">
        <CountrySelectForm
            bind:selectedCountryCode
            error={errors.country}
            {loading}
        />
    </div>
    <div class="mb-3">
        <PasswordForm
            id="initial-password-input"
            bind:password
            error={errors.password}
            {loading}
            passConfirm={false}
        />
    </div>
    <div class="mb-3">
        <PasswordForm
            id="confirm-password"
            bind:password={passwordConfirm}
            error={errors.passwordConfirm}
            {loading}
            passConfirm={true}
        />
    </div>
    <div class="mb-3">
        <PasswordForm
            bind:password
            error={errors.password}
            {loading}
            id="password"
        />
    </div>
    <div class="mb-3"></div>
    <div class="mb-3">
        <button
            class="btn btn-primary w-100"
            hidden={errors.email !=
                "Account is not validated yet, check your emails."}
            onclick={() => goto(resolve("/register/verification"))}
        >
            Verify Email
        </button>
    </div>
{/snippet}

{#snippet footer()}
    <div class="mb-3 post-form-actions">
        <AuthenticatorButton buttonType={"register"} onClick={registerUser} />
        <CancelButton path="/"></CancelButton>
    </div>
{/snippet}

<FormWrapper {header} {body} {footer} />
