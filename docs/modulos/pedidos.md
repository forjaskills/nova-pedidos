# Módulo Pedidos (nova-pedidos)

Ficha del módulo, acordada con el dev **antes** de escribir código. Es lo que el código tiene que cumplir y lo que
revisan las pruebas (`tests/…Unitarias/Pedidos/PedidoPruebas.cs` y `tests/…Integracion/PedidosPruebas.cs`).

| | |
|---|---|
| Qué es | Lo que compra un cliente en Nova Retail: una o más líneas con producto, cantidad y precio unitario. |
| Agregado | `Pedido` (raíz) con sus `LineaPedido` · número legible `PED-000123` (objeto valor `NumeroPedido`) |
| Campos | `Numero` (secuencia de la base, único) · `ClienteId` (el `sub` del token) · `Fecha` · `Estado` · `Total` (calculado) · líneas: `Producto` (1–100) · `Cantidad` (> 0) · `PrecioUnitario` (≥ 0, 2 decimales) |
| Estados | `Registrado → Pagado → Enviado → Entregado`, solo hacia adelante y de a uno · cancelar llega con su propio ticket |
| Reglas | al menos una línea (`pedido.sin-lineas`) · producto obligatorio, hasta 100 (`pedido.producto-invalido`) · cantidad > 0 (`pedido.cantidad-invalida`) · precio ≥ 0 con hasta 2 decimales (`pedido.precio-invalido`) · el total lo calcula el pedido, nunca viene de afuera · el estado solo avanza en orden; desde Entregado ya no (`pedido.no-avanza`, 409) |
| Quién | `cliente`: ve y crea **sus** pedidos (uno ajeno responde 404, como si no existiera) · `operador` y `admin`: ven todos y avanzan el estado |
| API | `GET /api/v1/pedidos?estado=&pagina=&tamano=` (máx. 50; filtro inválido → 400 `pedido.filtro-invalido`) · `GET /api/v1/pedidos/{numero}` · `POST /api/v1/pedidos` → 201 · `POST /api/v1/pedidos/{numero}/avanzar` (operador, admin; otro rol → 403) |
| Base | `bd/0001-modulo-pedidos.sql` (secuencia, `Pedidos`, `LineasPedido` con `CHECK`) · `bd/0002-pedidos-demo.sql` (3 de `cliente.demo`, solo DEV y QA) |
| Permisos | `pedidos.leer` y `pedidos.escribir` para cliente, operador y admin (`nova-seguridad/bd/0003-scopes-pedidos.sql`) |
