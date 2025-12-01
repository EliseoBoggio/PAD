// Config básico: si la API vive en el mismo host, usamos rutas relativas
const API_BASE = "";

// Estado simple en memoria
let selectedOwner = null;      // { ownerId, nombre, ... }
let selectedVehicle = null;    // { vehicleId, patente, ... }
let currentInvoiceId = null;

// Helpers DOM
const $ = (sel) => document.querySelector(sel);
const $$ = (sel) => Array.from(document.querySelectorAll(sel));

function setSection(id) {
    $$(".section").forEach(s => s.classList.remove("section--active"));
    $(`#${id}`).classList.add("section--active");

    $$(".nav-btn").forEach(btn => {
        btn.classList.toggle("nav-btn--active", btn.dataset.section === id);
    });
}

function setMessage(el, text, type) {
    el.textContent = text || "";
    el.classList.remove("messages--error", "messages--ok");
    if (!text) return;
    if (type === "error") el.classList.add("messages--error");
    if (type === "ok") el.classList.add("messages--ok");
}

// ---------------- Navegación ----------------

document.addEventListener("DOMContentLoaded", () => {
    // Navbar
    $$(".nav-btn").forEach(btn => {
        btn.addEventListener("click", () => setSection(btn.dataset.section));
    });

    $("#go-to-consulta").addEventListener("click", () => setSection("consulta"));

    $("#form-rango").addEventListener("submit", e => {
        e.preventDefault();
        importarPorRango();
    });

    // Búsqueda Owners
    $("#owner-search-form").addEventListener("submit", onOwnerSearch);
    $("#owner-search-clear").addEventListener("click", () => {
        $("#owner-search-input").value = "";
        loadOwners();
    });

    // Emitir factura
    $("#emit-form").addEventListener("submit", onEmitSubmit);

    // Acciones de pago
    $("#mp-pay-btn").addEventListener("click", onMpPay);
    $("#pf-pdf-btn").addEventListener("click", onPfPdf);

    // Historial
    $("#hist-load-btn").addEventListener("click", loadHistorial);

    // Set periodo default (mes actual)
    const now = new Date();
    const y = now.getFullYear();
    const m = String(now.getMonth() + 1).padStart(2, "0");
    $("#emit-periodo").value = `${y}${m}`;

    // Cargar owners por defecto
    loadOwners();
});

function renderOwnersList(owners) {
    const listEl = $("#owner-list");
    listEl.innerHTML = "";

    if (!owners.length) {
        listEl.innerHTML = "<div class='owner-item'><span class='muted'>No se encontraron titulares.</span></div>";
        return;
    }

    owners.forEach(o => {
        const item = document.createElement("div");
        item.className = "owner-item";

        const header = document.createElement("div");
        header.className = "owner-header";

        const info = document.createElement("div");
        info.innerHTML = `
            <div class="owner-name">${o.nombre}</div>
            <div class="owner-meta">CUIT/CUIL: ${o.cuitCuil} · ${o.domicilio || "Sin domicilio"}</div>
        `;

        const btn = document.createElement("button");
        btn.className = "btn-secondary";
        btn.textContent = "Seleccionar";
        btn.addEventListener("click", () => onSelectOwner(o));

        header.appendChild(info);
        header.appendChild(btn);
        item.appendChild(header);

        const vehList = document.createElement("div");
        vehList.className = "vehicle-list";

        if (o.vehicles && o.vehicles.length) {
            o.vehicles.forEach(v => {
                const pill = document.createElement("button");
                pill.className = "vehicle-pill";

                const estado = v.esTitularActual ? "Titular actual" : "Titular anterior";
                const estadoClass = v.esTitularActual ? "badge-actual" : "badge-anterior";

                pill.innerHTML = `
            <span>${v.patente} · ${v.marca || ""} ${v.modelo || ""}</span>
            <span class="${estadoClass}" style="margin-left:0.5rem">${estado}</span>
        `;

                pill.addEventListener("click", () => onSelectOwnerVehicle(o, v));
                vehList.appendChild(pill);
            });
        } else {
            vehList.innerHTML = "<span class='muted'>Sin vehículos asociados.</span>";
        }


        item.appendChild(vehList);
        listEl.appendChild(item);
    });
}

// ---------------- Owners: búsqueda y selección ----------------

