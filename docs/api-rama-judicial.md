# API Rama Judicial — Información Recopilada

## Generalidades

- **Base URL (producción):** `https://consultaprocesos.ramajudicial.gov.co:448`
 -- **Nota Importante:** "Esta url será la que usaremos siempre en la app ya que es en la que podemos consultar el proceso por ID (23 dígitos)
						  sin embargo para obtener los departamentos y municipios y despachos es otra url, sin embargo esa no la necesitamos, nosotros nos encargaremos de consultar esa api antes para cargar esa info en la DB y no tener que ir a la api, por tanto no te pasaré informacipon de dicha api"
- **Base URL (sandbox/staging si existe): No se y no nos interesa atacamos siempre a prod , son apis públicas
- **Requiere autenticación:** "No"
- **Rate limits conocidos:** no documentado
- **Headers obligatorios:** "No lo se en la petición de la web manda varios pero uyo por postamn los deshabilité todos y envié la petición y respondió OK, igualmengte en la carpeta ligigApp están todas las request que copié de la web"
- **Formato de respuesta:** "JSON"
- **Documentación oficial (si existe):** "No encuentro algo como una wiki pero si encontré el swagger --> https://consultaprocesos.ramajudicial.gov.co:448/swagger/index.html, si no puedes leer el swagger de la web en la carpeta litigApp/consultaprocesos-ramajudicia-swagger.json "
- **Tiempos de respuesta típicos observados:** ej. "1s o menos, no se en horas pico"
- **Notas/quirks importantes:** 
	- "la api no es muy buena que digamos por lo que estuvimos probando, puede que luego de x peticiones nos bloquee, me pasó que en una ventana de chrome me saca 200 pero sin datos, y exáctamente el mismo proceso en incógnito si me trae los datos."
	- "Para recopilar cada parte de la información del proceso se debe atacar endpoints distintos, uno para la consulta incial, otro para el detalle, sujetos , documetos y actuaciones" 

---

## Endpoint 1: Consultar proceso por número de radicado - consulta incial (overview del proceso)

- **Método:** GET
- **URL:** `https://consultaprocesos.ramajudicial.gov.co:448/api/v2/Procesos/Consulta/NumeroRadicacion?numero={RadicadoId}&SoloActivos=false&pagina=1`
- ** CURL** ` curl 'https://consultaprocesos.ramajudicial.gov.co:448/api/v2/Procesos/Consulta/NumeroRadicacion?numero=17001400301020240019200&SoloActivos=false&pagina=1' \
  -H 'accept: application/json, text/plain, */*' \
  -H 'accept-language: es-ES,es;q=0.9' \
  -H 'origin: https://consultaprocesos.ramajudicial.gov.co' \
  -H 'priority: u=1, i' \
  -H 'referer: https://consultaprocesos.ramajudicial.gov.co/' \
  -H 'sec-ch-ua: "Chromium";v="148", "Google Chrome";v="148", "Not/A)Brand";v="99"' \
  -H 'sec-ch-ua-mobile: ?0' \
  -H 'sec-ch-ua-platform: "Windows"' \
  -H 'sec-fetch-dest: empty' \
  -H 'sec-fetch-mode: cors' \
  -H 'sec-fetch-site: same-site' \
  -H 'user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36'`
- **Query params:**
  | Nombre | Tipo | Obligatorio | Descripción |
  | numero | string | SI | Radicado completo (23 dígitos) |
  | SoloActivos | boolean | SI | Siempre en false, no sabemos si ya no está activo |
  | pagina | integer(32) | SI | La página a consultar, no necesitamos todo el histórico en el overview que es este endpoint, por tanto acá siempre será 1 |
- **Headers especiales:** "No"

### Respuesta exitosa (200) — Ejemplo real

```json
{
    "tipoConsulta": "NumeroRadicacion",
    "procesos": [
        {
            "idProceso": 132573703,
            "idConexion": 323,
            "llaveProceso": "17001400301020240019200",
            "fechaProceso": "2024-03-06T00:00:00",
            "fechaUltimaActuacion": "2026-03-20T00:00:00",
            "despacho": "JUZGADO 002 CIVIL MUNICIPAL DE EJECUCIÓN DE SENTENCIAS DE MANIZALES ",
            "departamento": "CALDAS",
            "sujetosProcesales": "Demandante: OSCAR ARTURO - ORTIZ HENAO | Demandado: N.N. | Demandado: N.N. | Demandado: FRANCISCA HELENA - GONZALEZ ARIAS ",
            "esPrivado": false,
            "cantFilas": -1
        }
    ],
    "parametros": {
        "numero": "17001400301020240019200",
        "nombre": null,
        "tipoPersona": null,
        "idSujeto": null,
        "ponente": null,
        "claseProceso": null,
        "codificacionDespacho": null,
        "soloActivos": false
    },
    "paginacion": {
        "cantidadRegistros": 1,
        "registrosPagina": 20,
        "cantidadPaginas": 1,
        "pagina": 1,
        "paginas": null
    }
}
```

