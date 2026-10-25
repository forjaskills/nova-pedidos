-- =============================================================================
-- 0001-modulo-pedidos.sql
-- Proyecto  : nova-pedidos · módulo Pedidos (creado por la skill nuevo-modulo)
-- Qué hace  : secuencia SecuenciaPedidos (PED-000123), tablas Pedidos y LineasPedido con sus CHECK e índices
-- Ambiente  : cualquiera (el ejecutor elige la base; aquí no hay USE)
-- Respaldo  : no requiere (solo crea lo que no existe)
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    PRINT 'Paso 1/4: secuencia SecuenciaPedidos';
    IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'SecuenciaPedidos' AND schema_id = SCHEMA_ID('dbo'))
        CREATE SEQUENCE dbo.SecuenciaPedidos AS BIGINT START WITH 1 INCREMENT BY 1 NO CACHE;

    PRINT 'Paso 2/4: tabla Pedidos';
    IF OBJECT_ID('dbo.Pedidos') IS NULL
        CREATE TABLE dbo.Pedidos (
            Id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Pedidos PRIMARY KEY,
            Numero    BIGINT           NOT NULL CONSTRAINT UQ_Pedidos_Numero UNIQUE
                                                CONSTRAINT CK_Pedidos_Numero CHECK (Numero > 0),
            ClienteId UNIQUEIDENTIFIER NOT NULL,
            Fecha     DATETIMEOFFSET   NOT NULL,
            Estado    NVARCHAR(20)     NOT NULL
                CONSTRAINT CK_Pedidos_Estado CHECK (Estado IN (N'Registrado', N'Pagado', N'Enviado', N'Entregado'))
        );

    PRINT 'Paso 3/4: tabla LineasPedido';
    IF OBJECT_ID('dbo.LineasPedido') IS NULL
        CREATE TABLE dbo.LineasPedido (
            Id             INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_LineasPedido PRIMARY KEY,
            PedidoId       UNIQUEIDENTIFIER   NOT NULL
                CONSTRAINT FK_LineasPedido_Pedidos REFERENCES dbo.Pedidos (Id) ON DELETE CASCADE,
            Producto       NVARCHAR(100)      NOT NULL
                CONSTRAINT CK_LineasPedido_Producto CHECK (LEN(Producto) >= 1),
            Cantidad       INT                NOT NULL
                CONSTRAINT CK_LineasPedido_Cantidad CHECK (Cantidad > 0),
            PrecioUnitario DECIMAL(18, 2)     NOT NULL
                CONSTRAINT CK_LineasPedido_PrecioUnitario CHECK (PrecioUnitario >= 0)
        );

    PRINT 'Paso 4/4: índices (los pedidos de un cliente, por estado y las líneas de un pedido)';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pedidos_ClienteId_Numero' AND object_id = OBJECT_ID('dbo.Pedidos'))
        CREATE INDEX IX_Pedidos_ClienteId_Numero ON dbo.Pedidos (ClienteId, Numero DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pedidos_Estado_Numero' AND object_id = OBJECT_ID('dbo.Pedidos'))
        CREATE INDEX IX_Pedidos_Estado_Numero ON dbo.Pedidos (Estado, Numero DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LineasPedido_PedidoId' AND object_id = OBJECT_ID('dbo.LineasPedido'))
        CREATE INDEX IX_LineasPedido_PedidoId ON dbo.LineasPedido (PedidoId);

    -- Validación posterior: lo creado existe.
    IF OBJECT_ID('dbo.Pedidos') IS NULL OR OBJECT_ID('dbo.LineasPedido') IS NULL
       OR NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'SecuenciaPedidos' AND schema_id = SCHEMA_ID('dbo'))
        THROW 50001, 'Validación: falta la secuencia o alguna tabla del módulo Pedidos.', 1;

    COMMIT;
    PRINT 'OK: módulo Pedidos listo en la base.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH
