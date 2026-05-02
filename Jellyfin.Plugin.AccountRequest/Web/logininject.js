(function () {
    if (window.__jellyfinAccountRequestLoginInjected) {
        return;
    }

    window.__jellyfinAccountRequestLoginInjected = true;

    const buttonId = 'accountRequestLoginButton';
    const modalId = 'accountRequestLoginModal';
    const styleId = 'accountRequestLoginStyles';

    function apiUrl(path) {
        const cleanPath = path.replace(/^\/+/, '');
        if (window.ApiClient && typeof window.ApiClient.getUrl === 'function') {
            return window.ApiClient.getUrl(cleanPath);
        }

        const webIndex = window.location.pathname.indexOf('/web/');
        const basePath = webIndex >= 0 ? window.location.pathname.slice(0, webIndex) : '';
        return `${basePath}/${cleanPath}`;
    }

    function addStyles() {
        if (document.getElementById(styleId)) {
            return;
        }

        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
                    .accountRequestLoginBackdrop {
                        align-items: center;
                        background: rgba(0, 0, 0, .72);
                        display: none;
                        inset: 0;
                        justify-content: center;
                        position: fixed;
                        z-index: 9999;
                    }

                    .accountRequestLoginBackdrop.isOpen {
                        display: flex;
                    }

                    .accountRequestLoginDialog {
                        background: var(--card-focused-background, #202020);
                        border-radius: .5rem;
                        box-shadow: 0 .5rem 2rem rgba(0, 0, 0, .45);
                        color: var(--theme-text-color, #fff);
                        max-width: 32rem;
                        padding: 1.5rem;
                        width: calc(100% - 2rem);
                    }

                    .accountRequestLoginField {
                        margin: 1rem 0;
                    }

                    .accountRequestLoginField label {
                        display: block;
                        margin-bottom: .35rem;
                    }

                    .accountRequestLoginField input,
                    .accountRequestLoginField textarea {
                        background: rgba(0, 0, 0, .25);
                        border: 1px solid rgba(255, 255, 255, .2);
                        border-radius: .25rem;
                        box-sizing: border-box;
                        color: inherit;
                        min-height: 2.5rem;
                        padding: .6rem;
                        width: 100%;
                    }

                    .accountRequestLoginField textarea {
                        min-height: 6rem;
                        resize: vertical;
                    }

                    .accountRequestLoginActions {
                        display: flex;
                        flex-wrap: wrap;
                        gap: .75rem;
                        margin-top: 1rem;
                    }

                    .accountRequestLoginMessage {
                        color: var(--theme-secondary-text-color, rgba(255, 255, 255, .75));
                        min-height: 1.25rem;
                    }
                `;
        document.head.appendChild(style);
    }

    function ensureModal() {
        const existingModal = document.getElementById(modalId);
        if (existingModal) {
            return existingModal;
        }

        addStyles();

        const modal = document.createElement('div');
        modal.id = modalId;
        modal.className = 'accountRequestLoginBackdrop';
        modal.setAttribute('role', 'dialog');
        modal.setAttribute('aria-modal', 'true');
        modal.innerHTML = `
                    <form class="accountRequestLoginDialog">
                        <h2>Request an Account</h2>
                        <p class="accountRequestLoginMessage" data-message>Enter your details and an administrator will review your request.</p>
                        <div class="accountRequestLoginField">
                            <label for="accountRequestUsername">Desired username</label>
                            <input id="accountRequestUsername" name="username" autocomplete="username" maxlength="64" required>
                        </div>
                        <div class="accountRequestLoginField">
                            <label for="accountRequestEmail">Email</label>
                            <input id="accountRequestEmail" name="email" type="email" autocomplete="email" required>
                        </div>
                        <div class="accountRequestLoginField">
                            <label for="accountRequestMessage">Message</label>
                            <textarea id="accountRequestMessage" name="message" maxlength="1000" placeholder="Tell the admin why you need access"></textarea>
                        </div>
                        <div class="accountRequestLoginActions">
                            <button type="submit" is="emby-button" class="raised button-submit">
                                <span>Submit Request</span>
                            </button>
                            <button type="button" is="emby-button" class="raised" data-close>
                                <span>Cancel</span>
                            </button>
                        </div>
                    </form>
                `;

        modal.querySelector('[data-close]').addEventListener('click', () => {
            modal.classList.remove('isOpen');
        });

        modal.addEventListener('click', event => {
            if (event.target === modal) {
                modal.classList.remove('isOpen');
            }
        });

        modal.querySelector('form').addEventListener('submit', submitRequest);
        document.body.appendChild(modal);
        return modal;
    }

    async function submitRequest(event) {
        event.preventDefault();

        const form = event.currentTarget;
        const modal = document.getElementById(modalId);
        const message = modal.querySelector('[data-message]');
        const submitButton = form.querySelector('button[type="submit"]');
        const payload = {
            username: form.username.value.trim(),
            email: form.email.value.trim(),
            message: form.message.value.trim()
        };

        submitButton.disabled = true;
        message.textContent = 'Submitting request...';

        try {
            const response = await fetch(apiUrl('AccountRequest/submit'), {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                const error = await response.json().catch(() => ({ message: response.statusText }));
                throw new Error(error.message || response.statusText);
            }

            form.reset();
            message.textContent = 'Request submitted. An administrator will review it soon.';
            window.setTimeout(() => modal.classList.remove('isOpen'), 1800);
        } catch (error) {
            message.textContent = error.message || 'Unable to submit account request.';
        } finally {
            submitButton.disabled = false;
        }
    }

    function createRequestButton() {
        const requestButton = document.createElement('button');
        requestButton.id = buttonId;
        requestButton.type = 'button';
        requestButton.setAttribute('is', 'emby-button');
        requestButton.className = 'raised cancel block';
        requestButton.innerHTML = '<span>Account Request</span>';
        requestButton.addEventListener('click', () => {
            ensureModal().classList.add('isOpen');
        });
        return requestButton;
    }

    function injectButton() {
        if (document.getElementById(buttonId)) {
            return;
        }

        const loginPage = document.querySelector('#loginPage');
        if (!loginPage) {
            return;
        }

        const manualForm = loginPage.querySelector('form.manualLoginForm');
        const readOnly = loginPage.querySelector('.readOnlyContent');
        const requestButton = createRequestButton();

        const manualVisible = manualForm && !manualForm.classList.contains('hide');
        if (manualVisible) {
            const loginButton = manualForm.querySelector('button[type="submit"].button-submit, button[type="submit"]');
            if (loginButton && loginButton.parentElement) {
                loginButton.insertAdjacentElement('afterend', requestButton);
                return;
            }
        }

        if (readOnly) {
            const forgot = readOnly.querySelector('.btnForgotPassword');
            if (forgot) {
                forgot.insertAdjacentElement('afterend', requestButton);
                return;
            }

            const disclaimer = readOnly.querySelector('.loginDisclaimerContainer');
            if (disclaimer) {
                readOnly.insertBefore(requestButton, disclaimer);
                return;
            }

            readOnly.appendChild(requestButton);
        }
    }

    const observer = new MutationObserver(injectButton);
    observer.observe(document.documentElement, {
        childList: true,
        subtree: true
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', injectButton);
    } else {
        injectButton();
    }
}());