### Respuesta cuando el proceso no existe - 200 sin error e información null

```json
{
    "tipoConsulta": "NumeroRadicacion",
    "procesos": [],
    "parametros": {
        "numero": "170014003010202400134200",
        "nombre": null,
        "tipoPersona": null,
        "idSujeto": null,
        "ponente": null,
        "claseProceso": null,
        "codificacionDespacho": null,
        "soloActivos": false
    },
    "paginacion": {
        "cantidadRegistros": 0,
        "registrosPagina": 20,
        "cantidadPaginas": 0,
        "pagina": 1,
        "paginas": null
    }
}
```

### Otros estados de error observados

- 404 cuando: El radicado no tiene los 23 dígitos --> ```{"StatusCode":404,"Message":"El parametro \"NumeroRadicacion\" ha de contener 23 digitos."}```

---

## Endpoint 2: Consultar proceso por id - Detalle del proceso


- **Método:** GET
- **URL:** `https://consultaprocesos.ramajudicial.gov.co:448/api/v2/Proceso/Detalle/{idProceso}`
- ** CURL** "igual al endpoint 1, sólo cambia el endpoint"
- **Url params:**
  | Nombre | Tipo | Obligatorio | Descripción |
  | idProceso | string | SI | Id del sisytema interno del proceso, obtenido de la llamada anterior campo idProceso |
- **Headers especiales:** "No"

### Respuesta exitosa (200) — Ejemplo real

```json
{
    "idRegProceso": 13257370,
    "llaveProceso": "17001400301020240019200",
    "idConexion": 323,
    "esPrivado": false,
    "fechaProceso": "2024-03-06T00:00:00",
    "codDespachoCompleto": "170014303002",
    "despacho": "JUZGADO 002 CIVIL MUNICIPAL DE EJECUCIÓN DE SENTENCIAS DE MANIZALES ",
    "ponente": "JUEZ JUZGADO 2 MUNICIPAL CIVIL DE EJECUCION",
    "tipoProceso": "De Ejecución",
    "claseProceso": "Ejecutivo Singular",
    "subclaseProceso": "Por sumas de dinero",
    "recurso": "Sin Tipo de Recurso",
    "ubicacion": "Otro juzgado",
    "contenidoRadicacion": "VENTANILLA VIRTUAL ID 63223",
    "fechaConsulta": "2026-05-25T20:40:41.94",
    "ultimaActualizacion": "2026-05-25T19:23:52.873"
}
```

### Respuesta cuando el proceso no existe - 404

```json
{"StatusCode":404,"Message":"No se encontro detalle para el IdProceso: 13257371"}
```

### Otros estados de error observados

- 404 cuando: El id del proceso sobrepasa el lenght permitido (9 dígitos) --> ```{"StatusCode":404,"Message":"Error converting data type numeric to int."}```
- 400 BadRequest cuando: el valor del url param NO es un número --> ``` {
    "errors": {
        "IdProceso": [
            "The value 'fdsfgtt' is not valid."
        ]
    },
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "traceId": "|87b73cf-4975829d43d81963."
} ```
---

## Endpoint 3: Consultar proceso por id - Sujetos del proceso


- **Método:** GET
- **URL:** `https://consultaprocesos.ramajudicial.gov.co:448/api/v2/Proceso/Sujetos/{idProceso}?pagina={page}`
- ** CURL** "igual al endpoint 1, sólo cambia el endpoint"
- **Url params:**
  | Nombre | Tipo | Obligatorio | Descripción |
  | idProceso | string | SI | Id del sisytema interno del proceso, obtenido de la llamada anterior campo idProceso |
  | pagina | integer | SI | página a consultar, se debe iterar por tanto sujetpos tenga el proceso |
- **Headers especiales:** "No"

### Respuesta exitosa (200) — Ejemplo real

