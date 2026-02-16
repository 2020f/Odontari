-- Ejecuta este script en tu base de datos si no puedes aplicar migraciones desde Visual Studio.
-- Añade la columna DuracionMinutos a la tabla Citas (equivalente a la migración AddDuracionMinutosCita).

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Citas') AND name = 'DuracionMinutos'
)
BEGIN
    ALTER TABLE dbo.Citas
    ADD DuracionMinutos INT NOT NULL CONSTRAINT DF_Citas_DuracionMinutos DEFAULT 30;
END
GO
