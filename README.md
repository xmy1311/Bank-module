# Devsu · Prueba Técnica .NET — Microservicios Cliente / Cuenta

Sistema bancario mínimo repartido en dos microservicios que se comunican de forma
asíncrona, con ASP.NET Core 10, Entity Framework Core, SQL Server, RabbitMQ y Docker.

**Xiomara Zapata Vásquez** · Septiembre 2026

---

## Puesta en marcha

```bash
git clone <url-del-repositorio>
cd Bank-module
cp .env.example .env
docker compose up --build
```

Eso es todo. El compose levanta SQL Server y RabbitMQ, **espera a que estén sanos**,
arranca los dos servicios, aplica las migraciones y siembra los datos del enunciado.

| Recurso | URL |
|---|---|
| Customer Service — Swagger | http://localhost:5001/swagger |
| Account Service — Swagger | http://localhost:5002/swagger |
| RabbitMQ — administración | http://localhost:15672 |
| Liveness | `/health` |
| Readiness (valida SQL Server y RabbitMQ) | `/health/ready` |

> **Si algún puerto está ocupado** el arranque falla con
> `Bind for 0.0.0.0:1433 failed: port is already allocated`.
> Comprueba con `docker ps --format "{{.Names}}\t{{.Ports}}"` y cambia el valor en
> `.env`. Dentro de la red de Docker los puertos no cambian.

### Comprobación en 30 segundos

```bash
curl http://localhost:5001/health/ready
curl http://localhost:5002/api/cuentas
curl "http://localhost:5002/api/reportes?fechaInicio=2022-02-01&fechaFin=2022-02-28&clienteId=CLI-0002"
```

---

## Arquitectura

```
                     ┌──────────────────┐
                     │ Postman /Swagger │
                     └────────┬─────────┘
              ┌───────────────┴───────────────┐
              ▼                               ▼
    ┌───────────────────┐           ┌───────────────────┐
    │  Customer Service │           │  Account Service  │
    │  Persona/Cliente  │           │ Cuenta/Movimiento │
    │      :5001        │           │      :5002        │
    └─────────┬─────────┘           └─────────┬─────────┘
              │                               │
        ┌─────▼─────┐                   ┌─────▼─────┐
        │ CustomerDb│                   │ AccountDb │
        └───────────┘                   └───────────┘
              │                               ▲
              │   ClienteCreado               │
              │   ClienteActualizado          │
              └────────▶ RabbitMQ ────────────┘
                      (topic exchange)
```

**No hay ninguna flecha HTTP entre los dos servicios**, y es lo más importante del
diseño. El Account Service mantiene una réplica local de clientes alimentada por
eventos, así que puede abrir cuentas y generar reportes aunque el Customer Service
esté completamente caído.

### Por qué la comunicación asíncrona no es decorativa

 El Account Service **necesita** los datos del cliente para dos cosas concretas:

1. **Validar la apertura de cuentas** — `POST /api/cuentas` comprueba que el cliente
   exista y esté activo.
2. **Generar el estado de cuenta (F4)** — el reporte incluye nombre e identificación.

Sin el evento, ninguna de las dos funciona. La alternativa —una llamada HTTP
síncrona— acoplaría los servicios en el tiempo: si uno cae, el otro cae con él.

### Capas (idénticas en ambos servicios)

```
API  ──▶  Application  ──▶  Domain
 └──────▶ Infrastructure ──────┘
```

| Capa | Depende de | Prohibido |
|---|---|---|
| **Domain** | Nada — ni un solo `PackageReference` | EF Core, ASP.NET, RabbitMQ |
| **Application** | Domain | `DbContext`, SQL |
| **Infrastructure** | Application, Domain | — |
| **API** | Application (+ Infrastructure solo en `Program.cs`) | Lógica de negocio |

Verificable de un vistazo: `Devsu.Customer.Domain.csproj` y
`Devsu.Account.Domain.csproj` están vacíos salvo el `Sdk`, y ningún archivo de los
dominios importa `Microsoft.EntityFrameworkCore`.

---

## Stack

| | |
|---|---|
| Runtime | .NET 10 (LTS) |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core 10 (Code First + migraciones) |
| Base de datos | SQL Server 2022 — una instancia, dos bases |
| Mensajería | RabbitMQ (`RabbitMQ.Client` 7.x, API asíncrona) |
| Pruebas | xUnit · Shouldly · Testcontainers |
| Contenedores | Docker + Docker Compose |
| CI | GitHub Actions |