async function loadOwners() {
    const q = $("#owner-search-input").value.trim();
    const listEl = $("#owner-list");

    if (!q) {
        // Si no hay DNI, mostrás la lista local (como antes)
        const res = await fetch("/api/v1/owners");
        const owners = await res.json();
        renderOwnersList(owners);
        return;
    }

    // Si se ingresó un DNI, buscamos en DNRPA + importamos
    listEl.innerHTML = "<div class='owner-item'><span class='muted'>Buscando en DNRPA...</span></div>";

    try {
        const resp = await fetch(`/api/v1/dnrpa/buscar-por-dni?dni=${encodeURIComponent(q)}`);

        if (!resp.ok) {
            listEl.innerHTML = `<div class='owner-item'><span class='messages messages--error'>DNI no encontrado en DNRPA.</span></div>`;
            return;
        }

        const data = await resp.json();
        // data = { ownerId, nombre, domicilio, vehiculos: [...] }

        const owner = {
            ownerId: data.ownerId,
            nombre: data.nombre,
            cuitCuil: "(sin CUIT/CUIL en DNRPA)",
            domicilio: data.domicilio || "",
            vehicles: data.vehiculos
        };

        renderOwnersList([owner]);

    } catch (err) {
        console.error(err);
        listEl.innerHTML = `<div class='owner-item'><span class='messages messages--error'>Error al consultar DNRPA.</span></div>`;
    }
}

function onOwnerSearch(e) {
    e.preventDefault();
    loadOwners();
}

function onSelectOwner(o) {
    selectedOwner = o;
    // si tiene al menos 1 vehículo, usamos el primero por defecto
    selectedVehicle = o.vehicles && o.vehicles.length ? o.vehicles[0] : null;
    currentInvoiceId = null;
    renderSelection();
}

function onSelectOwnerVehicle(o, v) {
    selectedOwner = o;
    selectedVehicle = v;
    currentInvoiceId = null;
    renderSelection();
}

function renderSelection() {
    const box = $("#selected-owner-box");
    const emitBtn = $("#emit-btn");
    const msgEl = $("#consulta-messages");
    const invSummary = $("#invoice-summary");

    setMessage(msgEl, "");
    currentInvoiceId = null;
    invSummary.className = "info-box info-box--empty";
    invSummary.innerHTML = "<p class='muted'>No hay factura emitida para el período seleccionado.</p>";

    if (!selectedOwner) {
        box.className = "info-box info-box--empty";
        box.innerHTML = "<p class='muted'>Ningún titular seleccionado.</p>";
        emitBtn.disabled = true;
        $("#mp-pay-btn").disabled = true;
        $("#pf-pdf-btn").disabled = true;
        return;
    }

    box.className = "info-box info-box--highlight";
    box.innerHTML = `
    <div><strong>${selectedOwner.nombre}</strong></div>
    <div class="muted">CUIT/CUIL: ${selectedOwner.cuitCuil} · ${selectedOwner.domicilio || "Sin domicilio"}</div>
    ${selectedVehicle
            ? `<div style="margin-top:0.3rem;font-size:0.85rem">
             Vehículo seleccionado:
             <span class="tag">${selectedVehicle.patente}</span>
             ${selectedVehicle.marca || ""} ${selectedVehicle.modelo || ""} · ${selectedVehicle.anio || ""}<br/>
             <span class="${selectedVehicle.esTitularActual ? "badge-actual" : "badge-anterior"}">
               ${selectedVehicle.esTitularActual ? "Titular actual" : "Titular anterior"}
             </span>
           </div>`
            : `<div class="muted" style="margin-top:0.3rem">No hay vehículo seleccionado.</div>`
        }
  `;

    emitBtn.disabled = !selectedVehicle;
    $("#mp-pay-btn").disabled = true;
    $("#pf-pdf-btn").disabled = true;
}

// ---------------- Emitir factura y preparar pago ----------------

async function onEmitSubmit(e) {
    e.preventDefault();
    const msgEl = $("#consulta-messages");

    if (!selectedOwner || !selectedVehicle) {
        setMessage(msgEl, "Seleccioná un titular y un vehículo primero.", "error");
        return;
    }

    const periodo = $("#emit-periodo").value.trim();
    if (!/^\d{6}$/.test(periodo)) {
        setMessage(msgEl, "Período inválido. Usá formato YYYYMM.", "error");
        return;
    }

    const body = {
        identificador: selectedOwner.ownerId,       // usás OWNER_ID
        tipoIdentificador: "OWNER_ID",
        periodo,
        overwrite: false
    };

    setMessage(msgEl, "Generando obligación/factura...", "ok");

    try {
        const res = await fetch(API_BASE + "/api/v1/invoices/emitir", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });
        if (!res.ok) {
            const text = await res.text();
            throw new Error("Error al emitir factura: " + text);
        }
        const data = await res.json();
        currentInvoiceId = data.invoiceId;

        renderInvoiceSummary(data);
        setMessage(msgEl, "Factura emitida correctamente.", "ok");
        $("#mp-pay-btn").disabled = false;
        $("#pf-pdf-btn").disabled = false;
    } catch (err) {
        setMessage(msgEl, err.message, "error");
    }
}

