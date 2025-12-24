-- USE DB
use erp_kardex;

drop table if exists stock_almacen;
drop table if exists empresa;
drop table if exists sucursal;
drop table if exists almacen;
drop table if exists motivo;
drop table if exists centro_costo;
drop table if exists actividad;
drop table if exists moneda;
drop table if exists tipo_documento;
drop table if exists ingresosalidaalm;
drop table if exists dingresosalidaalm;
drop table if exists grupo;
drop table if exists subgrupo;
drop table if exists cuenta;
drop table if exists unidad_medida;
drop table if exists formulacion_quimica;
drop table if exists peligrosidad;
drop table if exists producto;
drop table if exists ingrediente_activo;
drop table if exists detalle_ingrediente_activo;
drop table if exists marca;
drop table if exists modelo;
drop table if exists estado;

create table estado (
	id INT IDENTITY(1,1) PRIMARY KEY,
	nombre varchar(255),
	tabla varchar(255)
);

create table empresa (
	id INT IDENTITY(1,1) PRIMARY KEY,
	ruc char(11),
	razon_social varchar(255),
	estado BIT,
);

create table sucursal (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	nombre varchar(255),
	estado BIT,
	empresa_id INT,
);

create table almacen (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo VARCHAR(255),
	nombre varchar(255),
	estado BIT,
	cod_sucursal varchar(255),
	empresa_id int
);

create table moneda (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	nombre varchar(255),
	estado BIT
);

create table motivo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	tipo_movimiento BIT, -- 1: INGRESO, 0: SALIDA
	descripcion VARCHAR(255),
	estado BIT
);

create table centro_costo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(20),
    nombre VARCHAR(255),
    empresa_id INT,
    padre_id INT,
    es_imputable BIT DEFAULT 1,
    estado BIT DEFAULT 1,
    fecha_registro DATETIME DEFAULT GETDATE(),
);

create table actividad (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	nombre varchar(255),
	estado BIT,
);

create table tipo_documento (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	descripcion varchar(max),
	estado BIT
);

create table ingresosalidaalm (
	id INT IDENTITY(1,1) PRIMARY KEY,
	fecha DATE,
	numero varchar(255),
	sucursal_id INT,
	almacen_id INT,
	tipo_movimiento BIT,
	motivo_id INT,
	fecha_documento DATE,
	tipo_documento_id int,
	serie_documento varchar(255),
	numero_documento varchar(255),
	moneda_id int,
	estado_id int,
	usuario_id INT,
	fecha_registro DATETIME DEFAULT GETDATE()
);

create table dingresosalidaalm (
	id INT IDENTITY(1,1) PRIMARY KEY,
	ingresosalidaalm_id INT,
	item varchar(255),
	cod_producto varchar(255),
	descripcion_producto varchar(255),
	cod_unidad_medida varchar(255),
	cantidad decimal(12,2),
	tipo_documento_id int,
	fecha_documento DATE,
	serie_documento varchar(255),
	numero_documento varchar(255),
	moneda_id int,
	tipo_cambio decimal(12,6),
	precio decimal(19,6),
	igv decimal(19,6),
	subtotal decimal(19,6),
	total decimal(19,6),
	centro_costo_id int,
	actividad_id int,
	usuario_id INT,
	fecha_registro DATETIME DEFAULT GETDATE()
);

create table cuenta (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200)
);

create table grupo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	descripcion varchar(200),
	cuenta_id varchar(255)
);

create table subgrupo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	descripcion varchar(200),
	grupo_id INT,
	cod_grupo varchar(255),
	descripcion_grupo varchar(255),
	observacion varchar(255)
);

create table unidad_medida (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200)
);

create table formulacion_quimica (
	codigo varchar(255) PRIMARY KEY,
	nombre varchar(255),
	descripcion varchar(255),
	ejemplo varchar (255)
);

create table peligrosidad (
	codigo varchar(255) PRIMARY KEY,
	clase varchar(255),
	banda_color varchar(255),
	descripcion varchar(255),
	nivel_riesgo varchar(255),
	uso_senasa BIT
);

create table marca (
	id INT IDENTITY(1,1) PRIMARY KEY,
	nombre varchar(255),
	estado BIT
);

create table modelo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	nombre varchar(255),
	estado BIT,
	marca_id INT
);

