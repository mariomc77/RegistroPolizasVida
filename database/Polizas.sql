DROP DATABASE IF EXISTS registro_polizas_vida;

CREATE DATABASE registro_polizas_vida
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE registro_polizas_vida;


CREATE TABLE lote_carga (
    id CHAR(36) NOT NULL,
    nombre_archivo_zip VARCHAR(255) NOT NULL,
    estado VARCHAR(30) NOT NULL,
    fecha_carga DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_inicio_procesamiento DATETIME NULL,
    fecha_fin_procesamiento DATETIME NULL,
    total_archivos INT NOT NULL DEFAULT 0,
    archivos_procesados INT NOT NULL DEFAULT 0,
    total_polizas INT NOT NULL DEFAULT 0,
    polizas_insertadas INT NOT NULL DEFAULT 0,
    polizas_actualizadas INT NOT NULL DEFAULT 0,
    polizas_con_error INT NOT NULL DEFAULT 0,

    PRIMARY KEY (id)
);


CREATE TABLE archivo_lote (
    id CHAR(36) NOT NULL,
    lote_carga_id CHAR(36) NOT NULL,
    nombre_archivo VARCHAR(255) NOT NULL,
    estado VARCHAR(30) NOT NULL,
    cantidad_polizas INT NOT NULL DEFAULT 0,
    polizas_insertadas INT NOT NULL DEFAULT 0,
    polizas_actualizadas INT NOT NULL DEFAULT 0,
    polizas_con_error INT NOT NULL DEFAULT 0,

    PRIMARY KEY (id),

    CONSTRAINT fk_archivo_lote_lote
        FOREIGN KEY (lote_carga_id)
        REFERENCES lote_carga(id)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);


CREATE TABLE poliza (
    numero_poliza VARCHAR(50) NOT NULL,
    tipo ENUM('Individual', 'Colectiva') NOT NULL,
    fecha_emision DATE NOT NULL,
    fecha_vencimiento DATE NOT NULL,
    monto_cobertura DECIMAL(15,2) NOT NULL,
    moneda VARCHAR(10) NOT NULL,

    lote_carga_origen_id CHAR(36) NULL,
    lote_carga_ultima_actualizacion_id CHAR(36) NULL,

    fecha_creacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_actualizacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    version_actualizacion INT NOT NULL DEFAULT 1,

    PRIMARY KEY (numero_poliza),

    CONSTRAINT fk_poliza_lote_origen
        FOREIGN KEY (lote_carga_origen_id)
        REFERENCES lote_carga(id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT fk_poliza_lote_actualizacion
        FOREIGN KEY (lote_carga_ultima_actualizacion_id)
        REFERENCES lote_carga(id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT chk_poliza_fechas
        CHECK (fecha_vencimiento > fecha_emision),

    CONSTRAINT chk_poliza_monto
        CHECK (monto_cobertura > 0)
);


CREATE TABLE tomador (
    id CHAR(36) NOT NULL,
    numero_poliza VARCHAR(50) NOT NULL,

    tipo_persona ENUM('Fisica', 'Juridica') NOT NULL,

    cedula VARCHAR(30) NULL,
    cedula_juridica VARCHAR(30) NULL,

    nombre VARCHAR(100) NULL,
    primer_apellido VARCHAR(100) NULL,
    segundo_apellido VARCHAR(100) NULL,

    razon_social VARCHAR(200) NULL,

    telefono VARCHAR(30) NULL,
    correo VARCHAR(150) NULL,
    direccion VARCHAR(300) NULL,

    PRIMARY KEY (id),

    UNIQUE KEY uk_tomador_poliza (numero_poliza),

    CONSTRAINT fk_tomador_poliza
        FOREIGN KEY (numero_poliza)
        REFERENCES poliza(numero_poliza)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT chk_tomador_identificacion
        CHECK (
            (tipo_persona = 'Fisica' AND cedula IS NOT NULL)
            OR
            (tipo_persona = 'Juridica' AND cedula_juridica IS NOT NULL)
        )
);


CREATE TABLE asegurado (
    id CHAR(36) NOT NULL,
    numero_poliza VARCHAR(50) NOT NULL,

    cedula VARCHAR(30) NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    primer_apellido VARCHAR(100) NOT NULL,
    segundo_apellido VARCHAR(100) NULL,

    fecha_nacimiento DATE NOT NULL,

    telefono VARCHAR(30) NULL,
    correo VARCHAR(150) NULL,

    PRIMARY KEY (id),

    UNIQUE KEY uk_asegurado_poliza_cedula (
        numero_poliza,
        cedula
    ),

    CONSTRAINT fk_asegurado_poliza
        FOREIGN KEY (numero_poliza)
        REFERENCES poliza(numero_poliza)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);


CREATE TABLE beneficiario (
    id CHAR(36) NOT NULL,
    asegurado_id CHAR(36) NOT NULL,

    tipo_persona ENUM('Fisica', 'Juridica') NOT NULL,

    cedula VARCHAR(30) NULL,
    cedula_juridica VARCHAR(30) NULL,

    nombre VARCHAR(100) NULL,
    primer_apellido VARCHAR(100) NULL,
    segundo_apellido VARCHAR(100) NULL,

    razon_social VARCHAR(200) NULL,

    porcentaje_beneficio DECIMAL(5,2) NOT NULL,

    telefono VARCHAR(30) NULL,
    correo VARCHAR(150) NULL,

    PRIMARY KEY (id),

    CONSTRAINT fk_beneficiario_asegurado
        FOREIGN KEY (asegurado_id)
        REFERENCES asegurado(id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT chk_beneficiario_porcentaje
        CHECK (
            porcentaje_beneficio > 0
            AND porcentaje_beneficio <= 100
        ),

    CONSTRAINT chk_beneficiario_identificacion
        CHECK (
            (tipo_persona = 'Fisica' AND cedula IS NOT NULL)
            OR
            (tipo_persona = 'Juridica' AND cedula_juridica IS NOT NULL)
        )
);


CREATE TABLE error_procesamiento (
    id CHAR(36) NOT NULL,
    archivo_lote_id CHAR(36) NOT NULL,

    numero_poliza VARCHAR(50) NULL,

    tipo VARCHAR(30) NOT NULL,
    mensaje VARCHAR(1000) NOT NULL,
    linea INT NULL,

    PRIMARY KEY (id),

    CONSTRAINT fk_error_archivo
        FOREIGN KEY (archivo_lote_id)
        REFERENCES archivo_lote(id)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);


CREATE INDEX idx_archivo_lote_lote
ON archivo_lote(lote_carga_id);

CREATE INDEX idx_poliza_lote_origen
ON poliza(lote_carga_origen_id);

CREATE INDEX idx_poliza_lote_actualizacion
ON poliza(lote_carga_ultima_actualizacion_id);

CREATE INDEX idx_asegurado_poliza
ON asegurado(numero_poliza);

CREATE INDEX idx_beneficiario_asegurado
ON beneficiario(asegurado_id);

CREATE INDEX idx_error_archivo
ON error_procesamiento(archivo_lote_id);


SHOW TABLES;