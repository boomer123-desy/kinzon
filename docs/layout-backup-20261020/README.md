# returnOrder 项目文件

项目文件统一按前端、后端归类，各目录内保留原业务模块和新旧版本。

```text
returnOrder/
├─ frontend/                 前端 Vue、TypeScript、CSS、组件及前端测试
│  ├─ base-pc/               公共搜索组件
│  ├─ inventory/             库存页面
│  ├─ new-sale_return_order/ 新版退货页面
│  ├─ old-sale_return_order/ 旧版退货页面
│  ├─ package/               包装组件
│  ├─ production/            生产页面和类型
│  ├─ Sale_data_stat/        保留原有空目录
│  ├─ sale_order_manage/     销售订单页面和类型
│  ├─ sale_return_order/     退货、到货、销售月报和公共表格
│  └─ technical/             工艺、配方、排产、类型和公共样式
├─ backend/                  C#、SQL、后端测试、数据工具和报表模板
│  ├─ new-sale_return_order/
│  ├─ old-sale_return_order/
│  ├─ production/
│  ├─ sale_order_manage/
│  ├─ sale_return_order/
│  └─ technical/
├─ docs/                     文件搬迁清单及路径调整记录
└─ AGENTS.md                 项目开发约定
```

原 `web`、`front`、`view` 的内容直接放入 `frontend/业务模块/`；原 `production/back` 的内容直接放入 `backend/production/`。Controller、DTO、实体等内部目录及文件名保持原样。销售月报模板位于 `backend/sale_return_order/ExcelTemplates/MonthSaleDataStat.xlsx`。

此目录是业务模块源码集合，缺少完整宿主工程。前端 `@/` 导入和后端 namespace、接口路由沿用宿主契约；集成时仍需放入对应宿主工程。

## 现有验证入口

在项目根目录执行：

```powershell
$env:NODE_PATH = (Resolve-Path 'frontend/technical/production_schedule_manage/node_modules').Path
npm --prefix frontend/technical/production_schedule_manage test
node --test (Get-ChildItem 'frontend/technical/craft_manage/component/__tests__/*.test.cjs').FullName
node --test (Get-ChildItem 'frontend/production/views/dispatch_order_manage/__tests__/*.test.cjs').FullName
node --test frontend/sale_return_order/tests/month-sale-stat.test.cjs
python backend/production/tests/verify_bom_demand.py
python backend/production/tests/verify_dispatch_bom.py
python backend/technical/craft/tests/verify_mbom_sync.py
python backend/technical/production_schedule/tests/verify_quantity_query.py
python backend/sale_return_order/tests/verify_month_sale_stat.py
```

后端脚本使用 .NET SDK 9 和隔离测试替身。上述验证不代表完整宿主构建或真实数据库联调。

原文件路径、新路径与搬迁前 SHA256 见 `docs/file-moves-20261020.json`。路径调整前的文档和测试脚本备份位置见 `docs/path-updates-20261020.json`。
