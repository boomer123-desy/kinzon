# 订单及销售发货月报表



页面入口为 `index.vue`，复用宿主的 SearchPanel、KingZonDataGrid、Loading 和 kingzon.css。SearchPanel 按 `Search[]` 配置开账年份、月份及客户，查询时将 `monthSaleStat` 分组中的 `FilterTmp[]` 转为原 GET 参数；重置恢复当前年月和空客户。列表不分页，冻结排名及客户；横向滚动查看财务周金额、分类小计和同比。底部用 `footerData` 固定显示各周及金额总计，通过后端 Aspose.Cells 导出含公式的 Excel。



## 接入宿主



- 将本目录（包括 `component/sale-stat-week-detail.vue`）放入宿主页面目录，例如 `src/views/sale_data_stat/`，并在宿主路由/菜单中注册该页面。

- 将 `frontend/src/types/sales.ts` 中的报表类型同步合并到宿主 `src/types/sales.ts`，与当前页面的 `@/types/sales` 导入一致。

- 更新宿主的 `SaleDataStatController.cs` 和 `SaleDataStatDTO.cs`，然后重新构建并启动后端。Controller 仅保留实际使用的数据访问依赖；确认旧版销售/送货服务和 `CommonService<Base_Category>`、SystemParaService 可由宿主注入，客户信息取订单的 Customer 导航。

- 页面请求 `GET api/sales/SaleDataStat/GetMonthDataStat`，保持原 Controller 的 `api/sales` 路由。不要按邻近新版退货 Controller 的 `api/sale` 路由改写请求。

- 周明细增加 `CommonService<SaleOrderDetail>`、`CommonService<HistoryProduct>` 构造依赖，沿用旧版服务注册。对应周明细 DTO 同步到后端 `SaleDataStatDTO.cs` 和前端 `src/types/sales.ts`。



请求示例：`month=2026-09-01&customerName=甲&includeTax=false`，其中客户名称可省略。返回直接为 `MonthSaleDataStatDto`，沿用宿主 HTTP 封装的 `res.data` 获取方式。



本源码集合缺少完整宿主路由、应用入口及 `.csproj`。上述文件不会自行启动前端或注册宿主菜单。



## Excel 公式导出



- 点击“导出 Excel”，通过现有 `$http.getFile` 封装请求 `POST /api/sales/SaleDataStat/ExportMonthDataStat`，请求体为 `{ "month": "2026-03-01", "customerName": "甲", "includeTax": false }`。客户可省略；使用成功加载报表时的年月、客户和含税条件，导出全部匹配客户。服务端重新读取数据，和页面查询复用 `GetMonthReport`，统计口径一致。

- 后端复用宿主 `Aspose.Cells` 包及授权初始化，直接生成 `.xlsx` 并返回文件；月报关闭公共表格的 DOM 导出，周明细弹窗保留原导出方式。

- 导出模板为 `backend/Sales/KingZon.OLCM.Sales.WebApi/ExcelTmp/MonthSaleDataStat.xlsx`，部署到后端运行目录的 `ExcelTmp/MonthSaleDataStat.xlsx`。采用 `WorkbookDesigner.SetDataSource` / `Process(0, false)` 填充智能标记；固定表头、合并、字体、列宽、金额格式、百分比和退货颜色在模板内维护。六周模板按实际财务周数删除多余订单/发货列。

- 客户的周订单、周发货、退货及去年销售额写入数值；客户订单合计、月发货合计使用 `SUM`。分类小计和末尾总计的所有金额列（包括每周总计）使用 `SUBTOTAL(9, ...)`，总计自动跳过嵌套分类小计，避免重复累计。

- 每行同比使用 `IF`、`ROUND` 和 `ABS`，送货占比引用固定的总送货额单元格；比例保留六位精度、显示两位百分比。分母为零显示“—”。统计比例按汇总金额计算，不对客户百分比求和或平均。

- 导出前计算公式结果，同时保留公式并设置自动计算。Excel/WPS 修改周金额或去年销售额后，合计、同比及占比随之更新。客户排名保持查询结果中的1月排名。

- 根据财务月动态生成四、五或六周表头，冻结排名/客户和表头；标题居中，小计加粗斜体，总计加粗，退货红色，同比下降红色、上升绿色。



