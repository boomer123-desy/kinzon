-- 在 test_qibiao 库执行。仅 SET / SELECT，不修改业务数据。
-- 选择2026年3月、结算日26：本期为2026-02-26至2026-03-26（结束时间不包含）。
-- 先执行参数区，再按需执行下面各段查询。金额列沿用接口字段名。
USE `test_qibiao`;

SET @report_month = CAST('2026-03-01 00:00:00' AS DATETIME);
SET @period_start = DATE_ADD(DATE_SUB(@report_month, INTERVAL 1 MONTH), INTERVAL 25 DAY);
SET @period_end = DATE_ADD(@period_start, INTERVAL 1 MONTH);
SET @last_year_start = DATE_SUB(@period_start, INTERVAL 1 YEAR);
SET @last_year_end = DATE_ADD(@last_year_start, INTERVAL 1 MONTH);
SET @january_start = STR_TO_DATE(CONCAT(YEAR(@report_month) - 1, '-12-26'), '%Y-%m-%d');
SET @january_end = DATE_ADD(@january_start, INTERVAL 1 MONTH);
SET @pending_status = 0;    -- 待下单；如宿主枚举值不同，请替换为实际值。
SET @customer_name = '';    -- 空字符串查全部；可填写客户名称片段。
SET @include_tax = 0;      -- 0不含税，1含税；和报表按钮一致。
SET @only_returns = 0;     -- 第4段查询：1只查看有退货数量的送货明细。
SET @customer_id = NULL;    -- 后面两段原始单据查询可指定客户ID。

-- 1. 客户、分类小计、总计及同比/送货占比。
-- 先分别汇总销售与送货，再合并。不能直接把两个明细集关联，否则会重复累计。
WITH order_totals AS (
    SELECT o.CustomerId,
           SUM(CASE WHEN o.CreateTime >= @period_start AND o.CreateTime < @period_end
                    THEN IF(@include_tax, o.OrderTotalPriceHaveTax, o.OrderTotalPrice) ELSE 0 END) AS orderAmount,
           SUM(CASE WHEN o.CreateTime >= @last_year_start AND o.CreateTime < @last_year_end
                    THEN IF(@include_tax, o.OrderTotalPriceHaveTax, o.OrderTotalPrice) ELSE 0 END) AS lastYearOrderAmount
    FROM SaleOrder o
    WHERE o.Status <> @pending_status
      AND ((o.CreateTime >= @period_start AND o.CreateTime < @period_end)
        OR (o.CreateTime >= @last_year_start AND o.CreateTime < @last_year_end))
    GROUP BY o.CustomerId
), delivery_totals AS (
    SELECT o.CustomerId,
           SUM(CASE WHEN o.DeliveryTime >= @period_start AND o.DeliveryTime < @period_end
                    THEN IF(@include_tax, sd.PriceHaveTax, sd.Price) * d.DeliveryQuantity ELSE 0 END) AS deliveryAmount,
           SUM(CASE WHEN o.DeliveryTime >= @period_start AND o.DeliveryTime < @period_end
                    THEN IF(@include_tax, sd.PriceHaveTax, sd.Price) * d.ReturnQuantity ELSE 0 END) AS returnAmount,
           SUM(CASE WHEN o.DeliveryTime >= @january_start AND o.DeliveryTime < @january_end
                    THEN IF(@include_tax, sd.PriceHaveTax, sd.Price) * (d.DeliveryQuantity - d.ReturnQuantity) ELSE 0 END) AS januaryNetDeliveryAmount
    FROM DeliveryOrderDetail d
    JOIN DeliveryOrder o ON o.Id = d.DeliveryOrderId
    LEFT JOIN SaleOrderDetail sd ON sd.Id = d.SaleOrderDetailId
    WHERE d.IsDeleted = 0
      AND ((o.DeliveryTime >= @period_start AND o.DeliveryTime < @period_end)
        OR (o.DeliveryTime >= @january_start AND o.DeliveryTime < @january_end))
    GROUP BY o.CustomerId
), related_customers AS (
    SELECT CustomerId FROM order_totals
    UNION
    SELECT CustomerId FROM delivery_totals
), customers AS (
    SELECT p.Id AS customerId, p.Name AS customerName, p.CategoryId AS categoryId,
           COALESCE(NULLIF(c.Name, ''), '未分类') AS categoryName,
           COALESCE(s.orderAmount, 0) AS orderAmount,
           COALESCE(d.deliveryAmount, 0) AS deliveryAmount,
           COALESCE(d.returnAmount, 0) AS returnAmount,
           COALESCE(d.januaryNetDeliveryAmount, 0) AS januaryNetDeliveryAmount,
           COALESCE(s.lastYearOrderAmount, 0) AS lastYearOrderAmount
    FROM related_customers r
    JOIN Customer p ON p.Id = r.CustomerId AND p.IsDeleted = 0
    LEFT JOIN Base_Category c ON c.Id = p.CategoryId
    LEFT JOIN order_totals s ON s.CustomerId = p.Id
    LEFT JOIN delivery_totals d ON d.CustomerId = p.Id
    WHERE @customer_name = '' OR INSTR(LOWER(p.Name), LOWER(TRIM(@customer_name))) > 0
), report_rows AS (
    SELECT 1 AS rowType, categoryId, categoryName, customerId, customerName,
           orderAmount, deliveryAmount, returnAmount, januaryNetDeliveryAmount, lastYearOrderAmount
    FROM customers
    UNION ALL
    SELECT 2, categoryId, categoryName, NULL, CONCAT(categoryName, '小计'),
           SUM(orderAmount), SUM(deliveryAmount), SUM(returnAmount), SUM(januaryNetDeliveryAmount), SUM(lastYearOrderAmount)
    FROM customers
    GROUP BY categoryId, categoryName
    UNION ALL
    SELECT 3, NULL, NULL, NULL, '总计',
           COALESCE(SUM(orderAmount), 0), COALESCE(SUM(deliveryAmount), 0), COALESCE(SUM(returnAmount), 0),
           COALESCE(SUM(januaryNetDeliveryAmount), 0), COALESCE(SUM(lastYearOrderAmount), 0)
    FROM customers
)
SELECT CASE rowType WHEN 1 THEN '客户' WHEN 2 THEN '分类小计' ELSE '总计' END AS 行类型,
       categoryId, categoryName, customerId, customerName,
       orderAmount, deliveryAmount, returnAmount,
       deliveryAmount - returnAmount AS netDeliveryAmount,
       januaryNetDeliveryAmount, lastYearOrderAmount,
       ROUND((deliveryAmount - lastYearOrderAmount) / NULLIF(ABS(lastYearOrderAmount), 0), 6) AS yearOnYearRate,
       ROUND(100 * (deliveryAmount - lastYearOrderAmount) / NULLIF(ABS(lastYearOrderAmount), 0), 2) AS 同比百分比,
       ROUND(deliveryAmount / NULLIF((SELECT SUM(deliveryAmount) FROM customers), 0), 6) AS deliveryShareRate,
       ROUND(100 * deliveryAmount / NULLIF((SELECT SUM(deliveryAmount) FROM customers), 0), 2) AS 客户送货占比百分比
