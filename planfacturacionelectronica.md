# planfacturacionelectronica.md

## Objetivo

Extender la facturacion actual de Odontari para soportar **Facturacion Electronica Dominicana e-CF DGII**, sin romper el flujo existente de factura interna y NCF tradicional.

Odontari actualmente maneja facturacion interna/fiscal con NCF, PDF y bitacora. Para e-CF falta crear una capa electronica formal: XML oficial, firma digital, envio/consulta DGII o proveedor certificado, estados, QR, almacenamiento legal y trazabilidad.

## Diagnostico Actual

Odontari ya tiene:

- Datos fiscales por clinica:
  - `RNC`
  - `RazonSocial`
  - `NombreComercial`
  - `DireccionFiscal`
  - `Telefono`
  - `Email`
  - `ItbisTasa`
  - formas de pago
  - mensajes/condiciones de factura
- Modo de facturacion:
  - `Interna`
  - `Fiscal`
- Catalogo de tipos NCF:
  - `NCFTipo`
  - codigos tipo `B01`, `B02`, `B14`, `E31`, etc.
- Rangos NCF por clinica:
  - `NCFRango`
  - `Desde`
  - `Hasta`
  - `Proximo`
  - `Estado`
- Asignacion concurrente segura de NCF:
  - `FacturaService` usa transaccion `Serializable`.
  - Evita duplicar `NumeroInterno` o `NCF` ante pagos simultaneos.
- Factura PDF:
  - `FacturaPdfService`.
  - Muestra datos fiscales, NCF, cliente, lineas, subtotal, ITBIS y total.
- Bitacora fiscal:
  - `NCFMovimiento`.
- Flujo actual:
  - Al registrar el primer pago en `CajaController.Cobrar`, se crea factura si no existe.

Diagnostico:

```text
Odontari tiene facturacion fiscal interna con NCF tradicional.
Odontari todavia no tiene facturacion electronica e-CF DGII end-to-end.
```

## Decision Recomendada

No reemplazar la facturacion actual. Agregar un tercer modo:

```text
Interna
FiscalNCF
ElectronicaECF
```

La idea es mantener:

```text
Facturacion interna / NCF tradicional
```

y agregar:

```text
Facturacion electronica e-CF
```

como modulo nuevo encima del flujo existente.

## Rutas Posibles

### Ruta A: Proveedor API e-CF certificado

Odontari envia los datos de la factura a un proveedor certificado/API intermedia.

El proveedor se encarga de:

- Generar XML e-CF.
- Firmar digitalmente.
- Enviar a DGII.
- Consultar resultado.
- Devolver e-NCF, QR, XML, PDF o estado.

Ventajas:

- Implementacion mas rapida.
- Menos riesgo tecnico/legal.
- Menos carga de certificacion directa.
- Mejor para comercializar pronto.

Desventajas:

- Dependencia del proveedor.
- Costo por factura o mensualidad.
- Menos control tecnico.

Recomendacion comercial:

```text
Usar Ruta A primero para salir al mercado mas rapido.
```

### Ruta B: DGII directo

Odontari implementa todo el ciclo e-CF directamente contra DGII.

Requiere:

- XML oficial e-CF.
- XSD/documentacion vigente.
- Firma digital con certificado.
- Web services DGII.
- Consulta TrackID/estado.
- Manejo de rechazos.
- Certificacion DGII.
- Contingencia.

Ventajas:

- Mayor control.
- Menos dependencia a largo plazo.
- Mejor margen si hay mucho volumen.

Desventajas:

- Mayor complejidad.
- Mayor riesgo.
- Mayor tiempo de implementacion/certificacion.

Recomendacion:

```text
Considerar Ruta B despues de validar demanda y volumen.
```

## Lo Que Falta Para e-CF

### 1. Modelo e-CF

Crear tablas/modelos para guardar informacion electronica:

```text
FacturaElectronica
- Id
- ClinicaId
- FacturaId
- Ambiente: Certificacion/Produccion
- TipoECF
- ENCF
- TrackIdDGII
- EstadoDGII
- FechaEnvioDGII
- FechaAceptacionDGII
- XMLGeneradoUrl
- XMLFirmadoUrl
- PdfUrl
- CodigoQR
- UrlConsultaDGII
- MensajeRespuestaDGII
- ErrorCodigo
- ErrorDetalle
- CreadoAt
- ActualizadoAt
```