模板放入宿主项目的 `ExcelTmp` 目录，并在 `.csproj` 中配置输出及发布复制：



```xml

<ItemGroup>

  <None Update="ExcelTmp\MonthSaleDataStat.xlsx"

        CopyToOutputDirectory="PreserveNewest"

        CopyToPublishDirectory="PreserveNewest" />

</ItemGroup>

```



模板第1行为标题、第2行为账期说明、第3/4行为表头、第5行为数据智能标记；第6/7行的 B 列分别提供小计/总计字体样式，处理前读取并删除。调整样式时保留这些行的位置及标记字段：



| 标记 | 用途 |

| --- | --- |

| `&=$Title`、`&=$Period` | 月报标题、账期及客户条件 |

| `&=$DeliveryTitle`、`&=$LastYearTitle` | 本年发货额和去年销售额标题 |

| `&=$Week1` 至 `&=$Week6` | 财务周日期范围，订单和送货使用同一范围 |

| `&=Rows.Rank(copystyle)`、`&=Rows.CustomerName(copystyle)` | 排名及客户/统计行名称 |

| `&=Rows.OrderWeek1(formula,copystyle)` 等金额/比例字段 | 客户数值或统计公式，并复制模板样式 |



金额字段的 `formula` 参数让数据源中的公式字符串成为真实 Excel 公式，同时保留客户周金额的数值类型。`copystyle` 将数据行格式复制到新增行；小计/总计只按模板原型覆盖加粗和斜体，保留金额格式及退货颜色。



## 周金额明细



- 右上角按钮切换含税/不含税，默认为不含税；成功查询后固定显示本次税额口径，失败保留原报表。切换影响订单、送货、退货、同比、排名和Excel公式导出。

- 点击订单/送货周金额，打开 Dialog 并使用 KingZonDataGrid 展示只读明细。客户行查询对应客户，小计查询对应分类，总计查询当前筛选结果的全部客户；沿用成功加载报表时的客户名称条件。

- 销售请求 `GET api/sales/SaleDataStat/GetWeekSaleOrderDetails`，送货请求 `GET api/sales/SaleDataStat/GetWeekDeliveryDetails`。参数为 `month`、`weekNo`、可选 `customerId`、`filterCategory`、可空 `categoryId`、`includeTax`、可选 `customerName`；服务端根据财务配置确定日期范围，不接受任意无限日期区间。

- 接口返回 `rows`、`totalAmount`、`orderCount`、`detailCount`。单周非分页，前端虚拟滚动；销售金额按单头去重累计，产品行中的订单总额仅供查看，不能重复求和。没有产品明细的销售单保留空产品行。

- 字段参考 `SaleOrderController.GetSaleOrderData`、`DeliverOrderManageController.GetDeliverOrderManageIndexData`。历史产品和销售明细采用保留历史记录的左连接；送货/退货金额使用关联销售明细 Price/PriceHaveTax，不读取送货单价快照。销售关联缺失时保留明细，单价和金额显示“—”，汇总不计未知金额。

- 点击月报退货列，沿用送货接口，传 `onlyReturns=true、weekNo=0` 查询整个财务月，只显示 ReturnQuantity>0 的送货明细；客户/分类/总计和税额条件与月报一致。正常送货下钻仍传所点周号。

- 明细同时展示含税/不含税单价、明细金额；销售显示两种单头金额，送货显示两种退货金额。移除 ShowForm，日期和税额口径显示在弹窗标题。编号逐明细显示，底部编号合计显示单据数/明细数；金额放入 footerData。相同 orderId 的单号、客户、日期和销售单头金额通过公共 getCellSpan 合并，单头合计按单据去重。



## 统计口径



- 沿用原接口的旧版实体，未切换到 `Sale_Order` / `Inventory_DeliveryOrderDetail`。旧版与新版不可混用单位字段和数量。

- 所选月份是账期结束月：起始日读取系统参数“财务结算日期”，从上月该日至所选月份该日前一天。例如选择2026年3月、配置26时为2026-02-26至2026-03-25；选择2026年1月为2025-12-26至2026-01-25。查询使用 `>=开始日 && <结束日次日`，包含25日全天。周明细、同比、1月排名及Excel导出共用此账期定义，不在调用方额外增减月份。财务周为周六至周五，首尾截断；实际可能有四、五或六周。

