/* ============================================================================
   BaseDatos.sql — Prueba Técnica Devsu (.NET / Sector Financiero)
   Autora  : Xiomara Zapata Vásquez
   Motor   : Microsoft SQL Server 2022
   ----------------------------------------------------------------------------
   Crea las DOS bases de datos de la solución (una por microservicio):

     · CustomerDb  →  Customer Service   (Personas, Clientes)
     · AccountDb   →  Account Service    (Cuentas, Movimientos, réplica, eventos)

   El script es IDEMPOTENTE: puede ejecutarse varias veces sin duplicar datos
   ni fallar por objetos existentes.

   Ejecución:
     sqlcmd -S localhost,1433 -U sa -P "<password>" -C -i BaseDatos.sql
     o desde SSMS / Azure Data Studio con SQLCMD desactivado.

   NOTA sobre EF Core: el esquema se genera desde las migraciones
   (dotnet ef migrations script --idempotent). Este archivo incluye el registro
   en __EFMigrationsHistory para que la API no intente reaplicar la migración
   sobre una base ya creada por este script.
   ============================================================================ */

SET NOCOUNT ON;
GO

/* ============================================================================
   PARTE 1 — CustomerDb  (Customer Service)
   ============================================================================ */

IF DB_ID('CustomerDb') IS NULL
    CREATE DATABASE [CustomerDb];
GO

USE [CustomerDb];
GO

-- ---------------------------------------------------------------------------
-- Control de migraciones de EF Core
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.__EFMigrationsHistory') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId    NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Tabla base de la herencia TPT
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.Personas') IS NULL
BEGIN
    CREATE TABLE dbo.Personas (
        PersonaId       UNIQUEIDENTIFIER NOT NULL,
        Nombre          NVARCHAR(150)    NOT NULL,
        Genero          VARCHAR(10)      NOT NULL,
        Edad            INT              NOT NULL,
        Identificacion  VARCHAR(20)      NOT NULL,
        Direccion       NVARCHAR(200)    NOT NULL,
        Telefono        VARCHAR(20)      NOT NULL,

        CONSTRAINT PK_Personas PRIMARY KEY CLUSTERED (PersonaId),
        CONSTRAINT CK_Personas_Genero
            CHECK (Genero IN ('Masculino','Femenino','Otro')),
        CONSTRAINT CK_Personas_Edad
            CHECK (Edad >= 0 AND Edad <= 150),
        CONSTRAINT CK_Personas_Nombre
            CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0),
        CONSTRAINT CK_Personas_Identificacion
            CHECK (LEN(LTRIM(RTRIM(Identificacion))) >= 5)
    );

    -- Una persona no puede registrarse dos veces (I-01)
    CREATE UNIQUE NONCLUSTERED INDEX UX_Personas_Identificacion
        ON dbo.Personas (Identificacion);
END
GO

-- ---------------------------------------------------------------------------
-- Tabla derivada de la herencia TPT
-- La PK es a la vez FK hacia Personas: relación 1:1 obligatoria.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.Clientes') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        PersonaId       UNIQUEIDENTIFIER NOT NULL,
        ClienteId       VARCHAR(20)      NOT NULL,
        PasswordHash    VARCHAR(200)     NOT NULL,
        PasswordSalt    VARCHAR(100)     NOT NULL,
        Estado          BIT              NOT NULL CONSTRAINT DF_Clientes_Estado   DEFAULT (1),
        CreadoEn        DATETIME2(3)     NOT NULL CONSTRAINT DF_Clientes_CreadoEn DEFAULT (SYSUTCDATETIME()),
        ActualizadoEn   DATETIME2(3)     NULL,
        DesactivadoEn   DATETIME2(3)     NULL,

        CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (PersonaId),
        CONSTRAINT FK_Clientes_Personas_PersonaId FOREIGN KEY (PersonaId)
            REFERENCES dbo.Personas (PersonaId) ON DELETE CASCADE,

        -- Coherencia entre el estado y su fecha (RN-07)
        CONSTRAINT CK_Clientes_Desactivacion CHECK (
               (Estado = 1 AND DesactivadoEn IS NULL)
            OR (Estado = 0 AND DesactivadoEn IS NOT NULL)
        ),
        -- La contraseña jamás se guarda en claro: hash y salt son obligatorios (I-03)
        CONSTRAINT CK_Clientes_Credenciales CHECK (
            LEN(PasswordHash) >= 32 AND LEN(PasswordSalt) >= 16
        )
    );

    -- EF Core emite el filtro IS NOT NULL en índices únicos. Sobre una columna
    -- NOT NULL es funcionalmente idéntico; se replica para que este script y la
    -- migración produzcan exactamente el mismo esquema.
    CREATE UNIQUE NONCLUSTERED INDEX UX_Clientes_ClienteId
        ON dbo.Clientes (ClienteId)
        WHERE [ClienteId] IS NOT NULL;
