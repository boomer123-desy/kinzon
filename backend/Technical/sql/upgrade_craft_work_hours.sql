-- MySQL / MariaDB：选择业务数据库后，在部署新增字段的服务前执行。
-- 历史记录默认为 0 小时、不报工；字段已存在时跳过，不覆盖已有配置。

SET @craft_upgrade_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Technical_NodeSettingTemplate' AND COLUMN_NAME = 'TheoreticalWorkHours'),
    'SELECT 1',
    'ALTER TABLE `Technical_NodeSettingTemplate` ADD COLUMN `TheoreticalWorkHours` decimal(12,4) NOT NULL DEFAULT 0 COMMENT ''理论加工工时（小时）''');
PREPARE craft_upgrade_statement FROM @craft_upgrade_sql;
EXECUTE craft_upgrade_statement;
DEALLOCATE PREPARE craft_upgrade_statement;

SET @craft_upgrade_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Technical_NodeSettingTemplate' AND COLUMN_NAME = 'IsReportWork'),
    'SELECT 1',
    'ALTER TABLE `Technical_NodeSettingTemplate` ADD COLUMN `IsReportWork` tinyint(1) NOT NULL DEFAULT 0 COMMENT ''是否报工''');
PREPARE craft_upgrade_statement FROM @craft_upgrade_sql;
EXECUTE craft_upgrade_statement;
DEALLOCATE PREPARE craft_upgrade_statement;

SET @craft_upgrade_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Technical_NodeSettingTemplate_Machine_RL' AND COLUMN_NAME = 'TheoreticalWorkHours'),
    'SELECT 1',
    'ALTER TABLE `Technical_NodeSettingTemplate_Machine_RL` ADD COLUMN `TheoreticalWorkHours` decimal(12,4) NOT NULL DEFAULT 0 COMMENT ''设备理论加工工时（小时）''');
PREPARE craft_upgrade_statement FROM @craft_upgrade_sql;
EXECUTE craft_upgrade_statement;
DEALLOCATE PREPARE craft_upgrade_statement;
