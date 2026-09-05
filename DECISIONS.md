# Registro de decisiones

Cada entrada dice **qué se decidió**, **qué alternativas se evaluaron** y **por qué se
descartaron**. Las marcadas como `DECISIÓN DE DISEÑO` no vienen del enunciado: son
elecciones propias ante una especificación ambigua o incompleta, y están aquí para que
se puedan discutir, no para presentarlas como requisitos.

---

## Stack y arquitectura

### D1 · .NET 10 LTS

.NET 8 y .NET 9 alcanzan fin de soporte el **10 de noviembre de 2026**. Entregar sobre
una versión que expira en semanas sería difícil de defender. .NET 10 es el LTS vigente.

### D2 · Clean Architecture de 4 capas, sin CQRS ni MediatR

Con ~10 casos de uso por servicio, MediatR añade indirección y dificulta seguir el
flujo sin resolver ningún problema real. Se usaría con decenas de handlers o con
modelos de lectura y escritura realmente divergentes.

Sí se aplica **CQRS ligero sin infraestructura**: las escrituras pasan por el agregado;
las lecturas masivas (listados, reporte) usan servicios de consulta con `AsNoTracking()`
que proyectan directamente a DTO. Cargar el agregado `Cuenta` con sus 10.000
movimientos para devolver un reporte sería un error de rendimiento grave.

**Desviación reconocida:** `Api` referencia `Infrastructure` para poder llamar a
`AddInfrastructure()` en `Program.cs`. Formalmente rompe la regla de dependencias.
Ningún controlador ni servicio de aplicación usa tipos de Infrastructure. La
alternativa purista es un proyecto composition root separado, que para dos servicios
es ceremonia. Es mejor reconocerlo que fingir que no existe.

### D3 · Una instancia de SQL Server, dos bases de datos

Database-per-service a nivel lógico sin duplicar un contenedor de 1,5 GB de RAM.
Permite mover `AccountDb` a un servidor más potente sin tocar `CustomerDb`.

### D4 · Herencia Persona → Cliente mapeada como TPT

| Opción | Por qué no |
|---|---|
| TPH (una tabla + discriminador) | Columnas NULLables de Cliente en filas de Persona; más rápido pero menos expresivo |
| TPC | Duplica columnas y complica las claves |
| **TPT** ✔ | Refleja la herencia, produce un `BaseDatos.sql` legible, y tiene argumento de dominio: en un banco existen personas que no son clientes (beneficiarios, apoderados) |

El coste del JOIN es irrelevante a este volumen. Con millones de filas donde el 100% de
las personas fueran clientes, TPH sería preferible.

### D5 · RabbitMQ

Frente a Azure Service Bus y AWS SQS, que necesitan nube o emuladores: imagen oficial
ligera, cliente .NET maduro y **UI de administración** para poder enseñar el mensaje
durante la entrevista. La prueba debe ejecutarse localmente con Docker.

### D7 · Réplica local de clientes en lugar de llamadas HTTP

**La decisión central de toda la solución.** `Cuenta.ClienteId` cruza el límite del
microservicio: no hay FK posible.

| Opción | Por qué no |
|---|---|
| HTTP síncrono | Acopla temporalmente: si Customer cae, Account cae. Contradice el requisito de asincronía |
| No validar | Inaceptable en un contexto bancario |
| **Réplica local por eventos** ✔ | Desacoplamiento real; el reporte se resuelve sin salir del servicio |

Es lo que hace que la asincronía **no sea artificial**: sin el evento, el Account
Service no puede abrir cuentas ni generar el reporte.

### D16 · Dominio en español, infraestructura en inglés

El mix es inevitable: el enunciado impone rutas en español y el mensaje literal
`"Saldo no disponible"`, y la BCL de .NET está en inglés. La decisión real es **dónde
va la costura**:

- Dominio en español (`Cliente`, `Cuenta`, `Saldo`) — es el *ubiquitous language*.
- Patrones e infraestructura en inglés (`Repository`, `Service`, `Middleware`).
- Endpoints, tablas y JSON en español.

La alternativa —todo el código en inglés— obligaría a una capa de traducción
`Account ↔ CuentaResponse` en cada DTO. Lo que penaliza en una revisión no es el idioma
elegido, sino la inconsistencia.

---

## Ambigüedades del enunciado y cómo se resolvieron

### A2 · `ClienteId` frente a la clave primaria

El enunciado dice que Cliente tiene `clienteid` **y además** que debe tener una clave
única. Se interpretó como dos identificadores distintos:

- `PersonaId` (GUID v7) — clave técnica, heredada por Cliente vía TPT.
- `ClienteId` (`CLI-0001`) — **código de negocio** con índice UNIQUE, generado por una
  `SEQUENCE` de SQL Server.

En la banca real el número de cliente es un dato de negocio, no un autoincremental.
Permite migrar de base de datos sin romper integraciones, y es el identificador que
viaja en el evento. `Identificacion` (cédula) también es UNIQUE, aunque el enunciado no
lo pide: una persona no puede registrarse dos veces.

> La secuencia **arranca en 4** porque el seed ocupa `CLI-0001`…`CLI-0003`. Si arrancara
> en 1, el primer cliente creado desde Postman colisionaría con Jose Lema.

### A3 · Qué significa `Movimiento.Saldo`

**Saldo resultante después del movimiento** (*running balance*). Es lo que muestra el
ejemplo del enunciado (`Movimiento: 600`, `Saldo Disponible: 700`) y la práctica
estándar en extractos bancarios. Permite auditar sin recalcular toda la historia.

### A4 · `SaldoInicial` significa dos cosas distintas

El enunciado usa el mismo nombre para el saldo de apertura de la cuenta y para una
columna por fila en la tabla del reporte. Los datos de prueba **no desambiguan**: cada
cuenta del ejemplo tiene un solo movimiento, así que ambas lecturas coinciden.

**Resolución:** `Cuenta.SaldoInicial` es el saldo de apertura e **inmutable** —no tiene
setter ni método que lo modifique—. Cada movimiento expone su propio saldo resultante,
con lo que el saldo previo es derivable (`saldo - valor`).

> El error clásico —`SaldoInicial += valor` en cada movimiento— destruye la
> trazabilidad. En este modelo es imposible de cometer.

### A6 · Qué significa "no contar con saldo" · `DECISIÓN DE DISEÑO`

El enunciado no define si aplica sobregiro. Regla adoptada: **un movimiento se rechaza
si `SaldoDisponible + Valor < 0`**, sin excepción por tipo de cuenta. Se modela como
`LimiteSobregiro = 0` para que la extensión sea trivial, pero **no se implementa el
sobregiro**: sería inventar un requisito. Dejar el saldo exactamente en cero sí está
permitido (caso 496825 del enunciado).

### A7 · `TipoMovimiento` frente al signo de `Valor`

Si el cliente enviara `tipo="Retiro"` y `valor=+500`, habría dos fuentes de verdad
contradictorias. **El API recibe únicamente `valor` con signo** —el enunciado dice
literalmente "se pueden tener valores positivos o negativos"— y el dominio **deriva** el
tipo. Se elimina por construcción el estado inconsistente, y un `CHECK` en base de datos
lo refuerza a nivel físico.

### A8 · DELETE de Cliente es una baja lógica · `DECISIÓN DE DISEÑO`

F1 exige CRUD completo. Pero el cliente tiene información financiera y sus cuentas
viven en **otro microservicio**, sin FK que las proteja.

| Opción | Por qué no |
|---|---|
| Hard delete | Deja cuentas huérfanas y destruye la trazabilidad |
| Hard delete condicionado | Obligaría a una llamada síncrona al Account Service |
| **Baja lógica + evento** ✔ | Coherente con retención de información financiera, y da un segundo caso de uso real a la mensajería |

