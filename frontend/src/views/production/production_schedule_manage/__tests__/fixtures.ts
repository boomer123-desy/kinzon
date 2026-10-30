import type { ScheduleNodeDto, ScheduleOrderDto } from '@/types/production-schedule'

export const product = {
    code: 'P-1001', name: '产品A', customerCode: 'KH-P001', specification: '500 × 300',
    technicalCode: 'TA-001', unit: 'PCS', orderCode: 'PS-20260001', version: 'V3',
    plannedQuantity: 100, start: '2026-08-28', end: '2026-09-05', owner: '张三'
}

export function createMockTree(): ScheduleNodeDto[] {
    const material = (id: string, name: string, code: string, specification: string,
        unit: string, baseDemand: number, stock: number, occupied = 0): ScheduleNodeDto => ({
        id, name, code, specification, unit, baseDemand, stock, occupied,
        kind: 'material', orders: [], children: []
    })
    const semi = (id: string, name: string, code: string, specification: string,
        baseDemand: number, stock: number, occupied: number, children: ScheduleNodeDto[] = [],
        orders: ScheduleOrderDto[] = []): ScheduleNodeDto => ({
        id, name, code, specification, baseDemand, stock, occupied, children, orders, unit: 'PCS', kind: 'semi'
    })
    const order = (id: string, quantity: number, start: string, end: string): ScheduleOrderDto => ({
        id, quantity, start: `2026-${start}T08:00`, end: `2026-${end}T18:00`
    })
    return [
        semi('a', '半成品A', 'SA-001', 'TA-001', 100, 20, 5, [
            semi('a-shell', '半成品A-外壳', 'SA-101', 'TA-101', 100, 40, 10, [
                material('steel', '钢板', 'M-1001', 'Q235', 'KG', 180, 200, 30),
                material('pipe', '钢管', 'M-1004', 'Φ20×2', 'KG', 200, 80)
            ]),
            semi('a-fastener', '半成品A-紧固件', 'SA-102', 'TA-102', 100, 60, 5, [
                material('screw', '螺丝', 'M-1002', 'M6×20', 'PCS', 720, 500, 100)
            ]),
            material('packaging', '包装材料', 'M-2001', 'BC-001', 'PCS', 100, 120)
        ], [order('APS-001', 20, '08-28', '08-29'), order('APS-002', 20, '08-31', '09-01')]),
        semi('b', '半成品B', 'SB-002', 'TB-002', 90, 30, 10, [
            semi('b-core', '半成品B-芯体', 'SB-201', 'TB-201', 90, 25, 5, [
                material('aluminium', '铝材', 'M-1003', '6061', 'KG', 120, 80, 10)
            ]),
            semi('b-seal', '半成品B-密封件', 'SB-202', 'TB-202', 90, 30, 0, [
                material('rubber', '橡胶圈', 'M-1005', 'Φ30', 'PCS', 180, 150)
            ])
        ], [order('APS-003', 30, '08-28', '08-30'), order('APS-004', 20, '08-31', '09-02')]),
        semi('c', '半成品C', 'SC-003', 'TC-003', 50, 10, 0, [
            semi('c-surface', '半成品C-表面处理', 'SC-301', 'TC-301', 50, 25, 5, [
                material('paint', '涂料', 'M-1006', '蓝色面漆', 'KG', 40, 30, 5),
                material('thinner', '稀释剂', 'M-1007', '标准型', 'KG', 20, 30)
            ]),
            material('label', '标签', 'M-2010', 'BC-201', 'PCS', 50, 60),
            semi('c-kit', '半成品C-辅料包', 'SC-302', 'TC-302', 50, 10, 0, [
                material('adhesive', '胶粘剂', 'M-1008', 'AB型', 'KG', 15, 5)
            ])
        ], [order('APS-005', 20, '08-31', '09-01')]),
        semi('d', '半成品D', 'SD-004', 'TD-004', 80, 0, 0, [
            material('washer', '垫片', 'M-1009', 'M8', 'PCS', 160, 200)
        ]),
        semi('e', '半成品E', 'SE-005', 'TE-005', 30, 40, 0, [
            material('film', '保护膜', 'M-1010', 'PE透明', 'M', 60, 100)
        ]),
        semi('f', '半成品F', 'SF-006', 'TF-006', 20, 25, 0)
    ]
}