- 配置为29至31且所在月没有该日时，边界取该月最后一天；开始和结束分别计算，避免二月截短后把三月边界也提前。去年同期按去年所选月份重新计算边界，正确处理闰年。

- 订单使用 `SaleOrder.CreateTime` 和 `OrderTotalPrice`，沿用原接口排除 `待下单` 的条件。

- 统计只返回金额，不返回订单笔数或送货明细数。客户、分类小计及总计分别返回 `orderWeeks`（销售订单金额）和 `deliveryWeeks`（送货金额），每项只含 `weekNo`、`amount`；表头日期范围统一在顶层 `weeks` 返回。

- 前端总计行也转换为 `weekAmounts`，通过周列的 `footer` 插槽展示；不在客户行中重复添加总计。空查询清空总计，失败保留原数据。同比及占比保留数值或null，到显示时再格式化。

- 客户送货占比 `deliveryShareRate`＝本行本期送货金额÷当前查询结果的送货总金额，按退货扣减前的送货金额计算。分类小计同用总额作分母，总计为100%；总送货额为零时返回null并显示“—”。客户筛选会同步改变分母。

- 退货标题及金额显示主题红色。同比下降显示红色向下图标及负百分比，上升显示绿色向上图标及百分比；持平显示0.00%，比较期为零显示“—”，均不显示涨跌图标。

- 前端按 `weekNo` 将两个金额列表映射到表格行的 `weekAmounts`，单元格通过 `scope.column.field` 取值；动态周列按带 key 的 `template v-for` 组织，避免两段循环的插槽取值串用。表格 ID 按表头结构及周数区分，避免公共表格把旧列缓存恢复到不同周数的表头。

- 发货使用旧版送货单 DeliveryOrder.DeliveryTime，统计关联有效送货单的未删除明细；IsOutboundInventory 表示通知时已出库，不能据此排除送货时出库的数据。金额为销售明细 Price/PriceHaveTax × DeliveryQuantity。

- 当前 Controller 已改为使用送货明细累计 `ReturnQuantity`，退货金额为销售明细 `Price/PriceHaveTax × ReturnQuantity`，归入送货单日期；这表示按发货账期回扣，不是按退货发生日期统计。净发货额＝发货额－退货额。

- 销售单头分别读取 OrderTotalPrice（不含税）和 OrderTotalPriceHaveTax（含税）；销售明细分别读取 Price/TotalPrice 和 PriceHaveTax/TotalPriceHaveTax。不根据税率重新推算，也不在含税金额为零时擅自回退为未税金额。

- 分类内按所选年1月**财务月净发货额**降序排名，同额按名称、ID稳定排序。客户来自当前/去年同期销售订单及当前/1月送货记录；没有相关业务的客户不额外补零。

- 客户名称筛选保留原排名，小计和总计只合计筛选结果。

- 去年销售额 `lastYearOrderAmount` 读取去年同财务月 `SaleOrder.OrderTotalPrice/OrderTotalPriceHaveTax`，与当前税额口径一致，按创建日期且排除待下单订单。同比为 `(今年发货额－去年销售额) / abs(去年销售额)`，今年发货额使用未扣退货的 `deliveryAmount`。去年销售额为零时返回null；小计/总计按合计金额计算比例，不平均客户百分比。当前页面不显示环比。



## 验证



```powershell

python backend/Sales/tests/verify_month_sale_stat.py

node --test frontend/tests/sales/month-sale-stat.test.cjs

```



后端检查编译实际 Controller/DTO，在 SQLite 中执行查询；缺少的宿主依赖使用按旧版调用点定义的测试替身。Excel 使用真实 Aspose.Cells 26.9.0 加载本模板，校验智能标记清除、原型行删除、数值类型、四/五/六周表头、客户/小计/总计金额及比例、筛选/空结果、样式、修改后重算和保存重开后公式保留。前端检查复用现有 `frontend/node_modules` 中的编译工具，编译实际页面与 DTO，并检查数据展平、下载筛选快照、重复点击及错误处理。它们不替代宿主构建、数据库联调及真实 KingZonDataGrid 渲染验证。

