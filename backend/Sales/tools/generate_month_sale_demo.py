"""Generate a marked, transactional batch for the legacy monthly report.

Credentials are supplied at runtime and never written to the batch manifest.
Without --apply the entire batch is validated and rolled back.
"""
import argparse
from datetime import datetime, timedelta
from decimal import Decimal
import getpass
import json
from pathlib import Path
import secrets

import pymysql


def next_month(date):
    return date.replace(year=date.year + (date.month == 12), month=date.month % 12 + 1)


def finance_weeks(year, month):
    current = datetime(year, month, 26, 9)
    last = next_month(current) - timedelta(days=1)
    result = []
    while current <= last:
        end = min(current + timedelta(days=(4 - current.weekday()) % 7), last)
        result.append(current)
        current = end + timedelta(days=1)
    return result


def json_default(value):
    if isinstance(value, Decimal):
        return str(value)
    if isinstance(value, datetime):
        return value.isoformat()
    raise TypeError(type(value).__name__)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--host', required=True)
    parser.add_argument('--port', type=int, default=3306)
    parser.add_argument('--user', required=True)
    parser.add_argument('--database', required=True)
    parser.add_argument('--year', type=int, default=2026)
    parser.add_argument('--month', type=int, default=3)
    parser.add_argument('--customers', type=int, default=8)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    if not 2 <= args.year <= 9998 or not 1 <= args.month <= 12 or not 1 <= args.customers <= 20:
        raise ValueError('Invalid reporting period or customer count')
    if not args.database.startswith('test'):
        raise ValueError('This generator is limited to test databases')
    batch = 'KZ-DEMO-' + datetime.now().strftime('%Y%m%d%H%M%S') + '-' + secrets.token_hex(3).upper()
    mark = batch + ' 月报模拟数据（订单/送货，非库存操作）'
    output = Path(__file__).resolve().parents[1] / 'data' / batch
    connection = pymysql.connect(host=args.host, port=args.port, user=args.user,
        password=getpass.getpass('Database password: '), database=args.database,
        charset='utf8mb4', connect_timeout=8, read_timeout=30, write_timeout=30,
        autocommit=False, cursorclass=pymysql.cursors.DictCursor)
    inserted = {table: [] for table in ['SaleOrder', 'SaleOrderDetail', 'DeliveryOrder', 'DeliveryOrderDetail']}
    manifest = {'batch': batch, 'database': args.database, 'period': f'{args.year}-{args.month:02d}',
        'committed': False, 'ids': inserted, 'periods': {}, 'customer_ids': []}
    try:
        with connection.cursor() as cursor:
            cursor.execute("SELECT TABLE_NAME,ENGINE FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('SaleOrder','SaleOrderDetail','DeliveryOrder','DeliveryOrderDetail')")
            engines = cursor.fetchall()
            if len(engines) != 4 or any(row['ENGINE'] != 'InnoDB' for row in engines):
                raise ValueError('All four target tables must support transactions')
            cursor.execute("SELECT COUNT(*) AS n FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() AND EVENT_OBJECT_TABLE IN ('SaleOrder','SaleOrderDetail','DeliveryOrder','DeliveryOrderDetail')")
            if cursor.fetchone()['n']:
                raise ValueError('Inspect trigger side effects before generating a batch')
            cursor.execute('''SELECT s.Id AS OrderId,d.Id AS DetailId,s.CustomerId,s.EnterpriseId
                FROM SaleOrder s JOIN SaleOrderDetail d ON d.SaleOrderId=s.Id
                JOIN Customer c ON c.Id=s.CustomerId AND c.EnterpriseId=s.EnterpriseId
                JOIN Base_Partner p ON p.Id=c.Id AND p.EnterpriseId=s.EnterpriseId AND TRIM(p.CustomerName)=TRIM(c.Name)
                JOIN HistoryProduct h ON h.Id=d.HistoryProductId AND h.EnterpriseId=s.EnterpriseId
                JOIN Product product ON product.Id=d.ProductId AND product.EnterpriseId=s.EnterpriseId
                JOIN `User` u ON u.Id=s.ReceiveUserId AND u.EnterpriseId=s.EnterpriseId
                WHERE c.IsDeleted=0 AND p.IsDeleted=0 AND h.IsDeleted=0 AND u.IsDeleted=0
                AND s.Status=1 AND s.AuditStatus=2 AND d.Status=1 AND d.IsReview=1 AND d.Price>0
                AND COALESCE(s.Remark,'') NOT LIKE 'KZ-DEMO-%'
                ORDER BY s.CreateTime DESC,d.Id DESC''')
            candidates = cursor.fetchall()
            selected = []
            for row in candidates:
                if row['CustomerId'] not in {item['CustomerId'] for item in selected}:
                    selected.append(row)
                if len(selected) == args.customers:
                    break
            if len(selected) != args.customers:
                raise ValueError('Not enough validated customer/product templates')
            manifest['customer_ids'] = [row['CustomerId'] for row in selected]
            weeks = finance_weeks(args.year, args.month)
            start = datetime(args.year, args.month, 26, 9)
            previous = (start.replace(day=1) - timedelta(days=1)).replace(day=26, hour=9)
            periods = [('current', weeks), ('previous', [previous]),
                ('last_year', [start.replace(year=args.year - 1)]),
                ('january', [datetime(args.year, 1, 26, 9)])]
            seen_periods = set()

            def insert(table, row):
                values = {key: value for key, value in row.items() if key != 'Id'}
                cursor.execute('INSERT INTO `' + table + '` (' + ','.join('`' + key + '`' for key in values)
                    + ') VALUES (' + ','.join(['%s'] * len(values)) + ')', tuple(values.values()))
                inserted[table].append(cursor.lastrowid)
                return cursor.lastrowid

            sequence = 0
            for period, dates in periods:
                period_key = (dates[0].year, dates[0].month)
                if period_key in seen_periods:
                    continue
                seen_periods.add(period_key)
                summary = {'orders': 0, 'deliveries': 0, 'order_amount': Decimal(0), 'delivery_amount': Decimal(0)}
                manifest['periods'][period] = summary
                for index, template in enumerate(selected):
                    cursor.execute('SELECT * FROM SaleOrder WHERE Id=%s', (template['OrderId'],))
                    order_template = cursor.fetchone()
                    cursor.execute('SELECT * FROM SaleOrderDetail WHERE Id=%s', (template['DetailId'],))
                    detail_template = cursor.fetchone()
                    for week_index, date in enumerate(dates):
                        sequence += 1
                        quantity = 6000 + index * 1200 + week_index * 850
                        if period != 'current':
                            factor = Decimal('0.65') if index % 2 == 0 else Decimal('1.35')
                            quantity = int(sum(6000 + index * 1200 + k * 850 for k in range(len(weeks))) * factor)
                        delivered = quantity * (55 + (index + week_index) % 5 * 8) // 100
                        price = detail_template['Price']
                        order_amount = (price * quantity).quantize(Decimal('0.0001'))
                        delivery_amount = price * delivered
                        if order_amount > Decimal('99999999.9999'):
                            raise ValueError('Generated amount exceeds the order field precision')
                        audit = dict(CreateTime=date, UpdateTime=date, CreateUserNo='KZ-DEMO',
                            UpdateUserNo='KZ-DEMO', CreateUserName='月报测试数据', UpdateUserName='月报测试数据')
                        order = {**order_template, **audit, 'OrderNo': f'{batch}-SO-{sequence:03d}',
                            'Remark': mark, 'CustomerOrderNo': f'{batch}-CUSTOMER-{sequence:03d}',
                            'OrderTotalPrice': order_amount, 'ReceiveOrderTime': date}
                        order_id = insert('SaleOrder', order)
                        detail = {**detail_template, **audit, 'SaleOrderId': order_id, 'Remark': mark,
                            'PlaceOrderQuantity': quantity, 'PickQuantity': 0, 'PackageQuantity': 0,
                            'DeliveryQuantity': delivered, 'DeliveryNoticeQuantity': delivered,
                            'TotalPrice': order_amount, 'MoldCost': 0, 'IsUrgent': 0, 'IsModel': 0,
                            'IsMoldAccount': 0, 'PickWight': 0, 'PickAbnormalWeight': 0,
                            'StockPlanQuantity': 0, 'DeliveryTime': date}
                        detail_id = insert('SaleOrderDetail', detail)
                        delivery_id = insert('DeliveryOrder', {**audit, 'OrderNo': f'{batch}-DO-{sequence:03d}',
                            'DeliveryAddress': order_template['CustomerAddress'], 'DeliveryTime': date,
                            'Remark': mark, 'DeliveryUserId': order_template['ReceiveUserId'],
                            'CustomerId': template['CustomerId'], 'EnterpriseId': template['EnterpriseId'],
                            'DeliveryMethod': '月报测试模拟直接送货'})
                        insert('DeliveryOrderDetail', {**audit, 'DeliveryQuantity': delivered,
                            'CustomerUsedQuantity': 0, 'ReturnQuantity': 0, 'AccountQuantity': 0,
                            'Remark': mark, 'SaleOrderDetailId': detail_id, 'DeliveryOrderId': delivery_id,
                            'ProductionId': None, 'EnterpriseId': template['EnterpriseId'],
                            'HistoryProductId': detail_template['HistoryProductId'], 'ProductionOrderNo': '',
                            'OutboundInspectionRecordId': None, 'DetailNo': f'{batch}-DD-{sequence:03d}',
                            'IsDirectDelivery': 1, 'IsOutboundInventory': 1, 'AbnormalQuantity': 0,
                            'IsAbnormal': 0, 'IsDeleted': 0, 'CustomerUsedAccountQuantity': 0,
                            'IsAccount': 0, 'DepartmentActualValueId': None, 'SaleRate': 0,
                            'SalePrice': price, 'CustomAttribute': '[]', 'NoticeQuantity': delivered})
                        summary['orders'] += 1
                        summary['deliveries'] += 1
                        summary['order_amount'] += order_amount
                        summary['delivery_amount'] += delivery_amount
            cursor.execute('''SELECT COUNT(*) AS n,SUM(s.OrderTotalPrice) AS order_amount,
                SUM(sd.Price*dd.DeliveryQuantity) AS delivery_amount,
                SUM(s.OrderTotalPrice<>sd.TotalPrice OR sd.TotalPrice<>sd.Price*sd.PlaceOrderQuantity
                  OR sd.DeliveryQuantity<>dd.DeliveryQuantity OR sd.DeliveryQuantity>sd.PlaceOrderQuantity
                  OR dd.SalePrice<>sd.Price OR dd.IsOutboundInventory<>1 OR dd.IsDeleted<>0
                  OR s.CustomerId<>d.CustomerId OR s.EnterpriseId<>d.EnterpriseId) AS errors
                FROM SaleOrder s JOIN SaleOrderDetail sd ON sd.SaleOrderId=s.Id
                JOIN DeliveryOrderDetail dd ON dd.SaleOrderDetailId=sd.Id
                JOIN DeliveryOrder d ON d.Id=dd.DeliveryOrderId JOIN Base_Partner p ON p.Id=s.CustomerId
                WHERE s.Remark=%s AND d.Remark=%s''', (mark, mark))
            verified = cursor.fetchone()
            if verified['n'] != sequence or verified['errors'] != 0:
                raise ValueError('Generated row linkage or amounts do not reconcile')
            if verified['order_amount'] != sum(value['order_amount'] for value in manifest['periods'].values()):
                raise ValueError('Order total verification failed')
            if verified['delivery_amount'] != sum(value['delivery_amount'] for value in manifest['periods'].values()):
                raise ValueError('Delivery total verification failed')
            for table, ids in inserted.items():
                cursor.execute('SELECT COUNT(*) AS n FROM `' + table + '` WHERE Remark=%s', (mark,))
                if cursor.fetchone()['n'] != len(ids):
                    raise ValueError('Batch count verification failed: ' + table)
            manifest['verified'] = verified
            output.mkdir(parents=True, exist_ok=False)
            cleanup = ['-- Remove only this generated batch; review before running.', 'START TRANSACTION;']
            for table in reversed(inserted):
                ids = ','.join(str(value) for value in inserted[table])
                cleanup.append(f"DELETE FROM `{table}` WHERE Id IN ({ids}) AND Remark LIKE '{batch}%';")
            cleanup.append('COMMIT;')
            (output / 'cleanup.sql').write_text('\n'.join(cleanup) + '\n', encoding='utf-8')
            (output / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2, default=json_default), encoding='utf-8')
            if args.apply:
                connection.commit()
                manifest['committed'] = True
            else:
                connection.rollback()
            (output / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2, default=json_default), encoding='utf-8')
            print(json.dumps({'batch': batch, 'committed': manifest['committed'], 'customers': len(selected),
                'counts': {table: len(ids) for table, ids in inserted.items()}, 'periods': manifest['periods'],
                'manifest': str(output / 'manifest.json'), 'cleanup': str(output / 'cleanup.sql')}, default=json_default))
    except Exception:
        connection.rollback()
        raise
    finally:
        connection.close()


if __name__ == '__main__':
    main()