FROM report_rows
ORDER BY rowType, categoryName, januaryNetDeliveryAmount DESC, customerName, customerId;

-- 2. 每周订单/送货总计，周六至周五；首尾周按账期截断。
WITH RECURSIVE weeks AS (
    SELECT 1 AS weekNo,
           CAST(@period_start AS DATETIME) AS startDate,
           CAST(LEAST(@period_end,
               DATE_ADD(@period_start, INTERVAL (MOD(4 - WEEKDAY(@period_start) + 7, 7) + 1) DAY)) AS DATETIME) AS endExclusive
    UNION ALL
    SELECT weekNo + 1, endExclusive, LEAST(@period_end, DATE_ADD(endExclusive, INTERVAL 7 DAY))
    FROM weeks
    WHERE endExclusive < @period_end
), order_rows AS (
    SELECT o.CreateTime, IF(@include_tax, o.OrderTotalPriceHaveTax, o.OrderTotalPrice) AS OrderTotalPrice
    FROM SaleOrder o
    JOIN Customer p ON p.Id = o.CustomerId AND p.IsDeleted = 0
    WHERE o.Status <> @pending_status
      AND o.CreateTime >= @period_start AND o.CreateTime < @period_end
      AND (@customer_name = '' OR INSTR(LOWER(p.Name), LOWER(TRIM(@customer_name))) > 0)
), delivery_rows AS (
    SELECT o.DeliveryTime, IF(@include_tax, sd.PriceHaveTax, sd.Price) * d.DeliveryQuantity AS amount
    FROM DeliveryOrderDetail d
    JOIN DeliveryOrder o ON o.Id = d.DeliveryOrderId
    LEFT JOIN SaleOrderDetail sd ON sd.Id = d.SaleOrderDetailId
    JOIN Customer p ON p.Id = o.CustomerId AND p.IsDeleted = 0
    WHERE d.IsDeleted = 0
      AND o.DeliveryTime >= @period_start AND o.DeliveryTime < @period_end
      AND (@customer_name = '' OR INSTR(LOWER(p.Name), LOWER(TRIM(@customer_name))) > 0)
), weekly_orders AS (
    SELECT w.weekNo, COALESCE(SUM(o.OrderTotalPrice), 0) AS orderAmount
    FROM weeks w
    LEFT JOIN order_rows o ON o.CreateTime >= w.startDate AND o.CreateTime < w.endExclusive
    GROUP BY w.weekNo
), weekly_deliveries AS (
    SELECT w.weekNo, COALESCE(SUM(d.amount), 0) AS deliveryAmount
    FROM weeks w
    LEFT JOIN delivery_rows d ON d.DeliveryTime >= w.startDate AND d.DeliveryTime < w.endExclusive
    GROUP BY w.weekNo
)
SELECT w.weekNo, DATE(w.startDate) AS startDate,
       DATE(DATE_SUB(w.endExclusive, INTERVAL 1 DAY)) AS endDate,
       s.orderAmount, d.deliveryAmount