Dependencias deliberadamente **no** incluidas: MediatR, AutoMapper, FluentValidation,
Serilog y Polly. Cada omisión está justificada en [`DECISIONS.md`](DECISIONS.md).

---

## Estructura

```
Bank-module/
├── docker-compose.yml            Orquestación completa
├── BaseDatos.sql                 Esquema + seed (generado desde las migraciones)
├── .env.example                  Plantilla de configuración
├── Directory.Build.props         TFM y propiedades comunes
├── Directory.Packages.props      Versiones centralizadas de NuGet
│
├── src/
│   ├── shared-contracts/
│   │   ├── Devsu.Shared.Contracts    Contratos de eventos (records, cero lógica)
│   │   └── Devsu.Shared.Messaging    Conexión, topología y health check de RabbitMQ
│   └── Services/
│       ├── Customer/                 Domain · Application · Infrastructure · Api
│       └── Account/                  Domain · Application · Infrastructure · Api
│
├── tests/
│   ├── Devsu.Customer.UnitTests      F5 — invariantes de Cliente
│   ├── Devsu.Account.UnitTests       Núcleo financiero y eventos fuera de orden
│   └── Devsu.Account.IntegrationTests F6 — API real contra SQL Server real
│
├── postman/                      Colección (35 peticiones) + entorno
└── .github/workflows/ci.yml      Build · tests · imágenes Docker
```

---

## Funcionalidades

| | Estado | Dónde |
|---|---|---|
| **F1** CRUD Cliente, CRU Cuenta y Movimiento | Completo, con una desviación documentada | `/api/clientes`, `/api/cuentas`, `/api/movimientos` |
| **F2** Registro de movimientos y actualización de saldo | Completo | `Cuenta.RegistrarMovimiento` |
| **F3** `"Saldo no disponible"` | Completo — `422` + código estable | `SaldoNoDisponibleException` |
| **F4** Estado de cuenta por rango y cliente | Completo, en dos sintaxis | `/api/reportes` |
| **F5** Prueba unitaria de Cliente | 13 pruebas | `Devsu.Customer.UnitTests` |
| **F6** Prueba de integración | 8 pruebas con SQL Server real | `Devsu.Account.IntegrationTests` |
| **F7** Despliegue en contenedores | Completo | `docker-compose.yml` |

### La desviación de F1: Movimiento no tiene Update

**Deliberada y documentada.** Un asiento contable es inmutable: modificar el valor de
una transacción registrada rompería la ecuación
`SaldoInicial + Σ Movimientos = SaldoDisponible`, invalidaría el histórico y eliminaría
la trazabilidad que exige F2. La operación correcta para corregir un movimiento erróneo
es el **reverso**: un contra-asiento de igual valor y signo opuesto, quedando ambos
registros visibles en el estado de cuenta.

 El update se implementa en `Cuenta`. Se modifica el estado y el tipo de cuenta.

---

## Endpoints

### Customer Service — `:5001`

| Verbo | Ruta | |
|---|---|---|
| `GET` | `/api/clientes` | Listado con filtros y paginación |
| `GET` | `/api/clientes/{clienteId}` | |
| `POST` | `/api/clientes` | El `ClienteId` lo genera el servidor |
| `PUT` | `/api/clientes/{clienteId}` | Reemplazo completo |
| `PATCH` | `/api/clientes/{clienteId}` | Solo los campos presentes |
| `DELETE` | `/api/clientes/{clienteId}` | **Baja lógica** + evento |
| `PATCH` | `/api/clientes/{clienteId}/password` | Endpoint dedicado |

### Account Service — `:5002`

| Verbo | Ruta | |
|---|---|---|
| `GET` `POST` `PUT` `PATCH` | `/api/cuentas` | Sin DELETE (el enunciado pide CRU) |
| `GET` `POST` | `/api/movimientos` | Sin Update — ver arriba |
| `GET` | `/api/reportes` | F4 |

### El reporte acepta dos sintaxis

```http
GET /api/reportes?fechaInicio=2022-02-01&fechaFin=2022-02-28&clienteId=CLI-0002
GET /api/reportes?fecha=2022-02-01,2022-02-28&cliente=CLI-0002
```

La segunda es la del enunciado —`(/reportes?fecha=rango fechas & cliente)`
Se implementó el contrato limpio **y** se aceptó el literal como
alias, para que la URL del documento funcione tal cual. 
El rango es inclusivo en ambos extremos: sin eso, un movimiento del último día quedaría fuera.

### Formato de errores — RFC 7807

