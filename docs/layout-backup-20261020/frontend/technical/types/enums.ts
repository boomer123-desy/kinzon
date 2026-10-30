export enum ShowFormDataType {
  文本 = 1,
  单个标签 = 2,
  多个标签 = 3,
  日期 = 4,
  可点击链接 = 5,
  文件 = 6,
  多个文件 = 7
}

export enum FilterType {
  Contains,
  Equals,
  NotEquals,
  GreaterThan,
  LessThan,
  GreaterThanOrEqual,
  LessThanOrEqual,
  Json,
  IntArrayContains
}

export enum DataType {
  Text,
  Integer,
  Decimal,
  Date,
  DateTime,
  SelectText,
  SelectNum,
  SelectBoolean,
  multiSelectNum
}

export enum FileBase {
  undefined = 0, //未定义
  Customer = 10, //客户文件
  DeliveryOrderTemplate = 19, //送货模板
  TalkRecord = 23, //面谈记录文件
  UserTask = 25, //项目处理文件
  Goods = 30, //客户产品文件
  Product = 40, //成品文件
  ProductSpec = 41, //成品规格文件
  ProductPack = 42, //成品规格文件
  Material = 60, //物料文件
  Formula = 70, //配方文件
  Craft = 90 //工序文件
}

export enum FileStorageSourceType {
  无 = 0, // 不对应任何来源
  客户文件 = 100, //客户文件，对应Customer的Id
  送货单模板 = 190, //送货单模板，对应Customer的Id
  面谈记录 = 230, // 面谈记录文件，对应TalkRecord的Id
  项目处理 = 250, // 项目处理文件，对应Task_User_RL的Id
  客户产品 = 300, // 客户产品文件，对应Goods的Id
  成品文件 = 400, // 成品文件，对应成品的Id、
  成品规格文件 = 410,
  成品包装文件 = 420,
  物料文件 = 600, //物料文件，对应物料的Id
  配方文件 = 700, //配方文件，对应配方的Id
  工序文件 = 900 //工序文件，对应工序的Id
}

export enum FileStorageContentType {
  未知 = 0,
  jpeg = 1,
  png = 2,
  pdf = 3,
  docx = 4,
  doc = 5,
  xls = 6,
  xlsx = 7,
  ppt = 8,
  pptx = 9
}

export enum SystemParaType {
  编码前缀 = 1,
  客户产品附加参数 = 21,
  /// <summary>
  /// 阿米巴信息
  /// </summary>
  每日结算时间 = 100,
  结算日期偏移值 = 101,
  销售单价比例 = 102,
  核算表多级参数 = 103,
  核算表单一参数 = 104,
  确认辅料费用部门 = 105,
  单本预警 = 106,
  /// <summary>
  /// 库存信息
  /// </summary>
  库存呆滞天数 = 120,

  /// <summary>
  /// 产品信息
  /// </summary>
  包装类型 = 140,
  产品附加参数 = 141,
  包装附加参数 = 142,

  /// <summary>
  /// 类别信息
  /// </summary>
  类别附加参数 = 180,
  /// <summary>
  /// 畅捷通
  /// </summary>
  费用畅捷通附加参数 = 200,
  费用类别畅捷通附加参数 = 201,
  畅捷通销售出库附加参数 = 202,

  /// <summary>
  /// 公司
  /// </summary>
  公司 = 300,

  /// <summary>
  /// 供应商管理
  /// </summary>
  供应商管理信息 = 400,

  /// <summary>
  /// 配方
  /// </summary>
  配方附加参数 = 500,
  配方版本附加参数 = 501,
  配方检验设备 = 502
}

export enum SystemParaCategory {
  //客户信息 = 1,
  //产品信息 = 2,
  //物料信息=3,
  //生产信息=4,
  //工艺单信息=5,
  采购信息 = 7,
  //工艺单信息=5,
  //采购信息=7,

  //销售信息 = 8,
  阿米巴信息 = 10,
  //到货信息 = 11,
  //库存信息 = 12,
  //财务信息 = 14,
  //类别信息 = 18,
  系统对接信息 = 20
}

export enum DisplayType {
  UnDefine = 0, //未定义
  Select = 10, //下拉框
  Switch = 20, //开关
  CheckBox = 30, //复选框
  RadioButton = 40, //单选框
  TextBox = 50, //文本框
  DateTime = 60, //日期时间
  Textarea = 70, //多行文本
  Number = 80, //数字
  Picture = 90, //图片
  Time = 100, //时间
  SelectFile = 110 //多选文件
}

export enum SystemParaCategoryType {
  系统参数 = 1,
  系统拓展参数 = 2,
  拓展参数 = 3
}

export enum DisplayTypeByCh {
  下拉选择 = 10, //下拉框
  开关 = 20, //开关
  多选框 = 30, //复选框
  单选框 = 40, //单选框
  单行文本 = 50, //文本框
  日期 = 60, //日期时间
  多行文本 = 70, //多行文本
  数字 = 80, //数字
  图片 = 90 //图片
}

export enum PartnerType {
  客户 = 1,
  供应商 = 2,
  客户兼供应商 = 3
}

export enum CategoryType {
  产品类别 = 1,
  物料类别 = 2,
  客户类别 = 3,
  供应商类别 = 4
}

export enum UnitType {
  Product,
  RawMaterial,
  // Price //计价单位
  Package = 3 //包装单位
}

export enum ProductType {
  原材料 = 0, // 原材料
  半成品 = 1, // 半成品
  成品 = 2 // 成品
}

export enum SupplierType {
  客户 = 0, // 不采购不外协：0000
  采购 = 1, // 采购：0001
  外协 = 2 // 外协：0010
}

export enum ReviewStatus {
  待提交 = 0,
  /// <summary>
  /// 待评审
  /// </summary>
  评审中 = 1,
  /// <summary>
  /// 评审通过
  /// </summary>
  评审通过 = 2,
  评审驳回 = 3
}

export enum ProductProperty {
  新品 = 1,
  成熟产品 = 2
}

export enum TimeUnit {
  天 = 1,
  月 = 2,
  年 = 3
}

export enum PriceModel {
  合同定价 = 1,
  单次询价 = 2
}

//产品管理类型
export enum ProductManageType {
  sync_product_add_goods = 'sync_product_add_goods', //同步产品手动新增客户产品，
  sync_and_add_product = 'sync_and_add_product' //同步产品并新增产品
}

export enum FormulaStatus {
  待审核 = 1,
  已生效 = 2,
  淘汰 = 3
}

export enum CraftStatus {
  待审核 = 1,
  已生效 = 2,
  淘汰 = 3
}

export enum FormulaDetailType {
  Product = 1, //DetailId 对应Base_Product
  Formula = 2 //DetailId 对应Technical_Formula
}

export enum FrequencyType {
  每批 = 1,
  每月 = 2
}

export enum InspValueMode {
  等于 = 1,
  范围 = 2,
  偏差 = 3,
  大于等于 = 4,
  小于等于 = 5
}

/** 版本状态：0 待审核、1 已通过、2 已废弃（对应 C# CraftOrderVersionStatus） */
export enum CraftOrderVersionStatus {
  待审核 = 1,
  已发布 = 2,
  已废弃 = 3
}

/** 排产状态保留现有值，草稿仅用于前端未保存空行。 */
export enum ProductionScheduleStatus {
  草稿 = 'draft',
  已排产 = 'planned',
  在制 = 'running',
  已撤销 = 'cancelled'
}

export enum ScheduleReceiptStatus {
  待入库 = 'planned',
  已入库 = 'received',
  已撤销 = 'cancelled'
}

/** 与排产物料数量接口的 Status 文本一致。 */
export enum MaterialRequirementStatus {
  待排产 = '待排产',
  部分排产 = '部分排产',
  排产延期 = '排产延期',
  完全缺料 = '完全缺料',
  部分缺料 = '部分缺料',
  采购中 = '采购中',
  库存充足 = '库存充足',
  已满足 = '已满足',
  无需需求 = '无需需求'
}