`DELETE` devuelve `204`, marca el cliente inactivo y publica `ClienteDesactivado`; el
Account Service inactiva sus cuentas. Los movimientos **no se tocan**.

### A11 · Sintaxis del endpoint de reportes

El enunciado especifica `(/reportes?fecha=rango fechas & cliente)`. Se implementó el contrato limpio **y** se aceptó el literal como alias,
para que la URL del documento funcione tal cual. El rango es inclusivo en ambos
extremos.

### A12 · Contraseñas · `DECISIÓN DE DISEÑO`

El enunciado da contraseñas literales. Se almacenan con **PBKDF2-HMAC-SHA256, 100.000
iteraciones, salt de 16 bytes por cliente**. El seed contiene los hashes de `1234`,
`5678` y `1245`, así que los casos de uso siguen funcionando. La contraseña no aparece
en ningún DTO de respuesta ni en los logs, y `PUT`/`PATCH` de cliente no la modifican:
hay un endpoint dedicado.

La invariante está garantizada **estructuralmente**: `PasswordHash` tiene setter privado
y la única vía de escritura exige un `IPasswordHasher` (patrón *double dispatch*). No es
una convención que alguien pueda olvidar.

### A15 · `Edad` es un dato derivado · `DECISIÓN DE DISEÑO`

Se desactualiza solo. En producción se guardaría `FechaNacimiento`. Se mantiene como lo
pide el enunciado, con la observación registrada.

### A17 · Movimiento no tiene Update · `DECISIÓN DE DISEÑO`

Ver el README. Se prefirió actualizar el estado de la cuenta y el tipo de cuenta.

---

## Datos y persistencia

### D8 · Saldo materializado, no calculado

`SELECT SUM(Valor)` en cada consulta sería imposible de desincronizar, pero es O(n)
sobre una tabla que crece sin límite y complica el bloqueo para concurrencia: no habría
una fila estable que versionar.

Se adoptó el enfoque de la banca real: **el ledger de movimientos es la fuente de verdad
contable y `SaldoDisponible` es una proyección optimizada**. La consulta de
reconciliación del README es el mecanismo que prueba que la proyección es correcta.

### D9 · Concurrencia optimista con `RowVersion`

Alternativas evaluadas: bloqueo pesimista (`UPDLOCK`) — correcto pero reduce
concurrencia; `SERIALIZABLE` — resuelve pero es caro y propenso a deadlocks;
`UPDATE ... WHERE SaldoDisponible + @valor >= 0` — atómico y eficiente, pero saca la
regla de negocio del dominio.

`RowVersion` va como **propiedad sombra** de EF Core: `byte[]` es un detalle de
persistencia de SQL Server y ponerlo en la entidad metería infraestructura dentro del
dominio. La protección es idéntica y el dominio queda sin un solo `using` de EF Core.

**Umbral de cambio:** si la tasa de conflictos superara ~5%, se pasaría al `UPDATE`
condicional atómico o a una cola de comandos por cuenta.

### D10 · Middleware global con ProblemDetails (RFC 7807)

Elimina el `try/catch(Exception)` por controlador y unifica el formato de error. El
dominio conoce su `Codigo` estable; la traducción a HTTP vive en el middleware, porque
el mismo dominio debe poder usarse desde un worker que no tiene HTTP.

### Los campos obligatorios de tipo valor se declaran ANULABLES

`[Required]` comprueba que el valor no sea `null`. Un `bool`, un `int` o un `enum`
**nunca son null**, así que sobre un tipo de valor no anulable el atributo no valida
nada: si el cliente omite el campo, System.Text.Json lo deja en su valor por defecto
y la petición pasa.

