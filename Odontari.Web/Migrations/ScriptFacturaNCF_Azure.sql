-- ============================================================
-- Script: Facturación y NCF para Azure SQL Database
-- Tablas: NCFTipos, NCFRangos, NCFMovimientos, Facturas
-- Columnas nuevas en: Clinicas
-- Ejecutar en la base de datos de la aplicación (Azure SQL).
-- ============================================================
SET NOCOUNT ON;
BEGIN TRANSACTION;

-- ----- 1) Nuevas columnas en Clinicas -----
IF COL_LENGTH('dbo.Clinicas','RazonSocial') IS NULL
    ALTER TABLE dbo.Clinicas ADD RazonSocial nvarchar(300) NULL;
IF COL_LENGTH('dbo.Clinicas','RNC') IS NULL
    ALTER TABLE dbo.Clinicas ADD RNC nvarchar(20) NULL;
IF COL_LENGTH('dbo.Clinicas','NombreComercial') IS NULL
    ALTER TABLE dbo.Clinicas ADD NombreComercial nvarchar(200) NULL;
IF COL_LENGTH('dbo.Clinicas','DireccionFiscal') IS NULL
    ALTER TABLE dbo.Clinicas ADD DireccionFiscal nvarchar(500) NULL;
IF COL_LENGTH('dbo.Clinicas','LogoUrl') IS NULL
    ALTER TABLE dbo.Clinicas ADD LogoUrl nvarchar(500) NULL;
IF COL_LENGTH('dbo.Clinicas','ModoFacturacion') IS NULL
    ALTER TABLE dbo.Clinicas ADD ModoFacturacion int NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','PermitirInternaConFiscal') IS NULL
    ALTER TABLE dbo.Clinicas ADD PermitirInternaConFiscal bit NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','ItbisTasa') IS NULL
    ALTER TABLE dbo.Clinicas ADD ItbisTasa decimal(5,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','ItbisAplicarPorDefecto') IS NULL
    ALTER TABLE dbo.Clinicas ADD ItbisAplicarPorDefecto bit NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','MensajeFactura') IS NULL
    ALTER TABLE dbo.Clinicas ADD MensajeFactura nvarchar(500) NULL;
IF COL_LENGTH('dbo.Clinicas','CondicionesPago') IS NULL
    ALTER TABLE dbo.Clinicas ADD CondicionesPago nvarchar(500) NULL;
IF COL_LENGTH('dbo.Clinicas','NotaLegal') IS NULL
    ALTER TABLE dbo.Clinicas ADD NotaLegal nvarchar(1000) NULL;
IF COL_LENGTH('dbo.Clinicas','MostrarFirma') IS NULL
    ALTER TABLE dbo.Clinicas ADD MostrarFirma bit NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','MostrarQR') IS NULL
    ALTER TABLE dbo.Clinicas ADD MostrarQR bit NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','FormaPagoEfectivo') IS NULL
    ALTER TABLE dbo.Clinicas ADD FormaPagoEfectivo bit NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','FormaPagoTransferencia') IS NULL
    ALTER TABLE dbo.Clinicas ADD FormaPagoTransferencia bit NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','FormaPagoTarjeta') IS NULL
    ALTER TABLE dbo.Clinicas ADD FormaPagoTarjeta bit NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','FormaPagoCredito') IS NULL
    ALTER TABLE dbo.Clinicas ADD FormaPagoCredito bit NOT NULL DEFAULT 0;
IF COL_LENGTH('dbo.Clinicas','FormaPagoMixto') IS NULL
    ALTER TABLE dbo.Clinicas ADD FormaPagoMixto bit NOT NULL DEFAULT 0;

-- ----- 2) Tabla NCFTipos -----
IF OBJECT_ID('dbo.NCFTipos','U') IS NULL
BEGIN
    CREATE TABLE dbo.NCFTipos (
        Id int IDENTITY(1,1) NOT NULL,
        Codigo nvarchar(10) NOT NULL,
        Nombre nvarchar(100) NOT NULL,
        Descripcion nvarchar(200) NULL,
        RequiereRNCCliente bit NOT NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_NCFTipos PRIMARY KEY CLUSTERED (Id)
    );
END