create table producto (
	codigo varchar(255) PRIMARY KEY,
	grupo_id INT,
	cod_grupo varchar(255),
	descripcion_grupo varchar(255),
	subgrupo_id INT,
	cod_subgrupo varchar(255),
	descripcion_subgrupo varchar(255),
	descripcion_producto varchar(255),
	descripcion_comercial varchar(255),
	concentracion decimal(12,2),
	cod_formulacion_quimica varchar(255),
	lote varchar(255),
	fecha_fabricacion date,
	fecha_vencimiento date,
	cod_peligrosidad varchar(255),
	cod_unidad_medida varchar(255),
	marca_id INT,
	modelo_id INT,
	serie varchar(255),
	es_activo_fijo BIT,
	estado BIT, -- 1: activo 0: inactivo
	empresa_id INT
);

create table ingrediente_activo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	descripcion varchar(255)
);

create table detalle_ingrediente_activo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	cod_producto varchar(255),
	ingrediente_activo_id int,
	porcentaje decimal(12,2)
);

CREATE TABLE stock_almacen (
    id INT IDENTITY(1,1) PRIMARY KEY,
    almacen_id INT NOT NULL,
    cod_producto VARCHAR(255) NOT NULL,
    stock_actual DECIMAL(12,2) DEFAULT 0,
    ultima_actualizacion DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Stock_Almacen UNIQUE (almacen_id, cod_producto)
);

