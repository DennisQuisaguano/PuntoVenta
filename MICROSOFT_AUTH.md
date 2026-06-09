# Implementación Técnica: Autenticación con Microsoft (Azure AD / Entra ID)

Este documento detalla la integración del sistema con el ecosistema de identidad de Microsoft y la arquitectura de intercambio de tokens utilizada.

## 1. Arquitectura Híbrida (Identity Exchange)
El sistema utiliza un flujo de **Autenticación Externa con Autorización Local**. Esto significa:
- **Microsoft** actúa como el Proveedor de Identidad (IdP) para validar quién es el usuario.
- **Nuestra API** actúa como el Emisor de Tokens (Server) para definir qué puede hacer el usuario (Roles).

### ¿Por qué este enfoque?
1. **Soberanía de Datos:** No dependemos de que un administrador de Azure nos asigne roles. Los gestionamos en nuestra propia base de datos.
2. **Seguridad:** El token de Microsoft nunca se usa para acceder a datos sensibles de nuestro POS; solo sirve como "pasaporte" para obtener nuestro propio JWT.
3. **Eficiencia:** Las validaciones de peticiones son locales y no requieren llamadas constantes a los servidores de Microsoft.

## 2. Flujo Lógico de Inicio de Sesión
1. **Blazor (MSAL):** El usuario hace clic en "Iniciar sesión con Microsoft". La librería MSAL redirige al portal de Microsoft.
2. **Callback:** Microsoft devuelve un `id_token` a nuestra página `/authentication/login-callback`.
3. **Intercambio (Exchange):** El componente `Authentication.razor` captura ese token y lo envía a `POST api/Auth/microsoft-login`.
4. **Validación Backend:** La API descarga las claves públicas de Microsoft, valida la firma del token y extrae el email.
5. **Auto-Provisión:** Si el email `@uta.edu.ec` no existe en nuestra DB, se crea un nuevo usuario automáticamente con rol "Vendedor".
6. **Emisión Propia:** La API responde con un **JWT propio**, el cual Blazor guarda para todas las futuras peticiones.