-- ----- 3) Tabla Facturas -----
IF OBJECT_ID('dbo.Facturas','U') IS NULL
BEGIN
    CREATE TABLE dbo.Facturas (
        Id int IDENTITY(1,1) NOT NULL,
        ClinicaId int NOT NULL,
        NumeroInterno int NOT NULL,
        TipoDocumento int NOT NULL,
        NCFTipoId int NULL,
        NCF nvarchar(50) NULL,
        Estado int NOT NULL,
        FechaEmision datetime2 NOT NULL,
        Subtotal decimal(18,2) NOT NULL,
        Itbis decimal(18,2) NOT NULL,
        Total decimal(18,2) NOT NULL,
        PacienteId int NOT NULL,
        CitaId int NULL,
        OrdenCobroId int NULL,
        FormaPago nvarchar(50) NULL,
        Nota nvarchar(500) NULL,
        CreadoAt datetime2 NOT NULL,
        UsuarioId nvarchar(max) NULL,
        CONSTRAINT PK_Facturas PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Facturas_Clinicas_ClinicaId FOREIGN KEY (ClinicaId) REFERENCES dbo.Clinicas(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Facturas_NCFTipos_NCFTipoId FOREIGN KEY (NCFTipoId) REFERENCES dbo.NCFTipos(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Facturas_Pacientes_PacienteId FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Facturas_Citas_CitaId FOREIGN KEY (CitaId) REFERENCES dbo.Citas(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Facturas_OrdenesCobro_OrdenCobroId FOREIGN KEY (OrdenCobroId) REFERENCES dbo.OrdenesCobro(Id) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX IX_Facturas_ClinicaId ON dbo.Facturas(ClinicaId);
    CREATE NONCLUSTERED INDEX IX_Facturas_PacienteId ON dbo.Facturas(PacienteId);
    CREATE NONCLUSTERED INDEX IX_Facturas_CitaId ON dbo.Facturas(CitaId);
    CREATE UNIQUE NONCLUSTERED INDEX IX_Facturas_OrdenCobroId ON dbo.Facturas(OrdenCobroId) WHERE OrdenCobroId IS NOT NULL;
    CREATE NONCLUSTERED INDEX IX_Facturas_NCFTipoId ON dbo.Facturas(NCFTipoId);
END

-- ----- 4) Tabla NCFRangos -----
IF OBJECT_ID('dbo.NCFRangos','U') IS NULL
BEGIN
    CREATE TABLE dbo.NCFRangos (
        Id int IDENTITY(1,1) NOT NULL,
        ClinicaId int NOT NULL,
        NCFTipoId int NOT NULL,
        SeriePrefijo nvarchar(20) NULL,
        Desde nvarchar(50) NOT NULL,
        Hasta nvarchar(50) NOT NULL,
        Proximo bigint NOT NULL,
        FechaAutorizacion datetime2 NULL,
        FechaVencimiento datetime2 NULL,
        Estado int NOT NULL,
        Fuente nvarchar(20) NOT NULL,
        Nota nvarchar(500) NULL,
        CreadoAt datetime2 NOT NULL,
        CONSTRAINT PK_NCFRangos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_NCFRangos_Clinicas_ClinicaId FOREIGN KEY (ClinicaId) REFERENCES dbo.Clinicas(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_NCFRangos_NCFTipos_NCFTipoId FOREIGN KEY (NCFTipoId) REFERENCES dbo.NCFTipos(Id) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX IX_NCFRangos_ClinicaId ON dbo.NCFRangos(ClinicaId);
    CREATE NONCLUSTERED INDEX IX_NCFRangos_NCFTipoId ON dbo.NCFRangos(NCFTipoId);
END

-- ----- 5) Tabla NCFMovimientos -----
IF OBJECT_ID('dbo.NCFMovimientos','U') IS NULL
BEGIN
    CREATE TABLE dbo.NCFMovimientos (
        Id int IDENTITY(1,1) NOT NULL,
        ClinicaId int NOT NULL,
        NCFTipoId int NOT NULL,
        NCFGenerado nvarchar(50) NOT NULL,
        FacturaId int NULL,
        Estado int NOT NULL,
        UsuarioId nvarchar(450) NULL,
        FechaHora datetime2 NOT NULL,
        Motivo nvarchar(500) NULL,
        CONSTRAINT PK_NCFMovimientos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_NCFMovimientos_Clinicas_ClinicaId FOREIGN KEY (ClinicaId) REFERENCES dbo.Clinicas(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_NCFMovimientos_NCFTipos_NCFTipoId FOREIGN KEY (NCFTipoId) REFERENCES dbo.NCFTipos(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_NCFMovimientos_Facturas_FacturaId FOREIGN KEY (FacturaId) REFERENCES dbo.Facturas(Id) ON DELETE SET NULL,
        CONSTRAINT FK_NCFMovimientos_AspNetUsers_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX IX_NCFMovimientos_ClinicaId ON dbo.NCFMovimientos(ClinicaId);
    CREATE NONCLUSTERED INDEX IX_NCFMovimientos_NCFTipoId ON dbo.NCFMovimientos(NCFTipoId);
    CREATE NONCLUSTERED INDEX IX_NCFMovimientos_FacturaId ON dbo.NCFMovimientos(FacturaId);
    CREATE NONCLUSTERED INDEX IX_NCFMovimientos_UsuarioId ON dbo.NCFMovimientos(UsuarioId);
END

-- ----- 6) Seed NCFTipos (solo si la tabla está vacía) -----
IF EXISTS (SELECT 1 FROM dbo.NCFTipos)
    PRINT 'NCFTipos ya tiene datos; no se inserta seed.';
ELSE
BEGIN
    SET IDENTITY_INSERT dbo.NCFTipos ON;
    INSERT INTO dbo.NCFTipos (Id, Codigo, Nombre, Descripcion, RequiereRNCCliente, Activo) VALUES
        (1, N'B01', N'Crédito Fiscal', N'Comprobante para crédito fiscal', 1, 1),
        (2, N'B02', N'Consumo', N'Comprobante de consumo', 0, 1),
        (3, N'B14', N'Gubernamental', N'Comprobante gubernamental', 0, 1),
        (4, N'E31', N'Electrónico', N'Comprobante electrónico (e-CF)', 1, 1);
    SET IDENTITY_INSERT dbo.NCFTipos OFF;
    PRINT 'Seed NCFTipos insertado (B01, B02, B14, E31).';
END

COMMIT TRANSACTION;
PRINT 'Script Factura/NCF completado correctamente.';
