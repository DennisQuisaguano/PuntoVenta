# Implementación Técnica: Auditoría General Automática (AuditLog)

Este documento detalla el sistema de trazabilidad y auditoría forense implementado en el núcleo de persistencia del sistema.

## 1. Concepto y Estrategia
A diferencia de los logs tradicionales que guardan mensajes de texto, este sistema implementa una **Auditoría de Datos Estructurada**. Captura el estado exacto de una entidad antes y después de cada transacción.

- **Tecnología Core:** Entity Framework Core `ChangeTracker`.
- **Ubicación:** `Infrastructure/Data/AppDbContext.cs`.
- **Acción:** Automática (se dispara en cada `SaveChangesAsync`).

## 2. Arquitectura del Sistema
El flujo de auditoría sigue un patrón de interceptación:

1. **Detección:** EF Core identifica qué objetos han sido creados, modificados o eliminados.
2. **Pre-procesamiento (`OnBeforeSaveChanges`):**
    - Se extraen las propiedades de la entidad.
    - Se guardan los **Valores Originales** (OldValues) y los **Valores Actuales** (NewValues).
    - Se identifican qué columnas específicas fueron alteradas.
3. **Serialización:** Los cambios se convierten a formato **JSON** para permitir el almacenamiento de estructuras de datos flexibles en una sola columna de la base de datos.
4. **Persistencia Atómica:** El log de auditoría se incluye en la misma transacción que los datos de negocio. Si la operación principal falla, no se genera un log falso.

## 3. Estructura del AuditLog
Cada registro de auditoría contiene:
- **TableName:** Nombre de la tabla afectada.
- **Type:** Tipo de operación (`Create`, `Update`, `Delete`).
- **UserId:** Usuario que realizó el cambio (obtenido mediante `IHttpContextAccessor`).
- **DateTime:** Marca de tiempo en formato UTC.
- **PrimaryKey:** Identificador único del registro afectado.
- **OldValues / NewValues:** Estados completos en JSON para comparación.

## 4. Beneficios para la Empresa (Puntos de Defensa)
1. **No-Repudio:** Es imposible que un usuario realice un cambio crítico (como cambiar el precio de un producto o anular una factura) sin dejar un rastro digital exacto.
2. **Recuperación de Datos:** Al tener los `OldValues`, es técnicamente posible revertir un cambio manual erróneo consultando el log.
3. **Transparencia Total:** El administrador puede auditar el sistema sin necesidad de pedir explicaciones, viendo directamente la "evidencia" técnica.
4. **Cero Mantenimiento:** Al estar integrado en el `AppDbContext`, cualquier tabla nueva que se agregue al sistema en el futuro será auditada automáticamente sin escribir código adicional.

## 5. Visualización (HCI)
Se implementó una interfaz avanzada en Blazor que permite:
- Buscar por cualquier criterio (tabla, usuario, acción).
- Comparar valores visualmente.
- Analizar cambios en campos específicos mediante la columna `AffectedColumns`.

---
*Esta funcionalidad eleva el sistema a un estándar de grado bancario/contable, garantizando la integridad inquebrantable de la información.*
