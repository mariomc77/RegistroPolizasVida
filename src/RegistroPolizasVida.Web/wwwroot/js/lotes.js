// Lógica del tablero: subir un .ZIP, listar lotes recientes y mostrar su estado
// en tiempo real usando SignalR (con sondeo periódico como respaldo, por si el
// navegador no logra mantener la conexión en vivo).

const cuerpoTabla = document.getElementById("cuerpoTablaLotes");
const detalleLote = document.getElementById("detalleLote");
const formCarga = document.getElementById("formCarga");
const estadoSubida = document.getElementById("estadoSubida");
const btnSubir = document.getElementById("btnSubir");
const btnRefrescar = document.getElementById("btnRefrescar");

const lotesConocidos = new Map(); // id -> objeto lote (resumen más reciente)

function etiquetaEstado(estado) {
    const clases = {
        Pendiente: "secondary",
        Procesando: "info",
        Completado: "success",
        CompletadoConErrores: "warning",
        Fallido: "danger",
    };
    const clase = clases[estado] || "secondary";
    return `<span class="badge text-bg-${clase}">${estado}</span>`;
}

function formatearDuracion(segundos) {
    if (segundos === null || segundos === undefined) return "—";
    return `${segundos.toFixed(1)} s`;
}

function pintarTabla() {
    const lotes = [...lotesConocidos.values()].sort((a, b) => b.fechaOrden - a.fechaOrden);
    if (lotes.length === 0) {
        cuerpoTabla.innerHTML = `<tr><td colspan="5" class="text-muted">Aún no se han subido lotes.</td></tr>`;
        return;
    }
    cuerpoTabla.innerHTML = lotes
        .map(
            (l) => `
        <tr style="cursor:pointer" onclick="verDetalle('${l.id}')">
            <td>${l.nombreArchivoZip}</td>
            <td>${etiquetaEstado(l.estado)}</td>
            <td>${l.archivosProcesados ?? 0}/${l.totalArchivos ?? 0}</td>
            <td>${l.polizasInsertadas ?? 0} nuevas · ${l.polizasActualizadas ?? 0} act. · ${l.polizasConError ?? 0} error</td>
            <td>${formatearDuracion(l.duracionSegundos)}</td>
        </tr>`
        )
        .join("");
}

function actualizarLoteEnMemoria(payload) {
    lotesConocidos.set(payload.id, { ...payload, fechaOrden: Date.now() });
    pintarTabla();
}

async function cargarListaInicial() {
    const respuesta = await fetch("/api/lotes?cantidad=20");
    if (!respuesta.ok) return;
    const lotes = await respuesta.json();
    for (const l of lotes) {
        lotesConocidos.set(l.id, {
            id: l.id,
            nombreArchivoZip: l.nombreArchivoZip,
            estado: l.estado,
            totalArchivos: l.totalArchivos,
            archivosProcesados: l.archivosProcesados,
            polizasInsertadas: l.polizasInsertadas,
            polizasActualizadas: l.polizasActualizadas,
            polizasConError: l.polizasConError,
            duracionSegundos: l.duracionSegundos,
            fechaOrden: new Date(l.fechaCarga).getTime(),
        });
        suscribirseALote(l.id);
    }
    pintarTabla();
}

