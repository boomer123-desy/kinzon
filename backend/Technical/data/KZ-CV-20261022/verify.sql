-- 只读核对本批数据；生命周期操作后状态、主标记与数量可能合理变化。
SET @batch = 'KZ-CV-20261022';

SELECT o.Id, o.Code, o.Name, COUNT(v.Id) AS VersionCount,
       SUM(v.IsMain) AS MainVersionCount,
       MAX(CASE WHEN v.IsMain=1 THEN v.Version END) AS MainVersion
FROM Technical_CraftOrder o
JOIN Technical_CraftOrderVersion v ON v.CraftOrderId=o.Id AND v.IsDeleted=0
WHERE o.CreateUserNo=@batch AND o.IsDeleted=0
GROUP BY o.Id,o.Code,o.Name
ORDER BY o.Code;

SELECT o.Code,v.Id AS CraftVersionId,v.Version,v.IsMain,
       CASE v.Status WHEN 0 THEN '暂存' WHEN 1 THEN '待审核'
            WHEN 2 THEN '已发布' WHEN 3 THEN '已废弃' END AS CraftStatus,
       v.WorkflowTemplateId,v.BomVersionId,b.IsMain AS BomIsMain,b.Status AS BomStatus,
       (SELECT COUNT(*) FROM Technical_NodeSettingTemplate n
        WHERE n.CraftOrderVersionId=v.Id AND n.IsDeleted=0) AS ProcessCount
FROM Technical_CraftOrder o
JOIN Technical_CraftOrderVersion v ON v.CraftOrderId=o.Id AND v.IsDeleted=0
LEFT JOIN Production_MBom_Version b ON b.Id=v.BomVersionId
WHERE o.CreateUserNo=@batch AND o.IsDeleted=0
ORDER BY o.Code,v.SequenceNo;

-- 领用引用：可看到 A 的不同版本分别引用 B-V1 与 B-V2。
SELECT o.Code,v.Version,n.Code AS NodeCode,p.ProductNo,p.ProductName,
       m.Quantity,u.Name AS UnitName,ref.Version AS ReferencedVersion,ref.IsMain AS RefIsMain
FROM Technical_NodeSettingTemplate_Material_RL m
JOIN Technical_NodeSettingTemplate n ON n.Id=m.NodeSettingTemplateId
JOIN Technical_CraftOrderVersion v ON v.Id=n.CraftOrderVersionId
JOIN Technical_CraftOrder o ON o.Id=v.CraftOrderId
JOIN Technical_CraftOrderVersion ref ON ref.Id=m.RefCraftOrderVersionId
JOIN Base_Product p ON p.Id=m.DataId
JOIN Base_Unit u ON u.Id=m.UnitId
WHERE m.CreateUserNo=@batch AND n.IsDeleted=0 AND v.IsDeleted=0
ORDER BY o.Code,v.SequenceNo,n.Sequence,m.SortNo;

-- 主料合并与替料明细；QuantityDiff2MainVersion 为相同类型/目标相对主版本的差量。
SELECT o.Code,v.Version,d.Id,d.DetailType,d.DetailId,d.Quantity,d.Ratio,
       d.IsSubstitute,d.MainDetailId,d.QuantityDiff2MainVersion,d.UnitInfo
FROM Production_MBom_Detail d
JOIN Technical_CraftOrderVersion v ON v.BomVersionId=d.VersionId
JOIN Technical_CraftOrder o ON o.Id=v.CraftOrderId
WHERE d.CreateUserNo=@batch AND v.IsDeleted=0
ORDER BY o.Code,v.SequenceNo,d.`Order`;