END
GO

-- ---------------------------------------------------------------------------
-- Secuencia de ClienteId.
-- Arranca en 4 porque el seed ocupa CLI-0001, CLI-0002 y CLI-0003.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.SeqClienteId') IS NULL
    CREATE SEQUENCE dbo.SeqClienteId AS INT START WITH 4 INCREMENT BY 1;
GO

/* ----------------------------------------------------------------------------
   SEED — CustomerDb
   Datos del enunciado (Casos de Uso 1).

   ADVERTENCIA — datos no provistos por el enunciado e inventados para el seed:
   Genero, Edad e Identificacion. Se documenta en DECISIONS.md.

   Las contraseñas 1234 / 5678 / 1245 se almacenan como PBKDF2-HMAC-SHA256,
   100.000 iteraciones, salt de 16 bytes y clave derivada de 32 bytes,
   ambos en Base64.  NUNCA en texto plano.
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Clientes)
BEGIN
    INSERT INTO dbo.Personas
        (PersonaId, Nombre, Genero, Edad, Identificacion, Direccion, Telefono)
    VALUES
        ('019205a1-0001-7000-8000-000000000001', N'Jose Lema',          'Masculino', 35, '1712345678', N'Otavalo sn y principal',   '098254785'),
        ('019205a1-0001-7000-8000-000000000002', N'Marianela Montalvo', 'Femenino',  29, '0923456789', N'Amazonas y NNUU',          '097548965'),
        ('019205a1-0001-7000-8000-000000000003', N'Juan Osorio',        'Masculino', 42, '1804567890', N'13 junio y Equinoccial',   '098874587');

    INSERT INTO dbo.Clientes
        (PersonaId, ClienteId, PasswordHash, PasswordSalt, Estado, CreadoEn)
    VALUES
        ('019205a1-0001-7000-8000-000000000001', 'CLI-0001',
         'x5pyxrT7ryEiPc7V/TXeJajxvCjZRp3HomB8Xytn2J0=', 'ej8cjlsgTZeh5sBLPY8hWQ==', 1, '2022-02-01T00:00:00.000'),
        ('019205a1-0001-7000-8000-000000000002', 'CLI-0002',
         '2SYFs5GQexixZOfaJCK4HyDWVKMCziJganfyvB2mQvU=', 'LptNB8GjSG+1DX4pRqyBNw==', 1, '2022-02-01T00:00:00.000'),
        ('019205a1-0001-7000-8000-000000000003', 'CLI-0003',
         'O2pH5ri6pKmukgDnNKyhgpLOouFV1WDq/NUfkpegyYw=', 'xI1fGgt+QmOa2BL15wO8ag==', 1, '2022-02-01T00:00:00.000');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260904172423_InitialCreate')
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260904172423_InitialCreate', N'10.0.0');
GO

PRINT '>> CustomerDb lista.';
GO

/* ============================================================================
   PARTE 2 — AccountDb  (Account Service)
   ============================================================================ */

IF DB_ID('AccountDb') IS NULL
    CREATE DATABASE [AccountDb];
