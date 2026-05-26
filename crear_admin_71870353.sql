-- Crear usuario admin con DNI 71870353 en todas las empresas activas
-- Ejecutar en: erp_kardex (BD local)

DECLARE @UsuarioId INT;

-- 1. Insertar usuario si no existe
IF NOT EXISTS (SELECT 1 FROM usuario WHERE dni = '71870353')
BEGIN
    INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
    VALUES ('71870353', 'Administrador', 'admin@corpsaf.com', '', 'password123', 1);
END

SET @UsuarioId = (SELECT id FROM usuario WHERE dni = '71870353');

-- 2. Asignar como administrador (tipo_usuario_id = 1) en todas las empresas activas
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
SELECT e.id, @UsuarioId, 1, 1
FROM empresa e
WHERE e.estado = 1
  AND NOT EXISTS (
      SELECT 1 FROM empresa_usuario eu
      WHERE eu.empresa_id = e.id AND eu.usuario_id = @UsuarioId
  );

-- 3. Verificar resultado
SELECT u.dni, u.nombre, e.ruc, e.razon_social, tu.nombre AS rol
FROM usuario u
JOIN empresa_usuario eu ON eu.usuario_id = u.id
JOIN empresa e ON e.id = eu.empresa_id
JOIN tipo_usuario tu ON tu.id = eu.tipo_usuario_id
WHERE u.dni = '71870353';
