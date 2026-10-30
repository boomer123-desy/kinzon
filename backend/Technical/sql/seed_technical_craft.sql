-- =====================================================================
-- 标准工序单模块 初始化演示数据（MariaDB）
-- 涉及表：
--   1. Technical_CraftOrder                       标准工序单
--   2. Technical_CraftOrderVersion                工序单版本（V1废弃/V2主版本/V3草稿）
--   3. Technical_NodeSettingTemplate              工序明细（挂版本下，Sequence 10/20/30…）
--   4. Technical_NodeSettingTemplate_Machine_RL   工序 - 设备 关联
--   5. Technical_NodeSettingTemplate_Material_RL  工序 - 物料 关联
--
-- ⚠️ 使用前注意：
--   a) 以下所有【外键】列当前都是占位值 0 / NULL，跑完后请自行 UPDATE 成真实 ID：
--        Technical_CraftOrder.ProductId / SpecificationId
--        Technical_CraftOrderVersion.WorkflowTemplateId  ⚠️表上有唯一索引 IX_..._WorkflowTemplateId，
--              多个版本必须互不相同（脚本用 9001/9002/9003 占位），建议后续删除该唯一索引
--        Technical_NodeSettingTemplate.NodeTemplateId / CraftId
--        Technical_NodeSettingTemplate_Machine_RL.MachineId
--        Technical_NodeSettingTemplate_Material_RL.DataId / UnitId /
--            UnitConversionId / InsteadSourceId / RefCraftOrderVersionId
--   b) Material_RL 的 DataType / IOType 为枚举 int，数值须按你代码里的枚举定义核对后替换
--      （脚本里示例默认：DataType=0 原料、IOType=0 投入 / 1 产出，请以真实枚举为准）
--   c) 版本状态 Status 按枚举 CraftOrderVersionStatus：0=待审核 1=已通过 2=已废弃
--   d) EnterpriseId（租户/企业）已统一赋值 1
--   e) 审计列（CreateTime/UpdateTime/CreateUserName）若你的表不存在或列名不同，请整列删除后执行
--   f) Id 使用 9xxx 高位段，避免与业务自动增长冲突；如需重跑请先执行文件顶部的 DELETE 段
-- =====================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;   -- 关闭外键校验，允许占位外键插入；跑完在文件末尾重新打开

-- ---------------------------------------------------------------------
-- [可选] 清空重跑（子表先删，父表后删）
-- ---------------------------------------------------------------------
-- DELETE FROM Technical_NodeSettingTemplate_Material_RL;
-- DELETE FROM Technical_NodeSettingTemplate_Machine_RL;
-- DELETE FROM Technical_NodeSettingTemplate;
-- DELETE FROM Technical_CraftOrderVersion;
-- DELETE FROM Technical_CraftOrder;

-- =====================================================================
-- 1. 标准工序单 Technical_CraftOrder
-- =====================================================================
INSERT INTO Technical_CraftOrder
    (Id, Code, Name, ProductId, SpecificationId, Description, IsDeleted, EnterpriseId, CreateTime, UpdateTime, CreateUserName)
VALUES
    (9001, 'CRAFT-0001', '示例成品-标准工艺单',
     0,            -- ⚠️ ProductId 待填：替换为真实 Base_Product.Id
     NULL,         -- SpecificationId 可空：替换为真实 Base_ProductSpecification.Id
     '初始化示例：演示 工序单/版本/工序 联动。',
     0, 1, '2026-09-01 09:00:00', '2026-09-01 09:00:00', 'admin');

-- =====================================================================
-- 2. 工序单版本 Technical_CraftOrderVersion
--    同一 CraftOrderId 只能有一个 IsMain=1（过滤唯一索引）
-- =====================================================================
INSERT INTO Technical_CraftOrderVersion
    (Id, CraftOrderId, SequenceNo, Version, IsMain, Status, Remark, IsDeleted, WorkflowTemplateId, EnterpriseId, CreateTime, UpdateTime, CreateUserName)