window.verDetalle = async function (id) {
    const respuesta = await fetch(`/api/lotes/${id}`);
    if (!respuesta.ok) return;
    const lote = await respuesta.json();

    const filasArchivos = (lote.archivos || [])
        .map((a) => {
            const errores = (a.errores || [])
                .slice(0, 15)
                .map((e) => `<li><span class="badge text-bg-light border">${e.tipo}</span> ${e.mensaje}${e.numeroPoliza ? ` <em>(póliza ${e.numeroPoliza})</em>` : ""}</li>`)
                .join("");
            return `
            <tr>
                <td>${a.nombreArchivo}</td>
                <td>${etiquetaEstado(a.estado)}</td>
                <td>${a.cantidadPolizas}</td>
                <td>${a.polizasInsertadas}</td>
                <td>${a.polizasActualizadas}</td>
                <td>${a.polizasConError}</td>
            </tr>
            ${errores ? `<tr><td colspan="6"><ul class="small text-muted mb-2">${errores}</ul></td></tr>` : ""}`;
        })
        .join("");

    detalleLote.innerHTML = `
        <div class="card mt-3 shadow-sm">
            <div class="card-body">
                <h3 class="h6">Detalle: ${lote.nombreArchivoZip} ${etiquetaEstado(lote.estado)}</h3>
                <div class="table-responsive">
                    <table class="table table-sm">
                        <thead>
                            <tr><th>Archivo XML</th><th>Estado</th><th>Pólizas</th><th>Nuevas</th><th>Actualizadas</th><th>Con error</th></tr>
                        </thead>
                        <tbody>${filasArchivos || `<tr><td colspan="6" class="text-muted">Sin archivos procesados todavía.</td></tr>`}</tbody>
                    </table>
                </div>
            </div>
        </div>`;
};

// --- SignalR: notificación en tiempo real de inicio/progreso/fin de cada lote ---
let conexion = null;
const gruposSuscritos = new Set();

async function iniciarSignalR() {
    conexion = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/procesamiento")
        .withAutomaticReconnect()
        .build();

    conexion.on("LoteIniciado", actualizarLoteEnMemoria);
    conexion.on("LoteProgreso", actualizarLoteEnMemoria);
    conexion.on("LoteFinalizado", (payload) => {
        actualizarLoteEnMemoria(payload);
        if (payload.id === ultimoLoteSubidoId) {
            estadoSubida.textContent = `Lote finalizado: ${payload.estado}.`;
        }
    });

    try {
        await conexion.start();
        // Re-suscribirse a los lotes que ya conocíamos antes de conectar.
        for (const id of gruposSuscritos) await conexion.invoke("SuscribirseALote", id);
    } catch (err) {
        console.warn("No se pudo establecer la conexión en tiempo real (SignalR); el tablero seguirá funcionando con actualización manual.", err);
    }
}

async function suscribirseALote(id) {
    gruposSuscritos.add(id);
    if (conexion && conexion.state === signalR.HubConnectionState.Connected) {
        await conexion.invoke("SuscribirseALote", id);
    }
}

// --- Subida del .ZIP ---
let ultimoLoteSubidoId = null;

formCarga.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    const input = document.getElementById("archivoZip");
    if (!input.files.length) return;

    btnSubir.disabled = true;
    estadoSubida.textContent = "Subiendo…";

    const datos = new FormData();
    datos.append("archivo", input.files[0]);

    try {
        const respuesta = await fetch("/api/lotes", { method: "POST", body: datos });
        if (!respuesta.ok) {
            const error = await respuesta.json().catch(() => ({ mensaje: "Error al subir el archivo." }));
            estadoSubida.textContent = error.mensaje;
            return;
        }
        const resultado = await respuesta.json();
        ultimoLoteSubidoId = resultado.id;
        estadoSubida.textContent = "Encolado. Procesando en segundo plano…";

        lotesConocidos.set(resultado.id, {
            id: resultado.id,
            nombreArchivoZip: resultado.nombreArchivoZip,
            estado: resultado.estado,
            totalArchivos: 0,
            archivosProcesados: 0,
            polizasInsertadas: 0,
            polizasActualizadas: 0,
            polizasConError: 0,
            duracionSegundos: null,
            fechaOrden: Date.now(),
        });
        pintarTabla();
        await suscribirseALote(resultado.id);
        formCarga.reset();
    } catch (err) {
        estadoSubida.textContent = "Error de red al subir el archivo.";
        console.error(err);
    } finally {
        btnSubir.disabled = false;
    }
});

btnRefrescar.addEventListener("click", cargarListaInicial);

// --- Arranque ---
cargarListaInicial();
iniciarSignalR();