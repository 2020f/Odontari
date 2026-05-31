# PLANWAS.md

## Objetivo

Implementar en Odontari un modulo **WhatsApp multi-clinica con numero propio por clinica**, usando **Meta Cloud API directo**.

Cada clinica tendra su propio numero WhatsApp Business conectado a Odontari. Cuando un paciente reciba o envie mensajes, vera el numero/nombre de su clinica, no el de Odontari.

## Concepto Base

Odontari tendra una integracion central con Meta Cloud API, pero cada clinica tendra su propia configuracion WhatsApp asociada a `ClinicaId`.

```text
Odontari App en Meta
   -> Webhook unico de Odontari
   -> Muchas clinicas conectadas
   -> Cada clinica tiene su PhoneNumberId/token/numero
```

Entrada:

```text
Meta -> Webhook Odontari -> metadata.phone_number_id -> ClinicaId -> Inbox
```

Salida:

```text
Odontari -> ClinicaId -> PhoneNumberId/token -> Meta /{PhoneNumberId}/messages -> Paciente
```

## Decision Tecnica

Proveedor elegido:

```text
Meta Cloud API directo
```

Esto implica:

- Odontari se conecta directamente con Meta Graph API.
- Cada clinica tiene su propio numero WhatsApp Business.
- Odontari guarda el `PhoneNumberId`, `WABA ID`, token y estado por `ClinicaId`.
- El webhook entrante es central, no uno por clinica.
- La salida de mensajes usa el `PhoneNumberId` y token de la clinica correspondiente.

## Fase 1: Meta App Central

Crear/configurar en Meta:

- Meta Developer App de Odontari.
- Producto WhatsApp.
- Webhook URL de Odontari.
- Verify token.
- Permisos necesarios para WhatsApp Business.
- Suscripcion a eventos de mensajes y estados.

Para MVP manual:

- Configurar 1 numero real de prueba.
- Obtener `WABA ID`.
- Obtener `PhoneNumberId`.
- Obtener token.
- Validar envio de plantilla.
- Validar recepcion de mensajes por webhook.

## Fase 2: Modelo Multi-Clinica En Odontari

Crear modelos/tablas base.

### WhatsAppCuentaClinica

```text
Id
ClinicaId
Proveedor = MetaCloudApi
WabaId
PhoneNumberId
NumeroTelefono
DisplayName
AccessTokenCifrado
EstadoConexion
FechaConexion
Activo
```

### WhatsAppConversacion

```text
Id
ClinicaId
PacienteId
TelefonoPaciente
UltimoMensajeAt
Estado
MensajesSinLeer
```

### WhatsAppMensaje

```text
Id
ClinicaId
ConversacionId
PacienteId
CitaId nullable
Direccion: Entrante/Saliente
Tipo: Texto/Plantilla/Imagen/Documento
Contenido
ProviderMessageId
Estado: Pendiente/Enviado/Entregado/Leido/Fallido
Error
FechaCreacion
```

### WhatsAppRecordatorioCita

```text
Id
ClinicaId
CitaId
Tipo: 24H/2H
ProgramadoPara
EnviadoAt
Estado
WhatsAppMensajeId
```

Regla obligatoria:

```text
Todo query, pantalla, envio, webhook y job debe respetar ClinicaId.
```

## Fase 3: Configuracion Por Clinica

Crear pantalla de configuracion para WhatsApp.

Inicialmente puede ser administrada por SaaS o por soporte interno de Odontari.

Campos visibles:

```text
WhatsApp conectado: Si/No
Numero conectado
Display name
Estado
Recordatorios activos
Plantilla usada
```

Para MVP, los datos se pueden cargar manualmente:

```text
WABA ID
PhoneNumberId
Token
Numero
Display name
```

Mas adelante se reemplaza o complementa con:

```text
Conectar WhatsApp Business
```

usando Embedded Signup.

## Fase 4: Salida De Mensajes

Crear servicios:

```text
IWhatsAppProvider
IWhatsAppMessageService
MetaWhatsAppProvider
```

Responsabilidades:

- Enviar plantilla.
- Enviar texto dentro de ventana de conversacion.
- Guardar resultado.
- Guardar error.
- Asociar mensaje con `ClinicaId`, `PacienteId` y opcionalmente `CitaId`.

Endpoint de Meta:

```text
POST https://graph.facebook.com/v23.0/{PhoneNumberId}/messages
Authorization: Bearer TOKEN_DE_LA_CLINICA
Content-Type: application/json
```

Ejemplo de plantilla de recordatorio:

```text
Hola {{1}}, te recordamos tu cita en {{2}} el {{3}} a las {{4}}.
```

Variables:

```text
{{1}} Paciente
{{2}} Clinica
{{3}} Fecha
{{4}} Hora
```

Ejemplo conceptual de JSON:

```json
{
  "messaging_product": "whatsapp",
  "to": "18095551234",
  "type": "template",
  "template": {
    "name": "recordatorio_cita",
    "language": {
      "code": "es"
    },
    "components": [
      {
        "type": "body",
        "parameters": [
          { "type": "text", "text": "Juan Perez" },
          { "type": "text", "text": "Clinica Dental Sonrisa" },
          { "type": "text", "text": "16/05/2026" },
          { "type": "text", "text": "10:00 AM" }
        ]
      }
    ]
  }
}
```

## Fase 5: Entrada De Mensajes

Crear webhook unico:

```text
GET /api/webhooks/whatsapp/meta
POST /api/webhooks/whatsapp/meta
```