Con `ActualizarCuentaRequest` eso significaba que un
`PUT /api/cuentas/478758 { "tipoCuenta": "Corriente" }` —sin `estado`— enlazaba
`Estado` a `false` y **desactivaba la cuenta en silencio**. En el Customer era peor:
desactivaba el cliente y publicaba `ClienteDesactivado`, que en cascada inactivaba
todas sus cuentas en el otro microservicio.

Todos los campos obligatorios de tipo valor pasan a ser anulables con `[Required]`:
omitirlos produce un `400` con `ValidationProblemDetails`, que es exactamente lo que
corresponde a un PUT (reemplazo completo: todos los campos son obligatorios). En los
servicios se usa `!.Value`, seguro porque `ModelState` ya rechazó la petición si
faltaba el campo.

Los DTO de PATCH ya eran anulables **sin** `[Required]`: ahí `null` significa
"no lo toques", y esa es justamente la diferencia semántica entre los dos verbos.

### D11 · `422` para saldo insuficiente

La petición es sintáctica y semánticamente válida; lo que falla es una regla de negocio
sobre el estado. `409 Conflict` es defendible y más común, pero encaja mejor en
conflictos de concurrencia o de versión, que es para lo que se reserva aquí.

### D13 · Repository por agregado, no genérico

Un `IRepository<T>` genérico sobre EF Core reexpone `DbSet<T>` sin añadir nada: `Add`,
`Remove` y `FindAsync` ya existen. Los repositorios por agregado sí encapsulan
**intención de negocio**: `ObtenerParaActualizarAsync` garantiza tracking activo para
que `RowVersion` funcione, mientras `ObtenerPorNumeroAsync` usa `AsNoTracking`. Con
`AsNoTracking` la concurrencia optimista simplemente no funcionaría.

`IUnitOfWork` lo implementa el propio `DbContext`, que **ya es** una unidad de trabajo:
envolverlo en otra clase no añadiría nada.

### Sin FK entre `Cuentas.ClienteId` y `ClientesReplica`

Ambas tablas viven en `AccountDb`, así que técnicamente se podría. No está a propósito:
`ClientesReplica` **no es una tabla maestra**, es una proyección cuyo ciclo de vida lo
gobiernan eventos. Con FK, un evento retrasado se manifestaría como un error 500 del
motor en lugar de un `404` de negocio limpio.

Donde sí hay FK es `Movimientos → Cuentas`, con `ON DELETE NO ACTION`: esa relación vive
dentro del mismo agregado y el mismo bounded context, así que el motor la protege.

### Enums persistidos como texto

`'Ahorros'` se entiende leyendo `BaseDatos.sql`; `1` no. El `CHECK` da la misma
integridad que daría un `TINYINT` y el coste de almacenamiento es irrelevante.

### `VARCHAR` con `IsUnicode(false)` en columnas de código

Ocupa la mitad que `NVARCHAR` en columnas indexadas. **Crítico:** sin
`IsUnicode(false)` en la configuración, EF Core envía el parámetro como `NVARCHAR`
contra una columna `VARCHAR` y la conversión implícita **invalida el índice**. Es un
problema invisible en desarrollo y muy visible en producción.

### Índices deliberadamente NO creados

Tan defendible como los que sí están. `IX_Clientes_Estado` e `IX_Cuentas_Estado` tienen
cardinalidad 2: el optimizador los ignoraría y solo penalizarían cada escritura. Un
índice se justifica por una consulta real y una cardinalidad que lo haga selectivo.

---

## Mensajería

### D6 · Eventos: `ClienteCreado`, `ClienteActualizado`, `ClienteDesactivado`

Los tres tienen consumidor real. Se evaluó publicar también desde el Account Service
(`CuentaCreada`, `MovimientoRegistrado`) y **se descartó**: el Customer Service no tiene
ningún caso de uso que los requiera. Un evento sin consumidor que lo necesite es
exactamente la asincronía artificial que hay que evitar. La dirección del flujo la
determina la dependencia de datos, y esa dependencia es unidireccional.

### Exchange `topic`, no `fanout` ni `direct`

Con `topic`, mañana un servicio de notificaciones puede suscribirse solo a
`cliente.desactivado` sin tocar el publicador. El coste frente a `direct` es cero.

### D14 · Outbox Pattern: NO implementado, sí documentado

**El problema es real.** En `ClienteService.CrearAsync` ocurren dos operaciones que no
son atómicas entre sí:

```csharp
await _unitOfWork.SaveChangesAsync();      // ① commit en SQL Server
await _eventPublisher.PublishAsync(evt);   // ② publicación en RabbitMQ
```

Si el proceso muere entre ① y ②, el cliente existe pero el Account Service nunca se
entera, y **nada lo detecta**.

La solución correcta es el Outbox: insertar el evento en una tabla dentro de la misma
transacción y despacharlo desde un background service. Requiere tabla, despachador,
marcado de estado, política de reintento, limpieza y manejo de publicación duplicada —
aproximadamente el mismo esfuerzo que toda la mensajería junta, para proteger una
ventana de fallo de milisegundos en un entorno de un solo nodo.

**Mitigación adoptada:** publicación inmediatamente después del commit, log de error
explícito si falla, y `eventId` estable para que un reproceso manual sea idempotente.

Es la limitación más relevante de la entrega y la primera que se resolvería en
producción.

### Idempotencia por partida doble

1. Tabla `EventosProcesados`: el registro del evento y la actualización de la réplica
   se persisten en la misma transacción.
2. La operación es un **upsert** por `ClienteId`, no un insert: aplicarla dos veces
   produce el mismo estado.

El primero protege contra el efecto duplicado; el segundo lo hace inofensivo aunque el
primero fallara.

### La topología se declara una vez por conexión

Declararla en cada publicación es idempotente y correcto, pero añade cuatro viajes de
ida y vuelta al broker por cada evento. Se movió a `RabbitMqConnection`, en el momento
de establecer la conexión: se ejecuta una sola vez y, como `AutomaticRecovery` crea una
conexión nueva tras una caída, se vuelve a declarar sola sin código adicional.

Se descartó hacerlo en un `IHostedService` de arranque porque fallaría el inicio de la
aplicación si el broker no estuviera disponible — y todo el diseño busca justo lo
contrario: que las APIs sigan sirviendo aunque RabbitMQ esté caído.

### Violación de índice único traducida a 409

Los servicios comprueban la existencia antes de insertar, pero **comprobar y luego
insertar no es atómico**: dos peticiones concurrentes con la misma identificación pasan
ambas la comprobación y la segunda choca contra el índice UNIQUE. El dato queda íntegro
—el índice hace su trabajo— pero sin traducción el error saldría como `500`.

El `DbContext` detecta los errores 2601 y 2627 de SQL Server y lanza
`ConflictoUnicidadException`, un tipo de la Application; el middleware la mapea a `409`
con el código `RECURSO_DUPLICADO`. Mismo criterio que con la concurrencia optimista: EF
Core no se filtra fuera de la capa de infraestructura.

### Un canal por publicación, una conexión por proceso

`IChannel` **no es thread-safe**: compartir uno entre peticiones concurrentes corrompe
el protocolo AMQP. Los canales son baratos; la conexión TCP, que sí se reutiliza, es lo
caro.

---

## Testing y operación

### D15 · Testcontainers para las pruebas de integración

El proveedor InMemory no aplica `CHECK` constraints, ni tipos `decimal`, ni
`RowVersion`. Una prueba verde ahí no dice nada sobre si el esquema real habría aceptado
los datos. SQLite se acerca más, pero su dialecto difiere de T-SQL. Con Testcontainers
se ejecutan las migraciones de verdad y los constraints de verdad.

### `BaseDatos.sql` se genera desde las migraciones