```json
{
    "sujetos": [
        {
            "idRegSujeto": 21238205,
            "tipoSujeto": "Demandante",
            "esEmplazado": false,
            "identificacion": null,
            "nombreRazonSocial": "OSCAR ARTURO - ORTIZ HENAO",
            "cant": 4
        },
        {
            "idRegSujeto": 21238206,
            "tipoSujeto": "Demandado",
            "esEmplazado": false,
            "identificacion": null,
            "nombreRazonSocial": "FRANCISCA HELENA - GONZALEZ ARIAS",
            "cant": 4
        },
        {
            "idRegSujeto": 21258415,
            "tipoSujeto": "Demandado",
            "esEmplazado": false,
            "identificacion": null,
            "nombreRazonSocial": "N.N.",
            "cant": 4
        },
        {
            "idRegSujeto": 24190469,
            "tipoSujeto": "Demandado",
            "esEmplazado": false,
            "identificacion": null,
            "nombreRazonSocial": "N.N.",
            "cant": 4
        }
    ],
    "paginacion": {
        "cantidadRegistros": 4,
        "registrosPagina": 40,
        "cantidadPaginas": 1,
        "pagina": 1,
        "paginas": null
    }
}
```

### Respuesta cuando el proceso no existe - 500

```json
{"StatusCode":500,"Message":"Object reference not set to an instance of an object."}
```

### Otros estados de error observados

- 500 cuando: ocurre un error interno del sistema --> ```{"StatusCode":500,"Message":"Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')"}```
---

## Endpoint 4: Consultar proceso por id - Documentos del proceso


- **Método:** GET
- **URL:** `https://consultaprocesos.ramajudicial.gov.co:448/api/v2/Proceso/Documentos/{idProceso}`
- ** CURL** "igual al endpoint 1, sólo cambia el endpoint"
- **Url params:**
  | Nombre | Tipo | Obligatorio | Descripción |
  | idProceso | string | SI | Id del sisytema interno del proceso, obtenido de la llamada anterior campo idProceso |
- **Headers especiales:** "No"

### Respuesta exitosa (200) — NO SE LOGRÓ OBTENER NUNCA UNA RESPUESTA EXITOSA DE ESTE ENDPOINT, SE PUEDE OBVIAR

```json
```

### Respuesta cuando los documentos no existes - 404

```json
{"StatusCode":404,"Message":"No se encontraron documentos para el IdProceso: 63556"}
```

---

## Endpoint 5: Consultar proceso por id - Actuaciones del proceso


- **Método:** GET
- **URL:** `https://consultaprocesos.ramajudicial.gov.co:448/api/v2/Proceso/Actuaciones/{idProceso}?pagina={page}`
- ** CURL** "igual al endpoint 1, sólo cambia el endpoint"
- **Url params:**
  | Nombre | Tipo | Obligatorio | Descripción |
  | idProceso | string | SI | Id del sisytema interno del proceso, obtenido de la llamada anterior campo idProceso |
  | pagina | integer | SI | página a consultar |
- **Headers especiales:** "No"
- **Nota importante:** "No se hasta que punto debamos consultar todas las páginas, ya que por página muestyra 40 y considero es más que suficiente para el MVP"

### Respuesta exitosa (200) — Ejemplo real 1