VALUES
    -- V1 已废弃
    (9101, 9001, 1, 'V1', 0, 2, 'V1：已废弃，被 V2 替代', 0,
     9001, 1, '2026-09-01 09:05:00', '2026-09-01 09:05:00', 'admin'),  -- WorkflowTemplateId 9001 占位待填
    -- V2 当前主版本（已发布）
    (9102, 9001, 2, 'V2', 1, 1, 'V2：当前主版本（已发布）', 0,
     9002, 1, '2026-09-01 09:10:00', '2026-09-01 09:10:00', 'admin'),  -- WorkflowTemplateId 9002 占位待填
    -- V3 草稿（待审核，可编辑测试）
    (9103, 9001, 3, 'V3', 0, 0, 'V3：草稿（待审核，测试编辑）', 0,
     9003, 1, '2026-09-01 09:15:00', '2026-09-01 09:15:00', 'admin');  -- WorkflowTemplateId 9003 占位待填

-- =====================================================================
-- 3. 工序明细 Technical_NodeSettingTemplate
--    工序编码/名称按版本内 Sequence 10/20/30 递增
-- =====================================================================
INSERT INTO Technical_NodeSettingTemplate
    (Id, CraftOrderVersionId, Sequence, Code, Name, IsOutsource, IsInbound, Description,
     IsDeleted, EnterpriseId, NodeTemplateId, CraftId, CreateTime, UpdateTime, CreateUserName)
VALUES
    -- ---- V1(9101) 的工序 ----
    (9201, 9101, 10, 'OP10', '下料', 0, 0, 'V1 下料工序',
     0, 1, 0, NULL, '2026-09-01 09:20:00', '2026-09-01 09:20:00', 'admin'),   -- NodeTemplateId 待填 / CraftId 可空
    (9202, 9101, 20, 'OP20', '机加工', 0, 0, NULL,
     0, 1, 0, NULL, '2026-09-01 09:20:00', '2026-09-01 09:20:00', 'admin'),
    (9203, 9101, 30, 'OP30', '检验入库', 0, 1, NULL,
     0, 1, 0, NULL, '2026-09-01 09:20:00', '2026-09-01 09:20:00', 'admin'),

    -- ---- V2(9102) 的工序（主版本，较完整，含外协/入库工序）----
    (9211, 9102, 10, 'OP10', '下料', 0, 0, NULL,
     0, 1, 0, NULL, '2026-09-01 09:30:00', '2026-09-01 09:30:00', 'admin'),
    (9212, 9102, 20, 'OP20', 'CNC 精加工', 0, 0, '挂 2 台设备见 Machine_RL',
     0, 1, 0, NULL, '2026-09-01 09:30:00', '2026-09-01 09:30:00', 'admin'),
    (9213, 9102, 30, 'OP30', '表面处理(外协)', 1, 0, '外协工序示例',
     0, 1, 0, NULL, '2026-09-01 09:30:00', '2026-09-01 09:30:00', 'admin'),
    (9214, 9102, 40, 'OP40', '检验包装入库', 0, 1, '完工入库工序',
     0, 1, 0, NULL, '2026-09-01 09:30:00', '2026-09-01 09:30:00', 'admin'),

    -- ---- V3(9103) 的工序（草稿，正在编辑）----
    (9221, 9103, 10, 'OP10', '下料', 0, 0, NULL,
     0, 1, 0, NULL, '2026-09-01 09:40:00', '2026-09-01 09:40:00', 'admin'),
    (9222, 9103, 20, 'OP20', 'CNC 精加工', 0, 0, '草稿阶段',
     0, 1, 0, NULL, '2026-09-01 09:40:00', '2026-09-01 09:40:00', 'admin');

-- =====================================================================
-- 4. 工序 - 设备 关联 Technical_NodeSettingTemplate_Machine_RL
--    外键列名是 NodeSettingTemplateId
-- =====================================================================
INSERT INTO Technical_NodeSettingTemplate_Machine_RL
    (Id, MachineId, NodeSettingTemplateId, IsDeleted, EnterpriseId, CreateTime, UpdateTime, CreateUserName)
