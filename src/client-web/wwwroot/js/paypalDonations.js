window.laylaPayPalDonations = {
    renderButton: async function (containerId, clientId, currency, orderId, dotNetRef) {
        const container = document.getElementById(containerId);
        if (!container) {
            return;
        }

        container.innerHTML = "";
        await this.ensureSdk(clientId, currency);

        if (!window.paypal) {
            await dotNetRef.invokeMethodAsync("HandlePayPalError", "No se pudo cargar PayPal Sandbox.");
            return;
        }

        window.paypal.Buttons({
            createOrder: function () {
                return orderId;
            },
            onApprove: async function (data) {
                await dotNetRef.invokeMethodAsync("CaptureDonationFromPayPal", data.orderID || orderId);
            },
            onError: async function () {
                await dotNetRef.invokeMethodAsync("HandlePayPalError", "PayPal no pudo completar el donativo.");
            },
            onCancel: async function () {
                await dotNetRef.invokeMethodAsync("HandlePayPalError", "El donativo fue cancelado.");
            }
        }).render(container);
    },

    ensureSdk: function (clientId, currency) {
        const src = "https://www.paypal.com/sdk/js?client-id=" + encodeURIComponent(clientId)
            + "&currency=" + encodeURIComponent(currency)
            + "&intent=capture";
        const existing = document.getElementById("paypal-sdk");

        if (existing && existing.getAttribute("src") === src && window.paypal) {
            return Promise.resolve();
        }

        if (existing) {
            existing.remove();
            window.paypal = undefined;
        }

        return new Promise(function (resolve, reject) {
            const script = document.createElement("script");
            script.id = "paypal-sdk";
            script.src = src;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }
};