```json
{
    "actuaciones": [
        {
            "idRegActuacion": 5923364,
            "llaveProceso": "66001233100020120021100",
            "consActuacion": 8,
            "fechaActuacion": "2013-09-17T00:00:00",
            "actuacion": "Archivo definitivo",
            "anotacion": null,
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2016-09-16T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 8
        },
        {
            "idRegActuacion": 5923354,
            "llaveProceso": "66001233100020120021100",
            "consActuacion": 7,
            "fechaActuacion": "2013-09-10T00:00:00",
            "actuacion": "Auto de sustanciación",
            "anotacion": "ESTESE A LO RESUELTO POR EL CONSEJO DE ESTADO Y LA CORTE CONSTITUCIONAL",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2013-09-10T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 8
        },
        {
            "idRegActuacion": 5923344,
            "llaveProceso": "66001233100020120021100",
            "consActuacion": 6,
            "fechaActuacion": "2012-07-26T00:00:00",
            "actuacion": "Envio Expediente Consejo de Estado",
            "anotacion": "MEDIANTE OFICIO N° 4023",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2012-07-26T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 8
        },
        {
            "idRegActuacion": 5923334,
            "llaveProceso": "66001233100020120021100",
            "consActuacion": 5,
            "fechaActuacion": "2012-06-06T00:00:00",
            "actuacion": "Auto concede impugnación tutela",
            "anotacion": null,
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2012-06-06T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 8
        },
        {
            "idRegActuacion": 5923324,
            "llaveProceso": "66001233100020120021100",
            "consActuacion": 4,
            "fechaActuacion": "2012-05-29T00:00:00",
            "actuacion": "Setencia Tutela Primera Instancia",
            "anotacion": "SE RECHAZA POR IMPROCEDENTE",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2012-05-29T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 8
        },
        {
            "idRegActuacion": 5923314,
            "llaveProceso": "66001233100020120021100",
            "consActuacion": 3,
            "fechaActuacion": "2012-05-23T00:00:00",
            "actuacion": "A despacho para sentencia",
            "anotacion": null,
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2012-05-23T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 8
        },
        {
            "idRegActuacion": 5923304,
            "llaveProceso": "66001233100020120021100",
            "consActuacion": 2,
            "fechaActuacion": "2012-05-17T00:00:00",
            "actuacion": "Auto Admite Recurso deTutela",
            "anotacion": "SE ORDENA DAR TRAMITE",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2012-05-17T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 8
        },
        {
            "idRegActuacion": 5923294,
            "llaveProceso": "66001233100020120021100",
            "consActuacion": 1,
            "fechaActuacion": "2012-05-16T00:00:00",
            "actuacion": "Reparto y Radicación",
            "anotacion": "REPARTO Y RADICACION DEL PROCESO REALIZADAS EL Miércoles, 16 de Mayo de 2012 con secuencia:  574",
            "fechaInicial": "2012-05-16T00:00:00",
            "fechaFinal": "2012-05-16T00:00:00",
            "fechaRegistro": "2012-05-16T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 8
        }
    ],
    "paginacion": {
        "cantidadRegistros": 8,
        "registrosPagina": 40,
        "cantidadPaginas": 1,
        "pagina": 1,
        "paginas": null
    }
}
```

### Respuesta exitosa (200) — Ejemplo real 2

