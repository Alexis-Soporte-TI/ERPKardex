-- ============================================================
-- SCRIPT: Módulo de Alertas de Vencimiento de Stock
-- Compatible con HeidiSQL (sin GO, sin BEGIN/END)
-- Ejecutar cada bloque por separado si persiste el error
-- ============================================================

IF OBJECT_ID('config_alerta_vencimiento', 'U') IS NULL
    CREATE TABLE config_alerta_vencimiento (
        id             INT IDENTITY(1,1) PRIMARY KEY,
        empresa_id     INT NOT NULL,
        meses_critico  INT NOT NULL CONSTRAINT DF_cav_critico DEFAULT 18,
        meses_alerta   INT NOT NULL CONSTRAINT DF_cav_alerta  DEFAULT 24,
        fecha_registro DATETIME CONSTRAINT DF_cav_fecha DEFAULT GETDATE(),
        CONSTRAINT FK_cav_empresa FOREIGN KEY (empresa_id) REFERENCES empresa(id)
    );

IF OBJECT_ID('correo_alerta_stock', 'U') IS NULL
    CREATE TABLE correo_alerta_stock (
        id             INT IDENTITY(1,1) PRIMARY KEY,
        empresa_id     INT NOT NULL,
        nombre         NVARCHAR(150) NOT NULL,
        email          NVARCHAR(200) NOT NULL,
        frecuencia     NVARCHAR(20)  NOT NULL CONSTRAINT DF_cas_frec   DEFAULT 'SEMANAL',
        dia_envio      INT           NOT NULL CONSTRAINT DF_cas_dia    DEFAULT 1,
        activo         BIT           NOT NULL CONSTRAINT DF_cas_activo DEFAULT 1,
        fecha_registro DATETIME CONSTRAINT DF_cas_fecha DEFAULT GETDATE(),
        CONSTRAINT FK_cas_empresa FOREIGN KEY (empresa_id) REFERENCES empresa(id),
        CONSTRAINT CHK_cas_frec   CHECK (frecuencia IN ('SEMANAL', 'BISEMANAL')),
        CONSTRAINT CHK_cas_dia    CHECK (dia_envio BETWEEN 1 AND 7)
    );
