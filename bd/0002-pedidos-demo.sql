-- =============================================================================
-- 0002-pedidos-demo.sql
-- Proyecto  : nova-pedidos · módulo Pedidos (creado por la skill nuevo-modulo)
-- Qué hace  : 3 pedidos de DEMO de cliente.demo (Registrado, Pagado y Entregado) para probar a mano
-- Ambiente  : solo DEV y QA (en otra base, como PROD o _PRUEBAS, no inserta nada y lo dice)
-- Respaldo  : no requiere (solo inserta lo que falta)
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- cliente.demo en nova-seguridad (bd/0002-datos-semilla.sql)
DECLARE @ClienteDemo UNIQUEIDENTIFIER = '6f1c3a52-6a0e-4c8e-9a51-1a2b3c4d5e01';

IF DB_NAME() NOT LIKE N'%[_]DEV' AND DB_NAME() NOT LIKE N'%[_]QA'
BEGIN
    PRINT 'Omitido: la base ' + DB_NAME() + ' no es DEV ni QA; los pedidos demo no van aquí.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    PRINT 'Paso 1/2: pedidos demo de cliente.demo';
    DECLARE @Demo TABLE (Id UNIQUEIDENTIFIER, Estado NVARCHAR(20), Dias INT);
    INSERT INTO @Demo (Id, Estado, Dias) VALUES
        ('d3e0a1b2-0000-4000-8000-000000000001', N'Entregado',  10),
        ('d3e0a1b2-0000-4000-8000-000000000002', N'Pagado',      3),
        ('d3e0a1b2-0000-4000-8000-000000000003', N'Registrado',  0);

    -- Uno por uno, para que cada pedido tome su número de la secuencia en orden.
    DECLARE @Id UNIQUEIDENTIFIER, @Estado NVARCHAR(20), @Dias INT;
    DECLARE demo CURSOR LOCAL FAST_FORWARD FOR
        SELECT d.Id, d.Estado, d.Dias FROM @Demo d
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Pedidos p WHERE p.Id = d.Id)
        ORDER BY d.Dias DESC;
    OPEN demo;
    FETCH NEXT FROM demo INTO @Id, @Estado, @Dias;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO dbo.Pedidos (Id, Numero, ClienteId, Fecha, Estado)
        VALUES (@Id, NEXT VALUE FOR dbo.SecuenciaPedidos, @ClienteDemo, DATEADD(DAY, -@Dias, SYSDATETIMEOFFSET()), @Estado);
        FETCH NEXT FROM demo INTO @Id, @Estado, @Dias;
    END;
    CLOSE demo;
    DEALLOCATE demo;

    PRINT 'Paso 2/2: líneas de los pedidos demo';
    INSERT INTO dbo.LineasPedido (PedidoId, Producto, Cantidad, PrecioUnitario)
    SELECT v.PedidoId, v.Producto, v.Cantidad, v.PrecioUnitario
    FROM (VALUES
        (CAST('d3e0a1b2-0000-4000-8000-000000000001' AS UNIQUEIDENTIFIER), N'Café molido 500 g',  2,  8.50),
        (CAST('d3e0a1b2-0000-4000-8000-000000000001' AS UNIQUEIDENTIFIER), N'Taza de cerámica',   1, 12.00),
        (CAST('d3e0a1b2-0000-4000-8000-000000000002' AS UNIQUEIDENTIFIER), N'Audífonos Bluetooth', 1, 59.90),
        (CAST('d3e0a1b2-0000-4000-8000-000000000003' AS UNIQUEIDENTIFIER), N'Cuaderno A5',        3,  4.25),
        (CAST('d3e0a1b2-0000-4000-8000-000000000003' AS UNIQUEIDENTIFIER), N'Bolígrafo azul',    10,  0.80))
        AS v (PedidoId, Producto, Cantidad, PrecioUnitario)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.LineasPedido l WHERE l.PedidoId = v.PedidoId);

    -- Validación posterior: los 3 pedidos demo existen y todos tienen líneas.
    IF (SELECT COUNT(*) FROM dbo.Pedidos WHERE ClienteId = @ClienteDemo AND Id IN (SELECT Id FROM @Demo)) <> 3
       OR EXISTS (SELECT 1 FROM @Demo d WHERE NOT EXISTS (SELECT 1 FROM dbo.LineasPedido l WHERE l.PedidoId = d.Id))
        THROW 50002, 'Validación: faltan pedidos demo o alguno no tiene líneas.', 1;

    COMMIT;
    PRINT 'OK: 3 pedidos demo de cliente.demo listos.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH
