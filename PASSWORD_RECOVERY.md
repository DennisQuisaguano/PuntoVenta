# Implementación Técnica: Recuperación de Contraseña

Este documento detalla la arquitectura y las decisiones técnicas tomadas para la funcionalidad de recuperación de acceso.

## 1. Arquitectura de Servicios (Inversión de Dependencias)
Siguiendo los principios de **Clean Architecture**, el envío de correos se desacopló de la lógica de negocio:

- **Interfaz (`Application`):** `IEmailService.cs` define el contrato `SendEmailAsync`. La capa de aplicación no sabe "cómo" se envía el correo, solo sabe que puede enviarlo.
- **Implementación (`Infrastructure`):** `EmailService.cs` utiliza la librería **MailKit** para conectarse al servidor SMTP de Gmail.
- **Lógica de Negocio (`AuthService`):** Orquestra la generación de tokens de seguridad y la construcción de la URL de restablecimiento.

## 2. Seguridad del Proceso
- **Cifrado de Tokens:** Se utiliza `UserManager<ApplicationUser>.GeneratePasswordResetTokenAsync(user)`, que genera un token criptográficamente seguro y vinculado exclusivamente a un usuario.
- **Protección contra Enumeración:** Si un atacante ingresa un correo que no existe, el sistema responde con el mismo mensaje de éxito ("Si el correo está registrado..."). Esto evita que se descubran correos válidos en el sistema.
- **Expiración de Enlaces:** El token tiene una validez temporal limitada por la configuración de ASP.NET Identity (por defecto 24 horas).
- **Validación de Complejidad:** Al establecer la nueva clave, se fuerzan las reglas de negocio: 8-10 caracteres, mayúsculas, minúsculas, números y caracteres especiales.

## 3. Tecnologías y Librerías (NuGet)
- **MailKit (4.10.0):** Librería líder en .NET para manejo de protocolos de correo (SMTP, POP3, IMAP). Elegida por su soporte nativo para TLS/SSL y asincronismo.
- **MimeKit:** Utilizada para construir mensajes de correo con formato HTML profesional y responsivo.

## 4. Configuración de Servidor (SMTP)
Para el envío real se utiliza **Gmail** con el método de "Contraseña de Aplicación".
- **Servidor:** `smtp.gmail.com`
- **Puerto:** 587 (STARTTLS)
- **Seguridad:** Requiere autenticación y cifrado de canal.

---
*Diseñado bajo estándares de ingeniería de software senior para el proyecto Punto de Venta.*