```json
{
    "actuaciones": [
        {
            "idRegActuacion": 2308991013,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 82,
            "fechaActuacion": "2026-03-20T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2026-03-20 a las: 8:42am",
            "fechaInicial": "2026-03-24T00:00:00",
            "fechaFinal": "2026-03-24T00:00:00",
            "fechaRegistro": "2026-03-20T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2308991003,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 81,
            "fechaActuacion": "2026-03-20T00:00:00",
            "actuacion": "Auto señala honorarios",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-03-20T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2307377353,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 80,
            "fechaActuacion": "2026-03-12T00:00:00",
            "actuacion": "A Despacho",
            "anotacion": "Enviado por la Oficina de Ejecución Civil Municipal",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-03-12T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2300722143,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 79,
            "fechaActuacion": "2026-02-23T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Bancos",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-02-23T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2300169913,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 78,
            "fechaActuacion": "2026-02-19T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2026-02-19 a las: 8:54am",
            "fechaInicial": "2026-02-20T00:00:00",
            "fechaFinal": "2026-02-20T00:00:00",
            "fechaRegistro": "2026-02-19T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2300169903,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 77,
            "fechaActuacion": "2026-02-19T00:00:00",
            "actuacion": "Auto ordena correr traslado",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-02-19T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2299183153,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 76,
            "fechaActuacion": "2026-02-13T00:00:00",
            "actuacion": "A Despacho",
            "anotacion": "Enviado por la Oficina de Ejecución Civil Municipal",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-02-13T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2298217443,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 75,
            "fechaActuacion": "2026-02-09T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Acta De Entrega Secuestre",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-02-09T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2297847033,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 74,
            "fechaActuacion": "2026-02-06T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2026-02-06 a las: 8:45am",
            "fechaInicial": "2026-02-09T00:00:00",
            "fechaFinal": "2026-02-09T00:00:00",
            "fechaRegistro": "2026-02-06T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2297847023,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 73,
            "fechaActuacion": "2026-02-06T00:00:00",
            "actuacion": "Auto resuelve corrección providencia",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-02-06T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2296605793,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 72,
            "fechaActuacion": "2026-01-30T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2026-01-30 a las: 8:51am",
            "fechaInicial": "2026-02-02T00:00:00",
            "fechaFinal": "2026-02-02T00:00:00",
            "fechaRegistro": "2026-01-30T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2296605783,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 71,
            "fechaActuacion": "2026-01-30T00:00:00",
            "actuacion": "Auto termina proceso por pago",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-01-30T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2296088113,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 70,
            "fechaActuacion": "2026-01-28T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Terminaciones",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-01-28T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2295888073,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 69,
            "fechaActuacion": "2026-01-27T00:00:00",
            "actuacion": "A Despacho",
            "anotacion": "Enviado por la Oficina de Ejecución Civil Municipal",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-01-27T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2295792113,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 68,
            "fechaActuacion": "2026-01-26T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Terminaciones",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-01-26T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2295080303,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 67,
            "fechaActuacion": "2026-01-22T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Otras",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2026-01-22T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2290355933,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 66,
            "fechaActuacion": "2025-12-02T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2025-12-02 a las: 9:46am",
            "fechaInicial": "2025-12-03T00:00:00",
            "fechaFinal": "2025-12-03T00:00:00",
            "fechaRegistro": "2025-12-02T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2290355923,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 65,
            "fechaActuacion": "2025-12-02T00:00:00",
            "actuacion": "Auto resuelve aclaración providencia",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-12-02T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2289910003,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 64,
            "fechaActuacion": "2025-11-28T00:00:00",
            "actuacion": "A Despacho",
            "anotacion": "Enviado por la Oficina de Ejecución Civil Municipal",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-28T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2288690683,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 63,
            "fechaActuacion": "2025-11-21T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2025-11-21 a las: 11:54am",
            "fechaInicial": "2025-11-24T00:00:00",
            "fechaFinal": "2025-11-24T00:00:00",
            "fechaRegistro": "2025-11-21T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2288690673,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 62,
            "fechaActuacion": "2025-11-21T00:00:00",
            "actuacion": "Auto modifica liquidacion presentada",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-21T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2288690663,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 61,
            "fechaActuacion": "2025-11-21T00:00:00",
            "actuacion": "A Despacho",
            "anotacion": "Enviado por la Oficina de Ejecución Civil Municipal",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-21T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2288409503,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 60,
            "fechaActuacion": "2025-11-20T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Otras",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-20T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2287925193,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 59,
            "fechaActuacion": "2025-11-19T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2025-11-19 a las: 8:15am",
            "fechaInicial": "2025-11-20T00:00:00",
            "fechaFinal": "2025-11-20T00:00:00",
            "fechaRegistro": "2025-11-19T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2287925183,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 58,
            "fechaActuacion": "2025-11-19T00:00:00",
            "actuacion": "Auto fija fecha y hora para remate",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-19T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2286406603,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 57,
            "fechaActuacion": "2025-11-14T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Respuesta Requerimiento",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-14T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2286406593,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 56,
            "fechaActuacion": "2025-11-14T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Respuesta Requerimiento",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-14T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2284272823,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 55,
            "fechaActuacion": "2025-11-14T00:00:00",
            "actuacion": "A Despacho",
            "anotacion": "Enviado por la Oficina de Ejecución Civil Municipal",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-14T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2277633563,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 54,
            "fechaActuacion": "2025-11-12T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Informe Mensual",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-11-12T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2242318403,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 53,
            "fechaActuacion": "2025-10-31T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2025-10-31 a las: 11:40am",
            "fechaInicial": "2025-11-04T00:00:00",
            "fechaFinal": "2025-11-04T00:00:00",
            "fechaRegistro": "2025-10-31T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2242318393,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 52,
            "fechaActuacion": "2025-10-31T00:00:00",
            "actuacion": "Auto corre traslado avalúo catastral",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-10-31T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2238606253,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 51,
            "fechaActuacion": "2025-10-30T00:00:00",
            "actuacion": "A Despacho",
            "anotacion": "Enviado por la Oficina de Ejecución Civil Municipal",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-10-30T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2235105583,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 50,
            "fechaActuacion": "2025-10-29T00:00:00",
            "actuacion": "Traslado Art. 110",
            "anotacion": null,
            "fechaInicial": "2025-10-31T00:00:00",
            "fechaFinal": "2025-11-05T00:00:00",
            "fechaRegistro": "2025-10-29T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2233733603,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 49,
            "fechaActuacion": "2025-10-29T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Respuesta Requerimiento",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-10-29T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2229642723,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 48,
            "fechaActuacion": "2025-10-28T00:00:00",
            "actuacion": "Fijacion estado",
            "anotacion": "Actuación registrada el 2025-10-28 a las: 10:55am",
            "fechaInicial": "2025-10-29T00:00:00",
            "fechaFinal": "2025-10-29T00:00:00",
            "fechaRegistro": "2025-10-28T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2229642713,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 47,
            "fechaActuacion": "2025-10-28T00:00:00",
            "actuacion": "Auto corre traslado avalúo catastral",
            "anotacion": "",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-10-28T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2225224153,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 46,
            "fechaActuacion": "2025-10-27T00:00:00",
            "actuacion": "A Despacho",
            "anotacion": "Enviado por la Oficina de Ejecución Civil Municipal",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-10-27T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2220935803,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 45,
            "fechaActuacion": "2025-10-24T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Medidas Cautelares",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-10-24T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2220935793,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 44,
            "fechaActuacion": "2025-10-24T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Medidas Cautelares",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-10-24T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        },
        {
            "idRegActuacion": 2220935783,
            "llaveProceso": "17001400301020240019200",
            "consActuacion": 43,
            "fechaActuacion": "2025-10-24T00:00:00",
            "actuacion": "Recepción Memorial",
            "anotacion": "Recepción Memorial por Aporta Avalúo",
            "fechaInicial": null,
            "fechaFinal": null,
            "fechaRegistro": "2025-10-24T00:00:00",
            "codRegla": "00                              ",
            "conDocumentos": false,
            "cant": 82
        }
    ],
    "paginacion": {
        "cantidadRegistros": 82,
        "registrosPagina": 40,
        "cantidadPaginas": 3,
        "pagina": 1,
        "paginas": null
    }
}
```