```json
{
  "type": "https://httpstatuses.io/422",
  "title": "Saldo no disponible",
  "status": 422,
  "instance": "/api/movimientos",
  "code": "SALDO_NO_DISPONIBLE",
  "correlationId": "0c676a23-0329-44b7-b76c-a9d11eb3d449"
}
```

`code` es un identificador **estable**: un cliente puede ramificar su lógica sin
depender del texto del mensaje. Ningún controlador tiene `try/catch` — de eso se
encarga un middleware global.

| Código de negocio | HTTP |
|---|---|
| `SALDO_NO_DISPONIBLE`, `CUENTA_INACTIVA`, `CLIENTE_INACTIVO` | `422` |
| `ENTIDAD_NO_ENCONTRADA` | `404` |
| `CLIENTE_DUPLICADO`, `CUENTA_DUPLICADA`, `CONFLICTO_CONCURRENCIA` | `409` |
| Validación de entrada | `400` con `ValidationProblemDetails` |

**422  para saldo insuficiente** 
**409  Conflicto de concurrencia o de versión** 

---

## Base de datos

Una instancia de SQL Server con **dos bases**, una por servicio: aislamiento lógico
sin duplicar un contenedor de 1,5 GB de RAM.

| `CustomerDb` | `AccountDb` |
|---|---|
| `Personas`, `Clientes` (TPT) | `Cuentas`, `Movimientos` |
| `SeqClienteId` | `ClientesReplica`, `EventosProcesados` |

`BaseDatos.sql` está en la raíz y es **idempotente**: crea ambas bases, el esquema y el
seed, y registra las migraciones como aplicadas para que la API no intente reaplicarlas.

```bash
sqlcmd -S localhost,1433 -U sa -P "<password>" -C -i BaseDatos.sql
```

### Los tres constraints que importan

```sql
CK_Clientes_Credenciales     LEN(PasswordHash) >= 32 AND LEN(PasswordSalt) >= 16
CK_Cuentas_SaldoDisponible   SaldoDisponible >= 0
CK_Movimientos_TipoCoherente (Valor > 0 AND Tipo='Deposito') OR (Valor < 0 AND Tipo='Retiro')
```

El primero hace **físicamente imposible** guardar una contraseña en claro, incluso con
un INSERT manual. El segundo hace que un saldo negativo no se pueda persistir aunque
falle toda la aplicación. El tercero impide que el tipo contradiga al signo del valor.
Son reglas de negocio convertidas en restricciones físicas: defensa en profundidad.

### Verificar la consistencia del saldo

```sql
SELECT c.NumeroCuenta, c.SaldoInicial, c.SaldoDisponible,
       c.SaldoInicial + ISNULL(SUM(m.Valor), 0) AS SaldoCalculado
FROM   dbo.Cuentas c
LEFT JOIN dbo.Movimientos m ON m.CuentaId = c.CuentaId
GROUP BY c.NumeroCuenta, c.SaldoInicial, c.SaldoDisponible
HAVING c.SaldoDisponible <> c.SaldoInicial + ISNULL(SUM(m.Valor), 0);
-- 0 filas = el saldo materializado nunca miente
```

### Contraseñas

El enunciado da contraseñas literales (`1234`, `5678`, `1245`). Se almacenan como
**PBKDF2-HMAC-SHA256, 100.000 iteraciones, salt de 16 bytes por cliente**, en Base64.
Los casos de uso siguen funcionando —`VerificarPassword("1234")` devuelve `true` para
Jose Lema— y en la tabla no hay texto plano. El salt es por cliente, así que dos
clientes con la misma contraseña producen hashes distintos, lo que invalida cualquier
ataque por rainbow table.

> **Datos inventados, declarados:** el enunciado no proporciona `Genero`, `Edad` ni
> `Identificacion`. Los del seed son inventados. Las identificaciones tienen formato de
> cédula ecuatoriana pero **no** son válidas: no se valida el dígito verificador.

---

## Concurrencia

Dos retiros simultáneos sobre la misma cuenta son el escenario crítico de un sistema
financiero. La protección es **concurrencia optimista con `RowVersion`**, declarada
como propiedad sombra de EF Core para que el dominio no conozca ese detalle de
persistencia.

| | Transacción A | Transacción B |
|---|---|---|
| 1 | Lee saldo 1000, RowVersion `0xA1` | Lee saldo 1000, RowVersion `0xA1` |
| 2 | Valida 1000 − 700 ✓ | Valida 1000 − 500 ✓ |
| 3 | `UPDATE ... WHERE RowVersion = 0xA1` → 1 fila. **COMMIT** | |
| 4 | | `UPDATE ... WHERE RowVersion = 0xA1` → **0 filas** → excepción |
| 5 | | Reintento: relee saldo **300**. 300 − 500 ✗ → `422` |