```text
FacturaElectronicaEvento
- Id
- ClinicaId
- FacturaElectronicaId
- TipoEvento
- EstadoAnterior
- EstadoNuevo
- Mensaje
- PayloadJson
- FechaHora
- UsuarioId
```

### 2. Configuracion e-CF Por Clinica

Crear configuracion:

```text
ConfiguracionFacturaElectronica
- Id
- ClinicaId
- Ambiente
- RNCEmisor
- RazonSocial
- CertificadoUrl o certificado cifrado
- CertificadoPasswordCifrado
- Proveedor
- ApiBaseUrl
- ApiTokenCifrado
- EstadoConexion
- FechaUltimaPrueba
- Activo
```

Campos visibles:

```text
Modo e-CF: Activo/Inactivo
Ambiente: Certificacion/Produccion
Proveedor
Estado conexion
RNC emisor
Certificado configurado: Si/No
Ultima prueba
```

### 3. Generacion XML e-CF

Si se usa DGII directo:

- Mapear `Factura` actual al XML oficial.
- Validar contra XSD.
- Manejar campos obligatorios por tipo de e-CF.
- Generar XML antes de firma.

Si se usa proveedor:

- Mapear `Factura` actual al JSON/request requerido por proveedor.
- Guardar payload enviado y respuesta recibida.

### 4. Firma Digital

Si se usa DGII directo:

- Firmar XML con certificado `.p12`/`.pfx`.
- Cifrar clave del certificado.
- Guardar XML firmado.

Si se usa proveedor:

- Puede que el proveedor firme.
- Odontari puede solo enviar certificado o delegacion segun proveedor.

### 5. Envio Y Consulta

Crear servicios:

```text
IFacturacionElectronicaProvider
IFacturacionElectronicaService
```

Implementaciones:

```text
MockFacturacionElectronicaProvider
ProveedorApiFacturacionElectronicaProvider
DgiiDirectFacturacionElectronicaProvider
```

Responsabilidades:

- Emitir e-CF.
- Consultar estado por TrackID/e-NCF.
- Guardar respuesta.
- Actualizar estado.
- Reintentar en errores transitorios.
- Registrar eventos.

### 6. Estados e-CF

Estados recomendados:

```text
Borrador
PendienteGeneracion
XMLGenerado
Firmado
Enviado
Aceptado
AceptadoCondicional
Rechazado
Error
PendienteReintento
Anulado
Contingencia
```

### 7. Tipos Documento e-CF

Mapear tipos electronicos:

```text
31 Factura Credito Fiscal Electronica
32 Factura Consumo Electronica
33 Nota Debito Electronica
34 Nota Credito Electronica
43 Gastos Menores Electronico
44 Regimenes Especiales Electronico
45 Gubernamental Electronico
46 Exportaciones Electronico
47 Pagos al Exterior Electronico
```

Para Odontari inicialmente:

```text
31 Factura Credito Fiscal Electronica
32 Factura Consumo Electronica
34 Nota Credito Electronica, si luego se agregan anulaciones/devoluciones
```

### 8. Detalle Fiscal Mas Completo

La factura actual tiene lineas simples. e-CF puede requerir mas informacion:

```text
Tipo ingreso
Tipo pago
Fecha vencimiento
Identificacion comprador
Razon social comprador
Indicador facturacion
Impuestos por linea
Descuentos
Recargos
Totales detallados
Retenciones, si aplica
```

Habra que extender `Paciente` o crear datos fiscales del cliente:

```text
ClienteRNC
ClienteRazonSocial
TipoIdentificacion
```

### 9. Almacenamiento Legal

Guardar:

```text
XML generado
XML firmado
Respuesta DGII/proveedor
Representacion impresa PDF
QR
Historial de estados
```

Preferible en Azure Blob con referencias en DB.

### 10. Ambientes

Soportar por clinica:

```text
Certificacion
Produccion
```

No mezclar documentos de certificacion con documentos reales.

### 11. Contingencia Y Reintentos

Manejo de casos:

```text
DGII/proveedor no responde
Token vencido
XML rechazado
Certificado vencido
Timeout
Error temporal
Factura aceptada despues de consulta tardia
```

El sistema debe registrar eventos y permitir reintentos controlados.

## Integracion Con Flujo Actual

Flujo actual:

```text
Pago -> OrdenCobro -> FacturaService.CrearFacturaSiNoExisteAsync -> Factura PDF/NCF
```

Flujo futuro e-CF:

```text
Pago
-> OrdenCobro
-> FacturaService.CrearFacturaSiNoExisteAsync
-> Factura creada
-> FacturacionElectronicaService.EmitirAsync(facturaId)
-> Generar payload/XML
-> Firmar o enviar a proveedor
-> Enviar/consultar DGII
-> Guardar FacturaElectronica
-> PDF/QR/XML disponibles
```

Importante:

```text
No bloquear el registro del pago si DGII esta temporalmente caida.
Marcar la factura electronica como pendiente/reintento si corresponde.
```

## Fases De Implementacion

### Fase 1: Diseno y base de datos

- Agregar modo `ElectronicaECF`.
- Crear modelos `FacturaElectronica`, `FacturaElectronicaEvento`, configuracion e-CF.
- Crear enums de estado/ambiente/tipo.
- Migracion EF.

### Fase 2: Provider abstracto y mock

- Crear `IFacturacionElectronicaProvider`.
- Crear `IFacturacionElectronicaService`.
- Implementar provider mock para probar el flujo sin DGII/proveedor real.
- Guardar eventos y estados.

### Fase 3: Pantallas de configuracion

- Pantalla por clinica para e-CF.
- Estado conexion.
- Ambiente certificacion/produccion.
- Credenciales/proveedor.
- Prueba de conexion.

### Fase 4: Emision e-CF inicial

- Integrar emision despues de crear `Factura`.
- Crear registro `FacturaElectronica`.
- Guardar estado.
- Exponer PDF/XML/QR si existen.

### Fase 5: Integracion proveedor API

- Elegir proveedor certificado.
- Mapear request/response.
- Manejar errores.
- Descargar/guardar XML/PDF/QR.
- Consultar estado.

### Fase 6: Bitacora y soporte

- Pantalla de documentos e-CF.
- Filtros por estado, fecha, paciente, e-NCF.
- Ver eventos.
- Reintentar emision/consulta.

### Fase 7: Certificacion y piloto

- Probar con una clinica piloto.
- Certificacion/ambiente prueba.
- Validar documentos aceptados/rechazados.
- Ajustar campos.

### Fase 8: Produccion controlada

- Activar para pocas clinicas.
- Monitorear errores.
- Crear alertas.
- Documentar onboarding.

### Fase 9: DGII directo opcional

- Si el volumen justifica, implementar provider DGII directo.
- Reusar la misma interfaz `IFacturacionElectronicaProvider`.

## Informacion Necesaria Para Empezar Ruta A

Para proveedor API e-CF:

```text
Documentacion API del proveedor
Credenciales sandbox
Ejemplos request/response
Tipos de comprobante soportados
Forma de autenticacion
Como envian QR/XML/PDF
Como consultan estado
Manejo de errores
Precios
```

## Informacion Necesaria Para Empezar Ruta B

Para DGII directo:

```text
Documentacion tecnica oficial vigente
XSD/XML oficial
Credenciales ambiente certificacion
Certificado digital de prueba
URLs de web services
Proceso de certificacion
Ejemplos XML aceptados/rechazados
Reglas de contingencia
```

## Recomendacion Final

Para Odontari comercial:

```text
Empezar con Ruta A: proveedor API e-CF certificado.
Construir la arquitectura con interfaz de provider.
No acoplar el sistema a un proveedor especifico.
Mantener abierta la posibilidad de DGII directo en el futuro.
```

Motivo:

```text
Menos riesgo.
Menor tiempo de salida.
Mas facil de vender.
Mejor para pilotos reales.
```