function renderInvoiceSummary(data) {
    const box = $("#invoice-summary");
    box.className = "info-box info-box--highlight";

    box.innerHTML = `
    <div><strong>Factura generada</strong></div>
    <div class="muted">Periodo: ${data.periodo}</div>
    <div style="margin-top:0.3rem;font-size:0.9rem">
      Importe 1º venc.: <strong>$ ${data.importePrimerVenc.toFixed ? data.importePrimerVenc.toFixed(2) : data.importePrimerVenc}</strong><br/>
      1º Vencimiento: ${data.fechaPrimerVenc}<br/>
      2º Vencimiento: ${data.fechaSegundoVenc}<br/>
    </div>
    <div style="margin-top:0.35rem;font-size:0.8rem">
      Cliente14: <code>${data.cliente14}</code><br/>
      Código de barras (Pago Fácil):<br/>
      <code>${data.barcode42}</code>
    </div>
  `;
}

// ---------------- Pago con Mercado Pago ----------------

async function onMpPay() {
    const msgEl = $("#consulta-messages");
    if (!currentInvoiceId) {
        setMessage(msgEl, "No hay factura emitida para pagar.", "error");
        return;
    }

    setMessage(msgEl, "Creando preferencia de Mercado Pago...", "ok");

    try {
        const res = await fetch(API_BASE + `/api/v1/mercadopago/preferencia/${currentInvoiceId}`, {
            method: "POST"
        });
        if (!res.ok) {
            const text = await res.text();
            throw new Error("Error al crear preferencia MP: " + text);
        }
        const data = await res.json();
        if (data.initPoint) {
            // Redirigimos al checkout de MP
            window.location.href = data.initPoint;
        } else {
            throw new Error("Respuesta de MP sin initPoint.");
        }
    } catch (err) {
        setMessage(msgEl, err.message, "error");
    }
}

// ---------------- Cupón Pago Fácil (PDF) ----------------

function onPfPdf() {
    const msgEl = $("#consulta-messages");
    if (!currentInvoiceId) {
        setMessage(msgEl, "No hay factura emitida para descargar.", "error");
        return;
    }
    const url = API_BASE + `/api/v1/invoices/${currentInvoiceId}/pdf`;
    window.open(url, "_blank");
}

// ---------------- Historial de pagos ----------------

async function loadHistorial() {
    const msgEl = $("#hist-messages");
    const tbody = $("#hist-table-body");
    setMessage(msgEl, "Cargando historial...", "ok");
    tbody.innerHTML = "";

    try {
        const res = await fetch(API_BASE + "/api/v1/payments?take=100");
        if (!res.ok) throw new Error("Error al obtener historial de pagos");
        const list = await res.json();

        if (!list.length) {
            setMessage(msgEl, "No hay pagos registrados todavía.", "ok");
            return;
        }

        setMessage(msgEl, "", null);
        list.forEach(p => {
            const tr = document.createElement("tr");
            const fecha = new Date(p.fechaAcreditacion);

            tr.innerHTML = `
        <td>${fecha.toLocaleString()}</td>
        <td>${p.provider}</td>
        <td>$ ${p.monto.toFixed ? p.monto.toFixed(2) : p.monto}</td>
        <td>${p.invoicePeriodo || "-"}</td>
        <td>${p.patente || "-"}</td>
      `;
            tbody.appendChild(tr);
        });
    } catch (err) {
        setMessage(msgEl, err.message, "error");
    }
}
async function importarPorRango() {
    const desde = $("#rng-desde").value;
    const hasta = $("#rng-hasta").value;
    const out = $("#resultado-rango");

    if (!desde || !hasta) {
        out.innerHTML = `<p class="messages messages--error">Completá ambas fechas.</p>`;
        return;
    }

    out.innerHTML = `<p class="messages">Importando transacciones desde DNRPA...</p>`;

    const body = {
        desde: `${desde}T00:00:00`,
        hasta: `${hasta}T23:59:59`
    };

    try {
        const resp = await fetch("/api/v1/dnrpa/sync", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });

        if (!resp.ok) {
            out.innerHTML = `<p class="messages messages--error">Error al importar.</p>`;
            return;
        }

        const data = await resp.json();

        out.innerHTML = `
        <div class="messages messages--ok">
            <strong>Importación completada</strong><br>
            Eventos leídos: ${data.eventosLeidos}<br>
            Altas aplicadas: ${data.altasAplicadas}<br>
            Transferencias aplicadas: ${data.transferenciasAplicadas}<br>
            Owners creados: ${data.ownersCreados}<br>
            Vehículos creados: ${data.vehiculosCreados}<br>
            Historiales creados: ${data.historialesCreados}<br>
            Obligaciones creadas: ${data.obligacionesCreadas}
        </div>
        `;

        if (data.skipped?.length) {
            out.innerHTML += `
            <details style="margin-top:1rem;">
                <summary>Eventos saltados (duplicados)</summary>
                <pre>${data.skipped.join("\n")}</pre>
            </details>`;
        }
    } catch (err) {
        console.error(err);
        out.innerHTML = `<p class="messages messages--error">Error de conexión.</p>`;
    }
}

