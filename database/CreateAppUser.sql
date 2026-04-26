-- Run this ONCE as sysdba to create a dedicated app user with minimum privileges.
-- sqlplus / as sysdba @CreateAppUser.sql

CREATE USER obf_app IDENTIFIED BY "ChangeMe123!";
GRANT CREATE SESSION TO obf_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON system.OBF_USERS TO obf_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON system.OBF_DATA TO obf_app;
GRANT SELECT ON system.OBF_USERS_SEQ TO obf_app;
GRANT SELECT ON system.OBF_DATA_SEQ TO obf_app;
COMMIT;

-- After running this, update appsettings.json connection string to:
-- User Id=obf_app;Password=ChangeMe123!