GET:

- Verifica el webhook con Meta usando verify token.

POST:

- Recibe JSON de mensajes entrantes.
- Recibe estados de mensajes: enviado, entregado, leido, fallido.

Logica de entrada:

```text
Leer metadata.phone_number_id
Buscar WhatsAppCuentaClinica por PhoneNumberId
Obtener ClinicaId
Buscar paciente por telefono dentro de esa clinica
Crear conversacion si no existe
Guardar mensaje entrante
Marcar conversacion con mensaje sin leer
```

No se redirige directamente al usuario. Se guarda por clinica y luego el inbox lo muestra filtrado por `ClinicaId`.

## Fase 6: Inbox En Odontari

Crear modulo en area `Clinica`:

```text
WhatsApp / Mensajes
```

Pantallas:

- Lista de conversaciones.
- Chat con paciente.
- Enviar mensaje.
- Ver historial.
- Abrir expediente.
- Ver cita proxima.

Permisos recomendados:

```text
AdminClinica
Recepcion
```

Opcional:

```text
Doctor solo lectura o limitado a sus pacientes/citas.
```

Agregar modulo bloqueable en `VistasClinica`:

```text
WhatsApp / Mensajeria
```

Usar `Plan.PermiteWhatsApp` para habilitar o bloquear el modulo por plan.

## Fase 7: Recordatorios Automaticos

Crear job programado.

Recomendacion para Odontari:

```text
Hangfire con SQL Server
```

Alternativas:

```text
Azure Function Timer
BackgroundService dentro de Odontari.Web
```

Logica del job:

```text
Cada 5 o 10 minutos
Buscar citas proximas
Ejemplo: 24 horas antes
Verificar estado != Cancelada
Verificar paciente tiene telefono
Verificar clinica tiene WhatsApp activo
Verificar Plan.PermiteWhatsApp
Verificar que no exista recordatorio enviado
Enviar plantilla
Guardar WhatsAppRecordatorioCita
Guardar WhatsAppMensaje
```

Criterio ejemplo:

```text
ahoraRD = DateTime.UtcNow.AddHours(-4)
objetivo = ahoraRD.AddHours(24)
buscar citas entre objetivo - 5 min y objetivo + 5 min
```

Primer recordatorio:

```text
24 horas antes de la cita
```

Opcional despues:

```text
2 horas antes de la cita
```

## Fase 8: Respuestas Del Paciente

Cuando el paciente responde:

```text
SI / Confirmar
```

Odontari puede:

- Marcar cita como `Confirmada`.
- Guardar mensaje entrante.
- Crear evento o nota opcional.

Cuando responde:

```text
NO / Cancelar
```

Odontari puede:

- Crear alerta para recepcion.
- Registrar la respuesta.
- No cancelar automaticamente al principio.

Recomendacion:

```text
Primero registrar y notificar; despues automatizar cambios de estado.
```

## Fase 9: Embedded Signup

Despues del MVP manual, reducir friccion para las clinicas.

Boton:

```text
Conectar WhatsApp Business
```

Flujo:

```text
AdminClinica inicia sesion en Meta
Selecciona/crea Business
Selecciona/crea WABA
Agrega numero
Verifica codigo
Autoriza Odontari
Odontari guarda WABA ID / PhoneNumberId / token
```

Esto evita configurar cliente por cliente manualmente a largo plazo.

## Fase 10: Auditoria, Seguridad Y Operacion

Necesario por ser SaaS multi-tenant activo:

- Cifrar tokens/API keys.
- No mostrar tokens en UI.
- Validar firma de webhook.
- Filtrar todo por `ClinicaId`.
- Registrar errores de envio.
- Evitar duplicados.
- Respetar ventana de atencion de WhatsApp.
- Usar plantillas aprobadas para mensajes iniciados por clinica.
- Guardar logs de envio y recepcion.

Normalizar telefonos:

```text
809xxxxxxx -> 1809xxxxxxx
829xxxxxxx -> 1829xxxxxxx
849xxxxxxx -> 1849xxxxxxx
```

Estados visibles:

```text
Pendiente
En revision
Activo
Error
Suspendido
```

## Fase 11: Operacion Comercial

Oferta recomendada:

- WhatsApp incluido solo en planes superiores.
- O add-on mensual por clinica.
- Cobrar extra por numero conectado.
- Cobrar consumo de mensajes o incluir bolsa mensual.

## Orden Recomendado De Implementacion

1. Crear configuracion Meta App central.
2. Configurar una clinica piloto manualmente.
3. Crear base de datos multi-clinica.
4. Crear servicio de envio saliente.
5. Probar envio manual de plantilla.
6. Crear webhook entrante.
7. Crear inbox basico.
8. Crear recordatorio 24h.
9. Procesar respuestas simples del paciente.
10. Agregar Embedded Signup.
11. Mejorar metricas, auditoria y administracion comercial.

## Resumen Final

La arquitectura correcta para Odontari es:

```text
Una integracion central de Odontari con Meta Cloud API
Muchas cuentas/numeros WhatsApp por clinica
Webhook unico para entrada
PhoneNumberId/token por clinica para salida
Inbox filtrado por ClinicaId
Recordatorios automaticos por job
```

Recomendacion:

```text
Empezar con MVP manual controlado para 1 o 2 clinicas.
Validar envio, recepcion, inbox y recordatorios.
Luego automatizar onboarding con Embedded Signup.
```