Nunca hay saldo negativo y nunca se pierde una actualización. El servicio reintenta
hasta 3 veces; si persiste, `409`. Isolation level `READ COMMITTED` (el de por
defecto): `RowVersion` ya resuelve el *lost update*, y subir a `SERIALIZABLE` reduciría
la concurrencia y aumentaría los deadlocks sin aportar nada.

No se abre transacción explícita: `SaveChanges` ya envuelve el UPDATE del saldo y el
INSERT del movimiento en una sola transacción.

---

## Mensajería

```
Customer Service
     │ cliente.creado · cliente.actualizado · cliente.desactivado
     ▼
devsu.clientes (topic, durable)
     │ binding: cliente.*
     ▼
account.clientes.sync (durable, ack manual, prefetch 1)
     │ nack(requeue:false) tras agotar reintentos
     ▼
devsu.clientes.dlx (fanout) ──▶ account.clientes.sync.dlq
```

La topología se **declara por código** al arrancar, de forma idempotente: el evaluador
no tiene que configurar nada en la UI del broker.

**Sobre del evento** — `eventId` (idempotencia), `eventType`, `eventVersion`,
`occurredOn` (detección de eventos obsoletos), `correlationId` (traza de extremo a
extremo, cruzando el broker) y `data`. El payload lleva **solo** `clienteId`, `nombre`,
`identificacion` y `estado`: nunca la contraseña, la dirección, el teléfono ni la edad.
Minimizar el payload es una decisión de seguridad.

| Riesgo | Mitigación |
|---|---|
| Mensaje duplicado | Tabla `EventosProcesados` + upsert idempotente por `ClienteId` |
| Evento fuera de orden | Se descarta si `occurredOn <= ActualizadoEn` |
| Error transitorio (BD caída) | 3 reintentos con backoff exponencial |
| Mensaje envenenado | `nack(requeue:false)` → DLQ, **sin reintentar**: reintentar un error permanente es un bucle infinito disfrazado |
| Consumidor caído | Cola durable con mensajes persistentes: se acumulan y se procesan al volver |
| Broker no disponible al arrancar | Reintento con backoff + `AutomaticRecoveryEnabled` |

El `ack` es **manual y posterior al commit**. Si el proceso muere a mitad, el mensaje
vuelve a la cola en lugar de perderse.

### Consistencia eventual

Aparece **únicamente** en la réplica de clientes. Todo lo demás —saldos, movimientos,
cuentas— es fuertemente consistente dentro de su transacción, que es donde importa: el
dinero nunca es eventualmente consistente en este diseño.

Consecuencia observable: crear un cliente e inmediatamente después crear su cuenta
puede devolver `404`. La ventana son milisegundos con el broker sano. La colección de
Postman incluye una pausa entre ambas peticiones. **Es una propiedad del diseño, no un
defecto**, y se prefirió eso a añadir un fallback HTTP síncrono que reintroduciría el
acoplamiento y ocultaría los problemas de sincronización hasta producción.

---

## Pruebas

```bash
dotnet test Devsu.Banking.sln                      # las 37

dotnet test tests/Devsu.Customer.UnitTests         # 13 · F5 · dominio Cliente
dotnet test tests/Devsu.Account.UnitTests          # 16 · F2, F3 · saldo, sobregiro, réplica
dotnet test tests/Devsu.Account.IntegrationTests   #  8 · F6 · requiere Docker en ejecución
```

Resultado esperado:

```
Test summary: total: 37, failed: 0, succeeded: 37, skipped: 0
```

Las de integración **no** necesitan `docker compose up`: Testcontainers levanta y
destruye su propio SQL Server. Basta con que el demonio de Docker esté corriendo.
La primera ejecución descarga la imagen y tarda bastante más que las siguientes.

**Unitarias** — prueban comportamiento y reglas críticas, no getters. Las más relevantes:

- La contraseña nunca se almacena en claro, y dos clientes con la misma contraseña
  producen hashes distintos.
- `Desactivar()` es idempotente y conserva la fecha original.
- Un movimiento que falla por saldo **no deja efectos secundarios**: ni saldo alterado
  ni movimiento fantasma.
- La ecuación `SaldoDisponible = SaldoInicial + Σ Valor` se mantiene tras varios
  movimientos, y `SaldoInicial` no se mueve.
