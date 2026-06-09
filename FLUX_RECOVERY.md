# Flujo de Usuario: Recuperación de Acceso

Este documento explica el "Viaje del Usuario" (User Journey) y el flujo de datos a través de las capas del sistema.

## 1. Paso 1: Solicitud de Recuperación
- **UI (Blazor):** El usuario navega a `/forgot-password` e ingresa su email.
- **Acción:** Se llama al endpoint `POST api/Auth/forgot-password`.
- **Backend:** 
    1. El sistema verifica si el usuario existe.
    2. Si existe, genera un **Token de Seguridad**.
    3. Construye un enlace dinámico inyectando el token y el email en la URL.
    4. Envía el correo mediante `IEmailService`.

## 2. Paso 2: Recepción del Correo
- **Usuario:** Abre su bandeja de entrada y hace clic en el botón "Restablecer mi Contraseña".
- **URL generada:** `https://localhost:7279/reset-password?token=XXX&email=user@test.com`

## 3. Paso 3: Establecer Nueva Clave
- **UI (Blazor):** La página `/reset-password` lee el `token` y el `email` de la URL automáticamente.
- **Acción:** El usuario ingresa su nueva contraseña (verificándola con el ícono de "ojo").
- **Validación:** El sistema asegura que las dos claves coincidan y cumplan los requisitos mínimos.

## 4. Paso 4: Confirmación en Backend
- **Acción:** Se llama al endpoint `POST api/Auth/reset-password`.
- **Proceso:**
    1. Se valida el Token contra la base de datos de Microsoft Identity.
    2. Si es válido y no ha expirado, se actualiza el Hash de la contraseña.
    3. Se marca `MustChangePassword = false` para el usuario.
- **Finalización:** El usuario es redirigido al Login para entrar con su nueva clave.

---
*Este flujo garantiza que solo el dueño del correo electrónico pueda cambiar la contraseña de su cuenta.*
