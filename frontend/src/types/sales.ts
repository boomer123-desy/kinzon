export class MonthSaleWeekStatDto {
  weekNo: number = 0
  amount: number = 0

  constructor(init?: Partial<MonthSaleWeekStatDto>) {
    Object.assign(this, init)
  }
}

//报表表头日期范围，仅在顶层返回，不重复放入客户及小计的金额列表。
export class FinanceWeekDto {
  weekNo: number = 0
  startDate: string = ''
  endDate: string = ''
  days: number = 0

  constructor(init?: Partial<FinanceWeekDto>) {
    Object.assign(this, init)
  }
}

export class MonthSaleStatBaseDto {
  orderAmount: number = 0
  deliveryAmount: number = 0
  deliveryShareRate: number | null = null
  returnAmount: number = 0
  netDeliveryAmount: number = 0
  januaryNetDeliveryAmount: number = 0
  lastYearOrderAmount: number = 0
  previousMonthNetDeliveryAmount: number = 0
  yearOnYearRate: number | null = null
  monthOnMonthRate: number | null = null
  orderWeeks: Array<MonthSaleWeekStatDto> = []
  deliveryWeeks: Array<MonthSaleWeekStatDto> = []

  constructor(init?: Partial<MonthSaleStatBaseDto>) {
    Object.assign(this, init)
  }
}

export class MonthSaleCustomerStatDto extends MonthSaleStatBaseDto {
  customerId: number = 0
  customerName: string = ''
  rank: number = 0

  constructor(init?: Partial<MonthSaleCustomerStatDto>) {
    super()
    Object.assign(this, init)
  }
}

export class MonthSaleCategoryStatDto extends MonthSaleStatBaseDto {
  categoryId: number | null = null
  categoryName: string = ''
  customers: Array<MonthSaleCustomerStatDto> = []

  constructor(init?: Partial<MonthSaleCategoryStatDto>) {
    super()
    Object.assign(this, init)
  }
}

export class MonthSaleDataStatDto extends MonthSaleStatBaseDto {
  includeTax: boolean = false
  year: number = 0
  month: number = 0
  startDate: string = ''
  endDate: string = ''
  weeks: Array<FinanceWeekDto> = []
  categories: Array<MonthSaleCategoryStatDto> = []

  constructor(init?: Partial<MonthSaleDataStatDto>) {
    super()
    Object.assign(this, init)
  }
}

//前端使用：扁平表格中的客户、小计及总计行。
export enum SaleStatRowType {
  客户 = 'customer',
  小计 = 'subtotal',
  总计 = 'total'
}

export class MonthSaleTableRowDto extends MonthSaleStatBaseDto {
  id: string = ''
  rowType: SaleStatRowType = SaleStatRowType.客户
  categoryName: string = ''
  customerName: string = ''
  rank: number | null = null
  customerId: number | null = null
  categoryId: number | null = null
  //前端使用：以实际表格字段定位周金额，避免动态列插槽复用时取错列表。
  weekAmounts: Record<string, number> = {}

  constructor(init?: Partial<MonthSaleTableRowDto>) {
    super()
    Object.assign(this, init)
  }
}

export enum SaleStatDetailType {
  销售 = 'order',
  送货 = 'delivery',
  退货 = 'return'
}

export class MonthSaleWeekDetailQueryDto {
  includeTax: boolean = false
  month: string = ''
  weekNo: number = 0
  customerId: number | null = null
  filterCategory: boolean = false
  categoryId: number | null = null
  customerName: string = ''
  //前端使用：选择接口和显示标题，金额保留点击时的报表值。
  detailType: SaleStatDetailType = SaleStatDetailType.销售
  title: string = ''
  expectedAmount: number = 0
  startDate: string = ''
  endDate: string = ''

  constructor(init?: Partial<MonthSaleWeekDetailQueryDto>) {
    Object.assign(this, init)
  }
}

export class MonthSaleWeekDetailDto {
  id: string = ''
  orderId: number = 0
  orderNo: string = ''
  saleOrderNo: string = ''
  customerId: number = 0
  customerName: string = ''
  dateTime: string = ''
  detailId: number | null = null
  detailNo: string = ''
  customerOrderNo: string = ''
  customerMaterialNo: string = ''
  productNo: string = ''
  productName: string = ''
  specification: string = ''
  version: string = ''
  unit: string = ''
  quantity: number | null = null
  price: number | null = null
  priceHaveTax: number | null = null
  totalPrice: number | null = null
  totalPriceHaveTax: number | null = null
  orderTotalPrice: number | null = null
  orderTotalPriceHaveTax: number | null = null
  moldCost: number | null = null
  returnQuantity: number | null = null
  returnAmount: number | null = null
  returnAmountHaveTax: number | null = null
  remark: string = ''

  constructor(init?: Partial<MonthSaleWeekDetailDto>) {
    Object.assign(this, init)
  }
}

export class MonthSaleWeekDetailResultDto {
  totalAmount: number = 0
  orderCount: number = 0
  detailCount: number = 0
  rows: MonthSaleWeekDetailDto[] = []

  constructor(init?: Partial<MonthSaleWeekDetailResultDto>) {
    Object.assign(this, init)
  }
}