-- inserts de 'unidad_medida'
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('4A','BOBINAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BJ','BALDE');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BLL','BARRILES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BG','BOLSA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BO','BOTELLAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BX','CAJA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CT','CARTONES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CMK','CENTIMETRO CUADRADO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CMQ','CENTIMETRO CUBICO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CMT','CENTIMETRO LINEAL');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CEN','CIENTO DE UNIDADES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CY','CILINDRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CJ','CONOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('DZN','DOCENA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('DZP','DOCENA POR 10**6');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BE','FARDO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('GLI','GALON INGLES (4,545956L)');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('GRM','GRAMO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('GRO','GRUESA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('HLT','HECTOLITRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('LEF','HOJA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('SET','JUEGO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('KGM','KILOGRAMO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('KTM','KILOMETRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('KWH','KILOVATIO HORA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('KT','KIT');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CA','LATAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('LBR','LIBRAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('LTR','LITRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MWH','MEGAWATT HORA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MTR','METRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MTK','METRO CUADRADO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MTQ','METRO CUBICO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MGM','MILIGRAMOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MLT','MILILITRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MMT','MILIMETRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MMK','MILIMETRO CUADRADO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MMQ','MILIMETRO CUBICO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MLL','MILLARES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('UM','MILLON DE UNIDADES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('ONZ','ONZAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('PF','PALETAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('PK','PAQUETE');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('PR','PAR');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('FOT','PIES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('FTK','PIES CUADRADOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('FTQ','PIES CUBICOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('C62','PIEZAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('PG','PLACAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('ST','PLIEGO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('INH','PULGADAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('RM','RESMA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('DR','TAMBOR');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('STN','TONELADA CORTA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('LTN','TONELADA LARGA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('TNE','TONELADAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('TU','TUBOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('NIU','UNIDAD (BIENES)');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('ZZ','UNIDAD (SERVICIOS)');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('GLL','US GALON (3,7843 L)');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('YRD','YARDA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('YDK','YARDA CUADRADA');

-- inserts de 'cuenta'
INSERT INTO cuenta (codigo, descripcion) VALUES ('21', 'MERCADERÍAS');
INSERT INTO cuenta (codigo, descripcion) VALUES ('24', 'MATERIALES SUMINISTROS Y REPUESTOS');
INSERT INTO cuenta (codigo, descripcion) VALUES ('33', 'ACTIVOS');
INSERT INTO cuenta (codigo, descripcion) VALUES ('63', 'SERVICIOS');

-- inserts de 'peligrosidad'
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-IA','IA','ROJO INTENSO','EXTREMADAMENTE PELIGROSO','MUY ALTO',1);
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-IB','IB','ROJO','ALTAMENTE PELIGROSO','ALTO',1);
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-II','II','AMARILLO','MODERADAMENTE PELIGROSO','MEDIO',1);
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-III','III','AZUL','LIGERAMENTE PELIGROSO','BAJO',1);
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-U','U','VERDE','IMPROBABLE QUE PRESENTE PELIGRO','MUY BAJO',1);

-- inserts de 'formulacion_quimica'
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('EC','EMULSIFIABLE CONCENTRATE','INGREDIENTE ACTIVO DISUELTO EN SOLVENTE ORGÁNICO + EMULSIFICANTES','CLORPIRIFOS 48% EC');
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('SC','SUSPENSION CONCENTRATE','SÓLIDOS FINOS SUSPENDIDOS EN AGUA','IMIDACLOPRID 35% SC');
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('SL','SOLUBLE LIQUID','INGREDIENTE ACTIVO TOTALMENTE SOLUBLE EN AGUA','GLIFOSATO 48% SL');
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('EW','EMULSION, OIL IN WATER','EMULSIÓN ACEITE EN AGUA (MENOS SOLVENTE)','PIRETROIDES EW');
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('CS','CAPSULE SUSPENSION','MICROCÁPSULAS SUSPENDIDAS','LAMBDA-CIHALOTRINA CS');

-- inserts de 'empresa'
INSERT INTO empresa (ruc, razon_social, estado) VALUES ('20607778338', 'CONTROL SCIENCE DEL PERU S.A.C.', 1);
INSERT INTO empresa (ruc, razon_social, estado) VALUES ('20603727551', 'STALNO S.A.C.', 1);

-- inserts de 'sucursal'
INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('001', 'PRINCIPAL - POMALCA', 1, 1);
--INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('002', 'SUCURSAL - MORROPE', 1, 1);
--INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('002', 'PRINCIPAL - CHICLAYO', 1, 2);
--INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('002', 'SUCURSAL - MORROPE', 1, 2);

-- inserts de 'almacen'
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('01','PRINCIPAL',1,'001',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','TERCEROS',1,'001',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','PRODUCTO TERMIANDO',1,'001',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('03','MERMAS Y DESPERDICIOS',1,'001',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('04','ENVASES Y EMBALAJES',1,'001',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('05','MATERIALES Y AUXILIARES',1,'001',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('01','PRINCIPAL',1,'002',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','PRODUCTO TERMIANDO',1,'002',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('03','MERMAS Y DESPERDICIOS',1,'002',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('04','ENVASES Y EMBALAJES',1,'002',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('05','MATERIALES Y AUXILIARES',1,'002',1);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('01','PRINCIPAL',1,'001',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','MERCADERIAS',1,'001',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('03','REPUESTOS',1,'001',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('04','SISTEMA DE RIEGO',1,'001',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('05','MAQUINARIA',1,'001',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('06','MATERIALES DE CONSTRUCCION',1,'001',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('07','EQUIPOS DE PROTECCION',1,'001',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('01','PRINCIPAL',1,'002',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','MERCADERIAS',1,'002',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('03','REPUESTOS',1,'002',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('04','SISTEMA DE RIEGO',1,'002',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('05','MAQUINARIA',1,'002',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('06','MATERIALES DE CONSTRUCCION',1,'002',2);
--INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('07','EQUIPOS DE PROTECCION',1,'002',2);

-- inserts de 'tipo_documento'
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('01','Factura',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('02','Recibo por Honorarios',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('03','Boleta de Venta',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('04','Liquidación de compra',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('05','Boletos de Transporte Aéreo',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('06','Carta de porte aéreo por el servicio de transporte de carga aérea',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('07','Nota de crédito',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('08','Nota de débito',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('09','Guía de remisión - Remitente',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('10','Recibo por Arrendamiento',1);

-- inserts de 'moneda'
INSERT INTO moneda (codigo, nombre, estado) VALUES ('01', 'SOLES', 1);
INSERT INTO moneda (codigo, nombre, estado) VALUES ('02', 'DÓLARES', 1);

-- inserts de 'motivo'
-- tipo_movimiento: 1 - ENTRADAS
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('02',1,'COMPRA NACIONAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('03',1,'CONSIGNACIÓN RECIBIDA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('05',1,'DEVOLUCIÓN RECIBIDA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('07',1,'BONIFICACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('08',1,'PREMIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('09',1,'DONACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('16',1,'SALDO INICIAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('18',1,'IMPORTACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('19',1,'ENTRADA DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('20',1,'ENTRADA POR DEVOLUCIÓN DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('21',1,'ENTRADA POR TRANSFERENCIA ENTRE ALMACENES ',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('22',1,'ENTRADA POR IDENTIFICACIÓN ERRONEA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('24',1,'ENTRADA POR DEVOLUCIÓN DEL CLIENTE',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('26',1,'ENTRADA PARA SERVICIO DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('28',1,'AJUSTE POR DIFERENCIA DE INVENTARIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('29',1,'ENTRADA DE BIENES EN PRÉSTAMO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('31',1,'ENTRADA DE BIENES EN CUSTODIA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('33',1,'MUESTRAS MÉDICAS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('34',1,'PUBLICIDAD',1);
-- tipo_movimiento: 0 - SALIDAS
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('01',0,'VENTA NACIONAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('04',0,'CONSIGNACIÓN ENTREGADA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('06',0,'DEVOLUCIÓN ENTREGADA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('07',0,'BONIFICACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('08',0,'PREMIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('09',0,'DONACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('10',0,'SALIDA A PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('11',0,'SALIDA POR TRANSFERENCIA ENTRE ALMACENES ',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('12',0,'RETIRO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('13',0,'MERMAS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('14',0,'DESMEDROS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('15',0,'DESTRUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('17',0,'EXPORTACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('23',0,'SALIDA POR IDENTIFICACIÓN ERRONEA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('25',0,'SALIDA POR DEVOLUCIÓN AL PROVEEDOR',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('27',0,'SALIDA POR SERVICIO DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('28',0,'AJUSTE POR DIFERENCIA DE INVENTARIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('30',0,'SALIDA DE BIENES EN PRÉSTAMO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('32',0,'SALIDA DE BIENES EN CUSTODIA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('33',0,'MUESTRAS MÉDICAS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('34',0,'PUBLICIDAD',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('36',0,'RETIRO PARA ENTREGA A TRABAJADORES',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('37',0,'RETIRO POR CONVENIO COLECTIVO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('38',0,'RETIRO POR SUSTITUCIÓN DE BIEN SINIESTRADO',1);


-- inserts de 'estado'
INSERT INTO estado (nombre, tabla) VALUES ('Aprobado', 'INGRESOSALIDAALM');

-- inserts de 'centro_costo'
-- 1. NIVEL 1: Áreas Generales (Padres - No Imputables)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES 
--('ADM', 'ADMINISTRACIÓN CENTRAL', 1, NULL, 0, 1),      -- ID 1
--('OP-AGRO', 'OPERACIONES AGRÍCOLAS', 1, NULL, 0, 1),   -- ID 2
--('PROD', 'PLANTA DE PRODUCCIÓN', 1, NULL, 0, 1),       -- ID 3
--('COM', 'COMERCIAL Y VENTAS', 1, NULL, 0, 1);          -- ID 4

INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('001', 'RECEPCIÓN', 1, NULL, 1, 1),
('002', 'PROCESAMIENTO', 1, NULL, 1, 1),
('003', 'ENVASADO', 1, NULL, 1, 1);

-- 2. NIVEL 2: Sub-áreas (Hijos - Sí Imputables)
-- Hijos de ADMINISTRACIÓN (ID 1)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES 
--('ADM-RH', 'RECURSOS HUMANOS', 1, 1, 1, 1),
--('ADM-CON', 'CONTABILIDAD Y FINANZAS', 1, 1, 1, 1),
--('ADM-LOG', 'LOGÍSTICA Y COMPRAS', 1, 1, 1, 1);

---- Hijos de OPERACIONES AGRÍCOLAS (ID 2)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES 
--('FND-01', 'FUNDO SAN JORGE', 1, 2, 1, 1),
--('FND-02', 'FUNDO EL ALAMO', 1, 2, 1, 1),
--('MAQ-AGR', 'FLOTA DE TRACTORES', 1, 2, 1, 1);

---- Hijos de PLANTA DE PRODUCCIÓN (ID 3)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES 
--('PLT-L01', 'LÍNEA DE PROCESO 1', 1, 3, 1, 1),
--('PLT-MAN', 'MANTENIMIENTO PLANTA', 1, 3, 1, 1),
--('PLT-ALM', 'ALMACÉN DE INSUMOS', 1, 3, 1, 1);

---- Hijos de COMERCIAL (ID 4)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES 
--('VTAS-NAC', 'VENTAS NACIONALES', 1, 4, 1, 1),
--('VTAS-EXP', 'EXPORTACIONES', 1, 4, 1, 1);

-- inserts de 'actividad'
INSERT INTO actividad (codigo, nombre, estado) VALUES 
('001', 'RECEPCIÓN Y ALMACENAMIENTO', 1),
('002', 'PASAJE Y DOSIFICACIÓN', 1),
('003', 'MEZCLADO', 1),
('004', 'ENVASADO', 1),
('005', 'ETIQUETADO', 1),
('006', 'DESPACHO', 1),
('007', 'GESTIÓN OPERATIVA', 1);