-- MySQL / MariaDB：先部署 Production_MBom 系列实体对应的数据表，再运行本脚本。
-- 历史工艺版本保持 NULL，首次保存或发布时由控制器创建并关联 BOM 版本。
-- 同一个 BOM 版本只能关联一个工艺版本；多条 NULL 不受唯一索引限制。

SET @craft_bom_upgrade_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Technical_CraftOrderVersion' AND COLUMN_NAME = 'BomVersionId'),
    'SELECT 1',
    'ALTER TABLE `Technical_CraftOrderVersion` ADD COLUMN `BomVersionId` bigint NULL COMMENT ''关联的制造BOM版本Id''');
PREPARE craft_bom_upgrade_statement FROM @craft_bom_upgrade_sql;
EXECUTE craft_bom_upgrade_statement;
DEALLOCATE PREPARE craft_bom_upgrade_statement;

SET @craft_bom_upgrade_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Technical_CraftOrderVersion' AND INDEX_NAME = 'IX_Technical_CraftOrderVersion_BomVersionId'),
    'SELECT 1',
    'CREATE UNIQUE INDEX `IX_Technical_CraftOrderVersion_BomVersionId` ON `Technical_CraftOrderVersion` (`BomVersionId`)');
PREPARE craft_bom_upgrade_statement FROM @craft_bom_upgrade_sql;
EXECUTE craft_bom_upgrade_statement;
DEALLOCATE PREPARE craft_bom_upgrade_statement;
