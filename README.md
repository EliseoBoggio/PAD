📘Municipalidad – Sistema de Impuesto Vehicular

Este proyecto implementa un sistema municipal de gestión y cobro de impuestos vehiculares con:

Integración con DNRPA (Altas y Transferencias)

Generación automática de obligaciones tributarias

Emisión de facturas con código de barras Pago Fácil (42 dígitos)

Pago electrónico mediante Mercado Pago

Procesamiento de archivos de rendición Pago Fácil

Frontend simple en HTML/JS para consulta y pago

2. Mercado Pago

Configurar el token y URLs del webhook en:
-ngrok http 5214

appsettings.Development.json:

"MercadoPago": {
  "AccessToken": "APP_USR-...",
  "NotificationUrl": "https://tunnel-ngrok/api/v1/mercadopago/webhook",
  "BackUrls": {
    "Success": "https://tunnel-ngrok/api/v1/mercadopago/success",
    "Failure": "https://tunnel-ngrok/api/v1/mercadopago/success",
    "Pending": "https://tunnel-ngrok/api/v1/mercadopago/success"
  }
}

▶️ Cómo ejecutar el proyecto
1. Backend
dotnet run --project Muni.Api

2. Iniciar el túnel para MP
ngrok http 5214

3. Abrir interfaz web (frontend)
https://localhost:7149/

🧠 Funcionalidad principal

Importación de transacciones DNRPA y actualización de titulares.

Generación automática de obligaciones mensuales.

Emisión de facturas electrónicas.

Generación de PDF con código de barras Pago Fácil.

Pagos con Mercado Pago (checkout + webhook).

Acreditación automática de pagos de Pago Fácil mediante archivo PFddmma.9999.

Historial de pagos y deudas.
