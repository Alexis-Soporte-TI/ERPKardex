-- USE DB
use erp_kardex;

drop table if exists empresa;
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

create table empresa (
	id INT IDENTITY(1,1) PRIMARY KEY,
	ruc char(11),
	razon_social varchar(255),
	estado BIT,
);

create table cuenta (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200)
);

create table grupo (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200),
	cuenta_id varchar(255)
);

create table subgrupo (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200),
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
	cod_grupo varchar(255),
	descripcion_grupo varchar(255),
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
	cantidad decimal(12,2),
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
