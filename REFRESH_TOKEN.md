# Implementación Técnica: Refresh Tokens y Renovación Automática

Este documento detalla el mecanismo de persistencia de sesión y seguridad avanzada implementado en el sistema.

## 1. Arquitectura de Tokens (Refresh Token Rotation)
El sistema implementa una de las estrategias de seguridad más robustas para aplicaciones web: la **Rotación de Refresh Tokens**.

- **JWT (Access Token):** Se utiliza para autorizar cada petición a la API. Tiene una duración de 8 horas.
- **Refresh Token:** Es un token de larga duración y uso único que reside en la base de datos vinculado al usuario.
- **Mecanismo de Rotación:** Cada vez que se usa un Refresh Token para obtener un nuevo JWT, el sistema invalida el Refresh Token anterior y emite uno nuevo. Esto previene ataques de "replay" si un token fuera interceptado.

## 2. Renovación Proactiva (Interceptor de Red)
A diferencia de las implementaciones básicas que esperan a que una petición falle (401 Unauthorized) para renovar, nuestro sistema utiliza un **Interceptor Proactivo Senior** en el Frontend.

### Funcionamiento del Interceptor (`JwtInterceptor.cs`):
1. **Inspección de Petición:** Antes de que cualquier llamada salga de Blazor hacia la API, el Interceptor analiza el JWT guardado en el navegador.
2. **Cálculo de Expiración:** El Interceptor decodifica el payload del JWT y extrae el claim `exp` (timestamp de expiración).
3. **Umbral de Seguridad (5 Minutos):** Si el token va a expirar en los próximos 5 minutos, el Interceptor detiene la petición original temporalmente.
4. **Llamada Silenciosa:** Se realiza una petición en segundo plano al endpoint `api/Auth/refresh` enviando el par de tokens actuales.
5. **Sincronización:** La API valida la identidad, genera nuevos tokens y el Interceptor actualiza el `localStorage`.
6. **Continuidad:** La petición original se reanuda con el nuevo token, sin que el usuario note ninguna interrupción.

## 3. Ventajas Técnicas (Puntos de Defensa)
1. **Experiencia de Usuario (UX):** El usuario nunca verá una sesión cerrada abruptamente mientras esté trabajando. El sistema se "autorepara" solo.
2. **Seguridad Mejorada:** Permite trabajar con tiempos de vida de JWT más cortos (ej: 15 min) sin afectar la usabilidad, minimizando la ventana de exposición en caso de robo de token.
3. **Optimización de Recursos:** La validación se hace del lado del cliente antes de enviar la petición, ahorrando una llamada fallida y una excepción en el servidor.

---
*Esta implementación garantiza que el Punto de Venta sea apto para jornadas laborales extensas con máxima fluidez y seguridad.*