GO

USE [AccountDb];
GO

IF OBJECT_ID('dbo.__EFMigrationsHistory') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId    NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Réplica de clientes (read model alimentado por RabbitMQ).
-- El Account Service NO es dueño de estos datos: solo aplica eventos.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.ClientesReplica') IS NULL
BEGIN
    CREATE TABLE dbo.ClientesReplica (
        ClienteId       VARCHAR(20)   NOT NULL,
        Nombre          NVARCHAR(150) NOT NULL,
        Identificacion  VARCHAR(20)   NOT NULL,
        Estado          BIT           NOT NULL,
        ActualizadoEn   DATETIME2(3)  NOT NULL,

        CONSTRAINT PK_ClientesReplica PRIMARY KEY CLUSTERED (ClienteId)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Idempotencia del consumidor de eventos.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.EventosProcesados') IS NULL
BEGIN
    CREATE TABLE dbo.EventosProcesados (
        EventId     UNIQUEIDENTIFIER NOT NULL,
        TipoEvento  VARCHAR(100)     NOT NULL,
        ProcesadoEn DATETIME2(3)     NOT NULL CONSTRAINT DF_EventosProcesados_ProcesadoEn DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_EventosProcesados PRIMARY KEY CLUSTERED (EventId)
    );

    -- Soporta la purga periódica del histórico de eventos
    CREATE NONCLUSTERED INDEX IX_EventosProcesados_ProcesadoEn
        ON dbo.EventosProcesados (ProcesadoEn);
END
GO

-- ---------------------------------------------------------------------------
-- Cuentas — agregado raíz.
-- RowVersion habilita la concurrencia optimista (D9).
-- No hay FK hacia ClientesReplica: ver DECISIONS.md, sección de FK ausente.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.Cuentas') IS NULL
BEGIN
    CREATE TABLE dbo.Cuentas (
        CuentaId        UNIQUEIDENTIFIER NOT NULL,
        NumeroCuenta    VARCHAR(20)      NOT NULL,
        TipoCuenta      VARCHAR(15)      NOT NULL,
        SaldoInicial    DECIMAL(18,2)    NOT NULL,
        SaldoDisponible DECIMAL(18,2)    NOT NULL,
        Estado          BIT              NOT NULL CONSTRAINT DF_Cuentas_Estado   DEFAULT (1),
        ClienteId       VARCHAR(20)      NOT NULL,
        CreadoEn        DATETIME2(3)     NOT NULL CONSTRAINT DF_Cuentas_CreadoEn DEFAULT (SYSUTCDATETIME()),
        RowVersion      ROWVERSION       NOT NULL,

        CONSTRAINT PK_Cuentas PRIMARY KEY CLUSTERED (CuentaId),
        CONSTRAINT CK_Cuentas_TipoCuenta
            CHECK (TipoCuenta IN ('Ahorros','Corriente')),
        -- El saldo de apertura no puede ser negativo (I-06)
        CONSTRAINT CK_Cuentas_SaldoInicial
            CHECK (SaldoInicial >= 0),
        -- Última línea de defensa de "Saldo no disponible" (I-07)
        CONSTRAINT CK_Cuentas_SaldoDisponible
            CHECK (SaldoDisponible >= 0)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_Cuentas_NumeroCuenta
        ON dbo.Cuentas (NumeroCuenta);

    -- Índice cubriente para "cuentas del cliente X" (F4 y listados)
    CREATE NONCLUSTERED INDEX IX_Cuentas_ClienteId
        ON dbo.Cuentas (ClienteId)
        INCLUDE (NumeroCuenta, TipoCuenta, SaldoInicial, SaldoDisponible, Estado);
END
GO

-- ---------------------------------------------------------------------------
-- Movimientos — entidad interna del agregado Cuenta.
-- Inmutable: sin UPDATE ni DELETE por diseño (A17, I-12).
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.Movimientos') IS NULL
BEGIN
    CREATE TABLE dbo.Movimientos (
        MovimientoId    UNIQUEIDENTIFIER NOT NULL,
        CuentaId        UNIQUEIDENTIFIER NOT NULL,
        Fecha           DATETIME2(3)     NOT NULL,
        TipoMovimiento  VARCHAR(10)      NOT NULL,
        Valor           DECIMAL(18,2)    NOT NULL,
        Saldo           DECIMAL(18,2)    NOT NULL,
        RegistradoEn    DATETIME2(3)     NOT NULL CONSTRAINT DF_Movimientos_RegistradoEn DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Movimientos PRIMARY KEY CLUSTERED (MovimientoId),
        CONSTRAINT FK_Movimientos_Cuentas_CuentaId FOREIGN KEY (CuentaId)
            REFERENCES dbo.Cuentas (CuentaId) ON DELETE NO ACTION,

        CONSTRAINT CK_Movimientos_Tipo
            CHECK (TipoMovimiento IN ('Deposito','Retiro')),
        -- Un movimiento de valor cero no es un movimiento (I-09)
        CONSTRAINT CK_Movimientos_ValorNoCero
            CHECK (Valor <> 0),
        -- El saldo resultante nunca es negativo (I-07)
        CONSTRAINT CK_Movimientos_SaldoNoNegativo
            CHECK (Saldo >= 0),
        -- El tipo SIEMPRE concuerda con el signo del valor (I-10, A7)
        CONSTRAINT CK_Movimientos_TipoCoherente CHECK (
               (Valor > 0 AND TipoMovimiento = 'Deposito')
            OR (Valor < 0 AND TipoMovimiento = 'Retiro')
        )
    );

    -- Índice cubriente del estado de cuenta (F4): filtra por cuenta y rango de fechas
    CREATE NONCLUSTERED INDEX IX_Movimientos_CuentaId_Fecha
        ON dbo.Movimientos (CuentaId, Fecha DESC)
        INCLUDE (TipoMovimiento, Valor, Saldo);
END
GO

/* ----------------------------------------------------------------------------
   SEED — AccountDb
   Casos de Uso 2, 3 y 4 del enunciado.

   La réplica se precarga para que la solución sea demostrable desde el primer
   arranque, sin depender de que los eventos de RabbitMQ ya hayan viajado.

   ADVERTENCIA — las fechas de los movimientos no vienen todas en el enunciado.
   Se usan las del Caso de Uso 5 donde existen (08 y 10 de febrero de 2022) y
   fechas coherentes para las dos restantes.
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.ClientesReplica)
BEGIN
    INSERT INTO dbo.ClientesReplica (ClienteId, Nombre, Identificacion, Estado, ActualizadoEn)
    VALUES
        ('CLI-0001', N'Jose Lema',          '1712345678', 1, '2022-02-01T00:00:00.000'),
        ('CLI-0002', N'Marianela Montalvo', '0923456789', 1, '2022-02-01T00:00:00.000'),
        ('CLI-0003', N'Juan Osorio',        '1804567890', 1, '2022-02-01T00:00:00.000');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Cuentas)
BEGIN
    --                                                                          Saldo    Saldo
    --  CuentaId                               Numero   Tipo        Inicial  Disponible  Cliente
    INSERT INTO dbo.Cuentas
        (CuentaId, NumeroCuenta, TipoCuenta, SaldoInicial, SaldoDisponible, Estado, ClienteId, CreadoEn)
    VALUES
        ('019205a2-0001-7000-8000-000000000001', '478758', 'Ahorros',   2000.00, 1425.00, 1, 'CLI-0001', '2022-02-01T00:00:00.000'),
        ('019205a2-0001-7000-8000-000000000002', '225487', 'Corriente',  100.00,  700.00, 1, 'CLI-0002', '2022-02-01T00:00:00.000'),
        ('019205a2-0001-7000-8000-000000000003', '495878', 'Ahorros',      0.00,  150.00, 1, 'CLI-0003', '2022-02-01T00:00:00.000'),
        ('019205a2-0001-7000-8000-000000000004', '496825', 'Ahorros',    540.00,    0.00, 1, 'CLI-0002', '2022-02-01T00:00:00.000'),
        -- Caso de Uso 3: nueva cuenta corriente para Jose Lema, aún sin movimientos
        ('019205a2-0001-7000-8000-000000000005', '585545', 'Corriente', 1000.00, 1000.00, 1, 'CLI-0001', '2022-02-05T00:00:00.000');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Movimientos)
BEGIN
    INSERT INTO dbo.Movimientos
        (MovimientoId, CuentaId, Fecha, TipoMovimiento, Valor, Saldo)
    VALUES
        -- 496825 · retiro de 540  → 540 - 540 = 0        (Caso de Uso 5, 08/02/2022)
        ('019205a3-0001-7000-8000-000000000001', '019205a2-0001-7000-8000-000000000004',
         '2022-02-08T10:15:00.000', 'Retiro',   -540.00,    0.00),

        -- 478758 · retiro de 575  → 2000 - 575 = 1425
        ('019205a3-0001-7000-8000-000000000002', '019205a2-0001-7000-8000-000000000001',
         '2022-02-09T09:30:00.000', 'Retiro',   -575.00, 1425.00),

        -- 225487 · depósito de 600 → 100 + 600 = 700     (Caso de Uso 5, 10/02/2022)
        ('019205a3-0001-7000-8000-000000000003', '019205a2-0001-7000-8000-000000000002',
         '2022-02-10T11:45:00.000', 'Deposito', 600.00,  700.00),

        -- 495878 · depósito de 150 → 0 + 150 = 150
        ('019205a3-0001-7000-8000-000000000004', '019205a2-0001-7000-8000-000000000003',
         '2022-02-11T14:20:00.000', 'Deposito', 150.00,  150.00);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260904182203_InitialCreate')
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260904182203_InitialCreate', N'10.0.0');
GO

PRINT '>> AccountDb lista.';
GO

/* ============================================================================
   PARTE 3 — VERIFICACIÓN DE CONSISTENCIA
   Comprueba el invariante I-14:  SaldoDisponible = SaldoInicial + SUM(Valor)
   Debe devolver CERO filas.
   ============================================================================ */

USE [AccountDb];
GO

PRINT '';
PRINT '--- Verificación del invariante del saldo (0 filas = consistente) ---';

SELECT  c.NumeroCuenta,
        c.SaldoInicial,
        c.SaldoDisponible,
        c.SaldoInicial + ISNULL(SUM(m.Valor), 0) AS SaldoCalculado
FROM    dbo.Cuentas c
LEFT JOIN dbo.Movimientos m ON m.CuentaId = c.CuentaId
GROUP BY c.NumeroCuenta, c.SaldoInicial, c.SaldoDisponible
HAVING  c.SaldoDisponible <> c.SaldoInicial + ISNULL(SUM(m.Valor), 0);
GO

PRINT '';
PRINT '--- Resumen del seed ---';

SELECT 'Clientes'  AS Entidad, COUNT(*) AS Registros FROM CustomerDb.dbo.Clientes
UNION ALL SELECT 'Personas',      COUNT(*) FROM CustomerDb.dbo.Personas
UNION ALL SELECT 'Cuentas',       COUNT(*) FROM AccountDb.dbo.Cuentas
UNION ALL SELECT 'Movimientos',   COUNT(*) FROM AccountDb.dbo.Movimientos
UNION ALL SELECT 'ClientesRepl.', COUNT(*) FROM AccountDb.dbo.ClientesReplica;
GO

PRINT '';
PRINT '============================================================';
PRINT '  BaseDatos.sql ejecutado correctamente.';
PRINT '  CustomerDb : Personas, Clientes, SeqClienteId';
PRINT '  AccountDb  : Cuentas, Movimientos, ClientesReplica, EventosProcesados';
PRINT '============================================================';
GO