- Un evento obsoleto no retrocede el estado de la réplica.

**Concurrencia** — `ConcurrenciaTests` lanza diez retiros simultáneos de 200 sobre una
cuenta con saldo 1000 y comprueba que el saldo final **nunca queda negativo**, que
cuadra exactamente con los retiros aceptados, que ninguno devuelve `500`, y que el
histórico de movimientos coincide con el saldo. Es la prueba que demuestra que
`RowVersion` hace su trabajo. La complementaria lanza depósitos concurrentes, donde
cualquier diferencia sería un *lost update* puro.

**Integración** — API real contra **SQL Server real** levantado por Testcontainers.
El proveedor InMemory no aplica `CHECK` constraints, ni tipos `decimal`, ni
`RowVersion`: una prueba verde ahí no diría nada sobre si el esquema real habría
aceptado los datos. Aquí se ejecutan las migraciones de verdad y los constraints de
verdad.

---

## Postman

```
postman/Devsu.postman_collection.json     35 peticiones en 5 carpetas
postman/Devsu.postman_environment.json    baseUrl, customerServiceUrl, accountServiceUrl
```

Importa ambos y ejecútala completa con el **Collection Runner**: las variables
(`clienteIdNuevo`, `numeroCuentaNueva`, `movimientoId`) se encadenan solas y cada
petición lleva sus aserciones. La identificación y el número de cuenta se generan
únicos por ejecución, así que se puede correr muchas veces sin chocar con las claves
únicas.

La carpeta **5 · Integración asíncrona** es la que demuestra la mensajería: da de baja
un cliente en el Customer Service y comprueba que sus cuentas quedan inactivas en el
Account Service, sin que nadie las haya llamado.

---

## Solución de problemas

| Síntoma | Causa | Solución |
|---|---|---|
| `Bind for 0.0.0.0:1433 failed` | Puerto ocupado | Cambia `MSSQL_PORT` en `.env` |
| `Login failed for user 'sa'` | Contraseña débil | Debe tener mayúscula, minúscula, dígito y símbolo |
| Las APIs no arrancan la primera vez | SQL Server tarda ~40 s | Los healthchecks ya lo cubren; espera |
| `404` al crear una cuenta recién creado el cliente | Consistencia eventual | Espera 1-2 s; ver arriba |
| `Globalization Invariant Mode is not supported` | `InvariantGlobalization` activado | Es incompatible con `Microsoft.Data.SqlClient`; ya está desactivado |
| Las pruebas de integración fallan | Docker no disponible | Testcontainers necesita Docker corriendo |

---

## Limitaciones conocidas

Se listan explícitamente porque una solución sin limitaciones declaradas es una
solución que no se ha revisado.

1. **Sin Outbox Pattern.** El `SaveChanges` y la publicación del evento no son
   atómicos: si el proceso muere entre ambos, la réplica queda desactualizada sin que
   nada lo detecte. Es la limitación más relevante y la primera que se resolvería en
   producción. Mitigación actual: publicación inmediata tras el commit, log de error
   explícito y `eventId` estable para reproceso.
2. **Sin autenticación ni autorización.** Los endpoints están abiertos. En producción
   irían JWT con OAuth2 y scopes por operación.
3. **Sin auditoría con usuario.** Las entidades llevan fechas de creación y
   modificación, pero no *quién*. Requiere autenticación primero.
4. **Moneda única implícita.** Un sistema multi-moneda necesitaría un value object
   `Dinero(Monto, Moneda)` y la prohibición de operar entre monedas distintas.
5. **`Edad` es un dato derivado** que se desactualiza solo. En producción se guardaría
   `FechaNacimiento`. Se mantiene porque el enunciado lo pide así.
6. **Sin métricas ni tracing distribuido.** Hay logging estructurado y correlation ID;
   faltaría OpenTelemetry con Prometheus.

## Mejoras futuras

Outbox Pattern · autenticación JWT · OpenTelemetry con alertas sobre la profundidad de
la DLQ y la tasa de conflictos de concurrencia · idempotencia en `POST /movimientos`
con cabecera `Idempotency-Key` · particionado de `Movimientos` por fecha · pruebas de
carga para validar el umbral en que la concurrencia optimista deja de ser adecuada ·
endpoint de reverso de movimientos.

---

## Decisiones de diseño

El registro completo —con las alternativas evaluadas y por qué se descartaron— está en
**[`DECISIONS.md`](DECISIONS.md)**.