VALUES
    -- V2 的 CNC 精加工(9212) 挂 2 台设备
    (9301, 0, 9212, 0, 1, '2026-09-01 09:50:00', '2026-09-01 09:50:00', 'admin'),  -- ⚠️ MachineId 待填
    (9302, 0, 9212, 0, 1, '2026-09-01 09:50:00', '2026-09-01 09:50:00', 'admin'),  -- ⚠️ MachineId 待填
    -- V2 的外协工序(9213) 挂 1 台设备
    (9303, 0, 9213, 0, 1, '2026-09-01 09:50:00', '2026-09-01 09:50:00', 'admin'),  -- ⚠️ MachineId 待填
    -- V3 草稿的 CNC 工序(9222) 挂 1 台设备
    (9304, 0, 9222, 0, 1, '2026-09-01 09:50:00', '2026-09-01 09:50:00', 'admin');  -- ⚠️ MachineId 待填

-- =====================================================================
-- 5. 工序 - 物料 关联 Technical_NodeSettingTemplate_Material_RL
--    DataType / IOType 为枚举 int，数值请按代码枚举核对后再替换；
--    DataId / UnitId / UnitConversionId 也需替换为真实 ID。
--    该实体未实现 ISoftDeletedEntity，故无 IsDeleted 列。
-- =====================================================================
INSERT INTO Technical_NodeSettingTemplate_Material_RL
    (Id, NodeSettingTemplateId, DataType, DataId, IOType, InsteadSourceId,
     Quantity, UnitId, UnitConversionId, UnitListInfo, LossRate, SortNo,
     RefCraftOrderVersionId, Remark, EnterpriseId, CreateTime, UpdateTime, CreateUserName)
VALUES
    -- V2 下料(9211)：投入 1 种原料
    (9401, 9211,
     0,             -- ⚠️ DataType 待填（物料类型枚举）
     0,             -- ⚠️ DataId 待填（真实物料/产品 ID）
     0,             -- ⚠️ IOType 待填（0 投入）
     NULL,          -- InsteadSourceId 可空（替换料 ID）
     1.000000,      -- Quantity decimal(18,6)
     0,             -- ⚠️ UnitId 待填
     0,             -- ⚠️ UnitConversionId 待填
     '',            -- UnitListInfo 多单位 Json（InboundUnitDto 列表），暂无则空串
     0.000000,      -- LossRate decimal(9,6)
     1,
     NULL,          -- RefCraftOrderVersionId 可空（外部半成品引用的工艺版本）
     '投入原料（示例，待替换为真实物料）',
     1, '2026-09-01 10:00:00', '2026-09-01 10:00:00', 'admin'),

    -- V2 检验包装入库(9214)：产出 1 种成品
    (9402, 9214,
     0,             -- ⚠️ DataType 待填
     0,             -- ⚠️ DataId 待填
     1,             -- ⚠️ IOType 待填（1 产出，请按枚举核对）
     NULL,
     1.000000,
     0,             -- ⚠️ UnitId 待填
     0,             -- ⚠️ UnitConversionId 待填
     '',
     0.000000,
     1,
     NULL,
     '产出成品（示例，待替换为真实物料）',
     1, '2026-09-01 10:05:00', '2026-09-01 10:05:00', 'admin');

-- =====================================================================
-- 恢复外键校验
-- =====================================================================
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
-- 校验（可加条件后使用）
-- =====================================================================
-- SELECT co.Id AS CraftOrderId, co.Code, co.Name,
--        v.Id AS VersionId, v.Version, v.SequenceNo, v.IsMain, v.Status
-- FROM Technical_CraftOrder co
-- LEFT JOIN Technical_CraftOrderVersion v ON v.CraftOrderId = co.Id
-- ORDER BY co.Id, v.SequenceNo;
