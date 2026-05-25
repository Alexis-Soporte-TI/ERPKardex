/**
 * Token Refresh Manager
 * Refresca automáticamente el token/sesión en cada llamada AJAX exitosa
 * 
 * El token se extiende solo cuando hay operaciones reales en el backend (GET, POST, PUT, DELETE)
 * Si la sesión expira (401), redirige automáticamente a login
 */
class TokenRefreshManager {
    constructor(options = {}) {
        this.REFRESH_ENDPOINT = options.refreshEndpoint || '/api/auth/refresh-session';
        this.isRefreshing = false;
        this.init();
    }

    /**
     * Inicializa el gestor de refresh
     */
    init() {
        this.interceptAjaxCalls();
        this.interceptFetchCalls();
        console.log('TokenRefreshManager initialized - Token will refresh on successful API calls');
    }

    /**
     * Intercepta llamadas jQuery AJAX ($.ajax, $.get, $.post, $.getJSON, etc.)
     */
    interceptAjaxCalls() {
        if (typeof $ === 'undefined') return; // jQuery no cargado

        // Interceptar todas las llamadas AJAX
        $(document).ajaxSuccess((event, xhr, settings) => {
            // Solo refrescar en llamadas exitosas (2xx)
            // IMPORTANTE: Excluir la propia ruta de refresh
            if (xhr.status >= 200 && xhr.status < 300 && !settings.url.includes(this.REFRESH_ENDPOINT)) {
                this.refreshSessionSilently();
            }
        });

        // Detectar errores 401
        $(document).ajaxError((event, jqxhr, settings, thrownError) => {
            if (jqxhr.status === 401) {
                console.warn('Session expired (401) - redirecting to login');
                this.handleSessionExpired();
            }
        });
    }

    /**
     * Intercepta llamadas fetch API nativa
     */
    interceptFetchCalls() {
        const originalFetch = window.fetch;

        window.fetch = async (...args) => {
            try {
                // CORRECCIÓN: Usar 'window' como contexto en lugar de 'this'
                const response = await originalFetch.apply(window, args);

                // Obtener la URL de la petición para evitar interceptar el propio refresh
                const url = typeof args[0] === 'string' ? args[0] : (args[0] instanceof Request ? args[0].url : '');

                // Detectar 401 en fetch
                if (response.status === 401) {
                    console.warn('Session expired (401) - redirecting to login');
                    this.handleSessionExpired();
                    return response;
                }

                // Refrescar en llamadas exitosas, EXCLUYENDO el propio endpoint de refresh
                if (response.status >= 200 && response.status < 300 && !url.includes(this.REFRESH_ENDPOINT)) {
                    this.refreshSessionSilently();
                }

                return response;
            } catch (error) {
                console.error('Fetch error:', error);
                throw error;
            }
        };
    }

    /**
     * Refresca la sesión silenciosamente
     */
    async refreshSessionSilently() {
        if (this.isRefreshing) return;

        this.isRefreshing = true;

        try {
            const response = await fetch(this.REFRESH_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include' // Importante: enviar cookies
            });

            if (response.status === 401) {
                this.handleSessionExpired();
                return;
            }

            if (response.ok) {
                const data = await response.json();
                console.log('✓ Session refreshed at', new Date(data.timestamp).toLocaleTimeString());
            } else {
                console.warn('Failed to refresh session:', response.status);
            }
        } catch (error) {
            console.error('Error refreshing session:', error);
        } finally {
            this.isRefreshing = false;
        }
    }

    /**
     * Maneja la expiración de sesión
     */
    handleSessionExpired() {
        if (typeof toastr !== 'undefined') {
            toastr.warning("Su sesión ha expirado. Será redirigido a iniciar sesión.");
        } else {
            alert("Su sesión ha expirado. Será redirigido a iniciar sesión.");
        }

        setTimeout(() => {
            window.location.href = '/Identity/Account/Login';
        }, 1500);
    }

    /**
     * Refrescar manualmente la sesión
     */
    async manualRefresh() {
        console.log('Manual session refresh requested');
        await this.refreshSessionSilently();
    }
}

// Inicializar el gestor cuando el documento esté listo
document.addEventListener('DOMContentLoaded', function() {
    window.tokenRefreshManager = new TokenRefreshManager();
});
