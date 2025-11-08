# API de Municipalidad - Guía de Consumo

La API expone los siguientes recursos principales:

## Autenticación

La API utiliza JWT Bearer. Solicite un token al equipo de infraestructura y envíelo en la cabecera `Authorization: Bearer <token>`.

## Vehículos

- `GET /api/vehicles` Obtiene el catálogo de vehículos registrados.
- `POST /api/vehicles/sync/{patente}` Sincroniza un vehículo con el padrón de la DNRPA.

## Impuestos

- `POST /api/taxes/assess` Calcula el tributo mensual de un vehículo. Cuerpo:

```json
{
  "vehicleId": "<guid>",
  "year": 2024,
  "month": 5
}
```

- `GET /api/taxes/vehicle/{vehicleId}` Lista tributos por vehículo.

## Facturación

- `POST /api/invoices/generate/{tributoId}` Genera y autoriza una factura en ARCA.
- `GET /api/invoices/{id}` Detalle de factura.

## Pagos

- `POST /api/payments/prepare/{invoiceId}` Genera medios de pago (Pago Fácil y MercadoPago).
- `POST /api/payments/mercadopago/callback` Callback utilizado por MercadoPago.

Consulte el swagger en `/swagger` para ejemplos adicionales.