FROM weeks w
JOIN weekly_orders s ON s.weekNo = w.weekNo
JOIN weekly_deliveries d ON d.weekNo = w.weekNo
ORDER BY w.weekNo;

-- 3. 原始销售单：检查本期订单总额及去年同期销售额。
-- 单头金额逐单展示，不关联销售明细，避免重复。
SELECT CASE WHEN o.CreateTime >= @period_start AND o.CreateTime < @period_end
            THEN '本期订单' ELSE '去年同期销售' END AS 统计期间,
       o.Id, o.OrderNo, o.CustomerId, p.Name, o.CreateTime, o.Status, o.OrderTotalPrice
FROM SaleOrder o
JOIN Customer p ON p.Id = o.CustomerId AND p.IsDeleted = 0
WHERE o.Status <> @pending_status
  AND ((o.CreateTime >= @period_start AND o.CreateTime < @period_end)
    OR (o.CreateTime >= @last_year_start AND o.CreateTime < @last_year_end))
  AND (@customer_id IS NULL OR o.CustomerId = @customer_id)
  AND (@customer_name = '' OR INSTR(LOWER(p.Name), LOWER(TRIM(@customer_name))) > 0)
ORDER BY o.CustomerId, o.CreateTime, o.Id;

-- 4. 原始本期送货明细：核对数量、单价、送货额与退货额。
-- 退货沿用当前 Controller 口径：累计退货数量按送货日期归入账期。
SELECT o.Id AS deliveryOrderId, o.OrderNo, d.Id AS detailId, d.DetailNo,
       o.CustomerId, p.Name, o.DeliveryTime,
       IF(@include_tax, sd.PriceHaveTax, sd.Price), d.DeliveryQuantity, d.ReturnQuantity,
       IF(@include_tax, sd.PriceHaveTax, sd.Price) * d.DeliveryQuantity AS deliveryAmount,
       IF(@include_tax, sd.PriceHaveTax, sd.Price) * d.ReturnQuantity AS returnAmount,
       IF(@include_tax, sd.PriceHaveTax, sd.Price) * (d.DeliveryQuantity - d.ReturnQuantity) AS netDeliveryAmount
FROM DeliveryOrderDetail d
JOIN DeliveryOrder o ON o.Id = d.DeliveryOrderId
    LEFT JOIN SaleOrderDetail sd ON sd.Id = d.SaleOrderDetailId
JOIN Customer p ON p.Id = o.CustomerId AND p.IsDeleted = 0
WHERE d.IsDeleted = 0 AND (@only_returns = 0 OR d.ReturnQuantity > 0)
  AND o.DeliveryTime >= @period_start AND o.DeliveryTime < @period_end
  AND (@customer_id IS NULL OR o.CustomerId = @customer_id)
  AND (@customer_name = '' OR INSTR(LOWER(p.Name), LOWER(TRIM(@customer_name))) > 0)
ORDER BY o.CustomerId, o.DeliveryTime, o.Id, d.Id;