Tener el esquema definido en dos sitios es garantía de divergencia. Las migraciones de
EF Core son la fuente de verdad y `BaseDatos.sql` se genera con
`dotnet ef migrations script --idempotent`. El script registra las migraciones en
`__EFMigrationsHistory` para que la API no intente reaplicarlas sobre una base ya
creada.

### El health check de RabbitMQ tiene un tope de 5 segundos

Lo detectó la prueba de integración: sin broker, `/health/ready` tardaba **100
segundos** en responder y la prueba moría por timeout del `HttpClient`. La causa era
que la comprobación entraba en el bucle de reintentos de `RabbitMqConnection` —diez
intentos con backoff exponencial— pensado para el arranque, no para una sonda.

Una sonda de readiness que tarda minuto y medio es inútil: el orquestador ya la habrá
dado por fallida, y mientras tanto retiene un hilo. Ahora usa un
`CancellationTokenSource` enlazado con tope de 5 segundos: interesa saber si el broker
responde **ahora**, no esperar a que vuelva.

Los reintentos con backoff siguen siendo correctos donde sí tienen sentido: al
establecer la conexión durante el arranque, cuando el broker todavía no está listo
dentro del compose.

### Dos endpoints de salud, no uno

`/health` (liveness) no toca la base de datos. Si lo hiciera, una caída momentánea de
SQL Server haría que Docker reiniciara un contenedor perfectamente sano, empeorando el
incidente. `/health/ready` sí comprueba SQL Server y RabbitMQ.

### Sin `InvariantGlobalization`

Reduce el tamaño de la imagen y acelera el arranque, pero **`Microsoft.Data.SqlClient`
lo rechaza** con `"Globalization Invariant Mode is not supported"`: necesita ICU para
las comparaciones sensibles a intercalación.

---

## Dependencias que NO se añadieron

| Descartada | Motivo | Cuándo sí |
|---|---|---|
| **MediatR** | ~10 casos de uso; añade indirección sin beneficio | Decenas de handlers |
| **AutoMapper** | 8 DTOs; el mapeo manual es explícito y falla en compilación, no en runtime | Decenas de mapeos repetitivos |
| **FluentValidation** | DataAnnotations + `[ApiController]` ya producen `ValidationProblemDetails` sin ningún paquete | Reglas de validación complejas o condicionales |
| **Serilog** | El logging integrado ya da JSON estructurado y scopes | Cuando se necesiten sinks (Seq, Elastic, archivo) |
| **Polly** | El reintento de conexión son quince líneas | Circuit breaker, timeouts, bulkhead |
| **FluentAssertions** | Cambio de licencia en la v8 | — (se usa Shouldly) |
| **API Gateway / BFF** | Dos servicios y un consumidor: un salto de red y un punto de fallo | Varios frontends, o autenticación centralizada |
| **Event Sourcing** | El ledger de movimientos ya da trazabilidad completa | Auditoría regulatoria con reconstrucción a cualquier punto |
| **Redis** | El read model local ya cumple la función de caché | Sesiones distribuidas o caché compartida |
| **Kubernetes** | El enunciado pide Docker | Producción con réplicas y autoescalado |

---

## Acoplamiento declarado: `Devsu.Shared.Contracts`

Compartir un ensamblado entre dos microservicios los acopla en tiempo de compilación: un
cambio en el contrato obliga a recompilar y redesplegar ambos, que es justo lo que los
microservicios pretenden evitar.

Se aceptó **con condiciones estrictas** —solo `record`s inmutables, cero lógica, cero
referencias a otras capas, y un campo `eventVersion` en el sobre— porque en un monorepo
que se despliega con un solo `docker compose` la independencia de despliegue no aporta
nada real, y duplicar el contrato solo añadiría riesgo de divergencia silenciosa.

En producción sería un **paquete NuGet versionado**, para que cada servicio adoptara la
nueva versión del contrato cuando pudiera.