### Respuesta cuando el proceso no existe - 404

```json
{"StatusCode":404,"Message":"No se encontraron Actuaciones para el Proceso: 142573703"}
```

### Otros estados de error observados

- 500 cuando: ocurre un error interno del sistema  --> ```{"StatusCode":500,"Message":"Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')"}```
- 404 cuando: el id es demasiado grande para parsear a int --> ```{"StatusCode":404,"Message":"Error converting data type bigint to int."}```
---

## Información adicional útil

- **Estructura del radicado de 23 dígitos**:
  - Posición 1-2: Departamento
  - Posición 3-5: Municipio
  - Pisición 6-12: Despacho
  - Posición 13-16: Año en que se radicó el proceso
  - Posición 17-23 : Consecutivo --> Una nota importante aquí, muchas veces el consecutivo del proceso no alcanza para los 23 dígitos, en ese caso se deben agregar los correspondientes ceros (0) al final para llegar a los 23 dígitos.
- **Catálogo oficial de tipos de juzgado** Primero debemos hacer una correción, no se llama tipo de juzgado sino que son dos campos uno Entidad y otro Especialidad, sirve para agregar mas filtros de búsqueda opcionales, tanto para la pestaña procesos como para crear un nuevo proceso sin el radicado completo, pero no se si sea adecuado agregarlo a este MVP: 
	- Entidad: Entidad a la que pertenece el despacho, campos Id de dos números y el nombre, ejemplo: Id = 71, nombre= 'CENTRO DE SERVICIOS JUDICIALES'
	- Especialidad: Especialidad del despacho : Id de dos números y el nombre, ejemplo: Id = 03, nombre= 'CIVIL'
- **Ejemplo de actuación con términos** Está en la respuesta de las actuaciones pero MUY IMPORTANTE AQUÍ: 
	- La persona de negocio nos aclara que lo que se declara hoy entra en vigencia al siguiente día hábil, por eso en una de las respuestas de ejemplo NO todas las actuaciones tienen fechaInicial ni fechaFinal, aquí las paricularidades:
		- Generalmente los que en la actuación empiezan por auto inmediatamente despés viene otro registro con actiación :"fijación ..." y ambas fechaRegistro iguales, esto quiere decir que es la fijación o entrada en vigencia de ese Auto, por ende no deberían duplicarse en la vista.
		- Los que dicen radicación también tienen fechas de inicio y fin se tratan como cualquier otra actuación.
		- Los que NO tengan fecha pues simplemente no se les pone.
- **¿Existe endpoint de "actuaciones desde fecha X"** que permita consultar solo cambios nuevos? --> No, la consulta es por página
- **Aclaración data que vamos a precargar: ** Los departamentos, municiíos y despachos son datos que vamos a pre cargar en la base de datos nosotros mismos, ya que es información estática que siempre será igual.
- **Descarga de documento pdf:** En vista que el endpoint de documentos nunca logramos que nos retornara una respuesta exitosa, la idea sería armar un pdf con la información que ya hemos consultado, por ahora será así, mas adelante esta funcionalidad cambiará, pero no nos meteremos en eso ahora para el MVP.  