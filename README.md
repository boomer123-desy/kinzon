# returnOrder 项目文件

按提供的宿主工程结构整理，源码路径以本目录为起点。

```text
returnOrder/
├─ frontend/
│  ├─ src/
│  │  ├─ assets/css/           公共 kingzon.css
│  │  ├─ components/           搜索、表格、包装等公共组件
│  │  ├─ types/                按领域组织的 DTO、枚举
│  │  │  └─ fragments/         尚未完整合入宿主的类型片段
│  │  └─ views/
│  │     ├─ sales/             销售、退货、销售月报
│  │     ├─ production/        派工
│  │     ├─ technology/        工艺、配方、排产
│  │     ├─ inventory/         库存、退货到货
│  │     └─ purchase/          采购入库
│  ├─ tests/sales/             销售月报测试；其他组件测试就近保留
│  ├─ examples/sales/          示例数据
│  ├─ package.json            当前源码集合的验证工具依赖与脚本
│  └─ node_modules/
├─ backend/
│  ├─ Sales/                  销售报表；其他领域同样分层
│  │  ├─ KingZon.OLCM.Sales.WebApi/
│  │  │  ├─ Controllers/
│  │  │  ├─ Dto/
│  │  │  └─ ExcelTmp/
│  │  ├─ tests/
│  │  ├─ tools/
│  │  └─ data/
│  ├─ Base/ Common/ Inventory/ Production/ Sale/ Technical/ Technology/
│  │                          按现有 namespace 归入 WebApi、DataBase、Services
│  └─ references/             同名的其他版本，单独保留
├─ docs/                      搬迁记录、验证日志及模块说明
└─ AGENTS.md                  项目开发约定
```

后端实体放在对应领域的 `KingZon.OLCM.<领域>.DataBase/Entities`，枚举在 `Enum`；Controller 与接口 DTO 分别放在 `.WebApi/Controllers`、`.WebApi/Dto`；已有服务及服务 DTO 放在 `.Services`、`.Services/Dto`。依据源码 namespace 归类，保留 Common/Sale/Sales、Technical/Technology 的实际区别，没有更改 namespace、接口路由或业务统计口径。

前端同名的新旧页面以 `_new`、`_legacy` 区分，表格组件的其他版本放在 `src/components/reference`。`src/types/fragments` 保留原始不完整类型定义；派工依据枚举已汇入 `src/types/enums.ts`。这些版本需在完整宿主集成时按真实契约选用，不能同时当作正式实现覆盖。

本目录仍是业务模块源码集合，缺少完整宿主 `.sln/.csproj`、Vue 应用入口和部分公共类型。没有为整理目录补造启动工程；`frontend/package.json` 用于现有测试。`@/` 以宿主 `src/` 为根，集成时仍需核对宿主依赖。

## 验证入口

在项目根目录执行：

```powershell
npm --prefix frontend test
python backend/Production/tests/verify_bom_demand.py
python backend/Production/tests/verify_dispatch_bom.py
python backend/Technical/tests/craft/verify_mbom_sync.py
python backend/Technical/tests/production_schedule/verify_quantity_query.py
python backend/Sales/tests/verify_month_sale_stat.py
```

前端依赖统一位于 `frontend/node_modules`。后端脚本使用 .NET SDK 9，在临时项目内配合测试替身运行，不代表完整宿主构建或真实数据库联调。

搬迁前后均运行前端全部 136 项测试，结果为 133 项通过、3 项已有失败（派工收入计价初始化、收入计价校验、月报周明细关闭状态）。5 组后端隔离检查通过。日志见 `docs/host-layout-*-20261020.log`；本次未修改这些已有失败对应的业务逻辑。

## 搬迁记录

本次 220 个文件的原路径、新路径、搬迁前后 SHA256 和内容变化清单见 [搬迁清单](docs/host-layout-moves-20261020.json)。路径调整前的部分文件及 skill 备份在 `docs/layout-backup-20261020`。更早的 `file-moves`、`path-updates` 清单作为历史记录保留，不作为当前路径说明。

项目写法继续遵循 [kingzon-project-style](C:/Users/11063/.codex/skills/kingzon-project-style/SKILL.md)，源码参考路径已同步更新。
# kinzon
