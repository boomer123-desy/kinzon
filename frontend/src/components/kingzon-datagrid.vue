<template>
  <div
    :style="{ position: 'relative', left: downloadLeft + 'px', top: top + 'px', zIndex: '999', height: '0px', width: '0px' }"
    v-if="showDownLoad">
    <font-awesome-icon :icon="['fas', 'download']" :style="{ width: '20px', height: '14px' }" class="kingzon-pointer"
      @click="downloadData" v-if="!isCheckBoxSelection || isDownLoad" />
  </div>
  <div
    :style="{ position: 'fixed', top: filterPosition.top + 'px', left: filterPosition.left + 'px', height: '0px', zIndex: '999' }"
    v-if="showFieldFilter">
    <span v-Tooltip="getFieldFilterContent()">
      <font-awesome-icon :icon="['far', 'list-alt']" style="width:20px;height:20px;" class="kingzon-pointer" />
    </span>
  </div>
  <!-- <KingZonCopy></KingZonCopy> -->
  <DataGrid ref="dataGrid" :columnMoving="true" @columnMove="onColumnMoved" :columnResizing="true" :multiSort="true"
    :pagination="true" :lazy="true"
    :pageLayout="['list', 'first', 'prev', 'sep', 'links', 'sep', 'next', 'last', 'sep', 'refresh', 'sep', 'tpl']"
    :pageSize="pageSize" :pageNumber="pageIndex" :pageList="[20, 50, 100, 1000]" :total="total" :loading="isDataLoading"
    @pageChange="onPageChange" @sortChange="onSortChange" @columnInited="onColumnInitedHandler"
    @columnResize="onColumnResizeHandler" @rendered="onRenderedHandler" v-bind="$attrs"
    :virtualScroll="virtualScrollLocal">
    <GridColumnGroup v-if="isCheckBoxSelection" :frozen="true" align="left" :width="leftGroupRowWidth"
      :columnMoving="true">
      <GridHeaderRow>
        <GridColumn field="ck" :width="40" align="center" v-if="isShowCheckBox">
          <template v-slot:header>
            <CheckBox :model-value="allChecked" @click="onAllCheckedClick()"></CheckBox>
          </template>
          <template v-slot:body="scope">
            <CheckBox v-if="!scope.row.unCheckeText" :model-value="scope.row.selected"
              :disabled="scope.row.checkBoxDisabled" @click="onCheckBoxClicked(scope.row)">
            </CheckBox>
            <div v-else>{{ scope.row.unCheckeText }}</div>
          </template>
        </GridColumn>
        <slot name="left"></slot>
      </GridHeaderRow>
      <slot name="multiLeftHeaderRow"></slot>
    </GridColumnGroup>
    <GridColumnGroup v-if="isCheckBoxSelection && $slots.center" width="200" align="center" :columnMoving="true">
      <GridHeaderRow>
        <slot name="center"></slot>
      </GridHeaderRow>
      <slot name="multiCenterHeaderRow"></slot>
    </GridColumnGroup>
    <GridColumnGroup v-if="isCheckBoxSelection && $slots.right" :frozen="true" align="right"
      :width="rightGroupRowWidth">
      <GridHeaderRow>
        <slot name="right"></slot>
      </GridHeaderRow>
      <slot name="multiRightHeaderRow"></slot>
    </GridColumnGroup>
    <slot v-if="!isCheckBoxSelection"></slot>
    <template #detail="scope">
      <slot name="detail" :row="scope.row"></slot>
    </template>
    <template #group="scope">
      <slot name="group" :row="scope.row"></slot>
    </template>
    <template #tpl>
      <div style="margin-left: 5px;">
        <span>跳至</span>
        <NumberBox :value="pageIndex" :spinners="true" :min="1" :max="totalPage" :inputStyle="{ textAlign: 'right' }"
          style="width:70px; height:21px;margin:0 4px" @valueChange="onPageIndexBoxChange">
        </NumberBox>
        <span>页</span>
      </div>
    </template>
  </DataGrid>

</template>

<script lang="ts">
import { isNumber, throttle } from 'lodash';
import { defineComponent, markRaw } from 'vue';
import FieldFilter from "./field-filter.vue";
import { type ColumnInfo, GridColumnType, type SaveContent } from './types.js';
import { LayoutObserver } from '@/utils/LayoutObserver.js';

export default defineComponent({
  name: 'KingZonDataGrid',
  emits: ['rendered', 'seletedRowsChanged', "singleRowChange"],
  props: {
    //默认选中的数据
    initSelectedRows: {
      type: Array<any>,
      default: () => []
    },
    isCheckBoxSelection: {
      type: Boolean,
      default: false
    },
    leftGroupRowWidth: {
      type: Number,
      default: 200
    },
    rightGroupRowWidth: {
      type: Number,
      default: 200
    },
    //是否显示选择列
    isShowCheckBox: {
      type: Boolean,
      default: true
    },
    showDownLoad: {
      type: Boolean,
      default: false
    },
    exportFileName: {
      type: String,
      default: '导出数据'
    },
    downloadLeft: {
      type: Number,
      required: false,
      default: 10
    },
    isDownLoad: {
      type: Boolean,
      default: false
    },
    virtualScroll: {
      type: Boolean,
      default: false
    },
    isDownloadRight: {
      type: Boolean,
      default: false
    },
    showFieldFilter: {
      type: Boolean,
      default: true
    },
    splitFields: {
      type: Array<String>,
      default: []
    }
  },
  data() {
    return {
      pageIndex: 1,
      pageSize: 20,
      total: 0,
      sorts: [] as any[],
      selectedRows: [] as any[],
      isDataLoading: false,
      isPageChanging: false,
      isKeepSelected: false,
      isMounted: false,
      top: 6,
      virtualScrollLocal: this.virtualScroll,
      filterPosition: {
        top: 0,
        left: 0
      },
      observer: null as LayoutObserver | null
    }
  },
  watch: {
    '$attrs.data': {
      handler: function (value) {
        if (this.isPageChanging || this.isMounted) {
          this.isMounted = false;
          if (value?.length > 0) {
            this.isDataLoading = true;
          }
        }

      },
      deep: true,
      flush: 'pre'
    },
    '$attrs.seletedRows': {
      handler: function (value) {
        if (value && value.length && value.length > 0) {
          this.isDataLoading = true;
        }
      },
      deep: true
    },
    initSelectedRows: {
      handler: function (value) {
        this.selectedRows = JSON.parse(JSON.stringify(value));
      },
      deep: true
    },
    virtualScroll: {
      handler: function (value) {
        this.virtualScrollLocal = value;
      }
    }
  },
  computed: {
    storageName() {
      return `${this.$route.path}_datagrid_${this.$attrs.id ? this.$attrs.id : 'default'}`;
    },
    dataGrid() {
      return this.$refs.dataGrid as EasyUI.DataGrid;
    },
    checkedRows() {
      return this.dataGrid.data.filter(row => row.selected);
    },
    idField(): string {
      return this.$attrs.idField as string;
    },
    addedRows() {
      return this.selectedRows.filter(item => this.initSelectedRows.findIndex(row => row[this.idField] === item[this.idField]) === -1);
    },
    removedRows() {
      return this.initSelectedRows.filter(item => this.selectedRows.findIndex(row => row[this.idField] === item[this.idField]) === -1);
    },
    allChecked() {
      let allRows = this.dataGrid.data.filter(row => !row.checkBoxDisabled || row.selected).length;
      return allRows > 0 && this.checkedRows.length === allRows;
    },
    totalPage() {
      if (this.total === 0) {
        return 1;
      }
      return Math.ceil(this.total / this.pageSize);
    },
    //在使用VirturalScroll时，DataGrid中当前渲染的数据（含缓存数据）
    frozenRows() {
      var dataGrid = this.$refs.dataGrid as EasyUI.DataGrid;
      return dataGrid.frozenRows;
    },
    columnType(): GridColumnType {
      if (this.dataGrid.groupRefs.length === 0) {
        return GridColumnType.ColumnRefs;
      } else {
        return GridColumnType.GroupRefs;
      }
    }
  },
  mounted() {
    this.isMounted = true;
    this.reOrderColumns();
    this.selectedRows = JSON.parse(JSON.stringify(this.initSelectedRows));

    // const observer = new ResizeObserver(() => {
    //   var boundingRect = this.dataGrid.$el.getBoundingClientRect();
    //   this.filterPosition.top = boundingRect.top - 20;
    //   this.filterPosition.left = boundingRect.right - 35;
    //   console.log('filterPosition1111', boundingRect);
    // });
    // observer.observe(this.dataGrid.$el);

    var that = this;
    this.observer = new LayoutObserver(this.dataGrid.$el, {
      onLayoutChanged() {
        var boundingRect = that.dataGrid.$el.getBoundingClientRect();
        that.filterPosition.top = boundingRect.top - 20;
        that.filterPosition.left = boundingRect.right - 35;
      }

    });
    this.observer.observe();
  },
  unmounted() {
    this.observer?.disconnect();
  },
  created() {
    // this.$watch('data', throttle((value) => {
    //   this.initData(value);
    // }, 2000), { deep: true });

    this.onRenderedHandler = throttle(() => {
      this.onRendered();
    }, 100);

    this.onColumnInitedHandler = throttle(() => {
      this.onColumnInited();
    }, 100);

    this.onColumnResizeHandler = throttle(() => {
      this.onColumnResize();
    }, 100);

    this.sorts = this.$attrs.sorts ? this.$attrs.sorts as any[] : [];

  },
  methods: {
    //--------------------保存移动后的列顺序-----------------
    onColumnResize() {
      this.saveColumnOrder();
    },
    onColumnResizeHandler() {
    },
    //列移动
    onColumnMoved() {
      //保存移动后的列顺序
      this.saveColumnOrder();
    },
    //保存列顺序,这里因为有嵌套引用，只能保存主要字段
    //在恢复时需要根据字段来恢复列的顺序和冻结状态
    saveColumnOrder() {
      let content = {
        columnType: this.columnType
      }

      if (this.columnType === GridColumnType.ColumnRefs) {
        const columnRefs = this.dataGrid.columnRefs.map((item) => {
          return this._createColumnInfo(item);
        });
        Object.assign(content, { columnRefs: columnRefs });
      } else {
        const leftHeaderColumns = this.dataGrid.leftGroup.rows[0].columns.map((item) => {
          return this._createColumnInfo(item);
        });
        const centerHeaderColumns = this.dataGrid.centerGroup.rows[0].columns.map((item) => {
          return this._createColumnInfo(item);
        });
        Object.assign(content, {
          leftHeaderColumns: leftHeaderColumns,
          centerHeaderColumns: centerHeaderColumns
        })
      }
      localStorage.setItem(this.storageName, JSON.stringify(content));
    },
    _createColumnInfo(item: EasyUI.GridColumn): ColumnInfo {
      return {
        field: item.field,
        title: item.title,
        frozen: item.frozenState,
        hidden: item.hiddenState,
        width: item.widthState ? Number(item.widthState.replace('px', '')) : 40
      }
    },
    _restoreColumnInfo(column: EasyUI.GridColumn, columnInfo: ColumnInfo) {
      if (!column || !columnInfo) {
        return;
      }
      if (!column.field || !column.title) {
        return;
      }
      setTimeout(() => {
        column.frozenState !== columnInfo.frozen ? column.frozenState = columnInfo.frozen : null;
        column.hiddenState !== columnInfo.hidden ? column.hiddenState = columnInfo.hidden : null;
        if (column.width !== columnInfo.width) {
          this.dataGrid.resizeColumn(column.field, columnInfo.width);
        }
      }, 0)
    },
    reOrderColumns() {
      //获取保存的列信息
      const content = localStorage.getItem(this.storageName);
      if (!content) {
        return;
      }

      const columnInfo: SaveContent = JSON.parse(content);

      if (columnInfo.columnType === GridColumnType.ColumnRefs) {
        this._reOrderColumnRefs(columnInfo);
        return;
      }

      if (columnInfo.columnType === GridColumnType.GroupRefs) {
        this._reOrderGroupRefs(columnInfo);
        return;
      }
    },
    _reOrderColumnRefs(columnInfo: SaveContent) {
      columnInfo.columnRefs!.forEach((col: ColumnInfo, index: number) => {
        const columnIndex = this.dataGrid.columnRefs.findIndex((c: any) => col.title && col.field && c.field === col.field);
        const column = this.dataGrid.columnRefs[columnIndex];

        if (columnIndex !== -1 && columnIndex !== index) {
          this.dataGrid.columnRefs.splice(columnIndex, 1);
          this.dataGrid.columnRefs.splice(index, 0, column);
        }
        this._restoreColumnInfo(column, col);
      })
    },
    _reOrderGroupRefs(columnInfo: SaveContent) {
      //先处理所有的固定列和非固定列
      //1、在保存的固定列中，寻找还在 centerGroup 中的，移入leftGroup
      columnInfo.leftHeaderColumns!.forEach((col: any) => {
        const columnIndex = this.dataGrid.groupRefs[1].rows[0].columns.findIndex((c: any) => col.title && col.field && c.field === col.field);
        if (columnIndex > -1) {
          const column = this.dataGrid.groupRefs[1].rows[0].columns[columnIndex];
          column.rowspanState = this.dataGrid.groupRefs[0].rows.length;
          this.dataGrid.groupRefs[1].rows[0].columns.splice(columnIndex, 1);
          this.dataGrid.groupRefs[0].rows[0].columns.push(column);
        }
      });

      //2、现有的 leftGroup 中的列，寻找不需要固定的列，移入centerGroup
      const savedLeftHeaderColumns = new Set(columnInfo.leftHeaderColumns!.map((item) => {
        return item.field;
      }));

      const notFixedColumns = this.dataGrid.groupRefs[0].rows[0].columns
        .filter((col: any) => !savedLeftHeaderColumns.has(col.field))
        .map((col: any) => {
          return this._createColumnInfo(col);
        });

      notFixedColumns.forEach((col: any) => {
        const columnIndex = this.dataGrid.groupRefs[0].rows[0].columns.findIndex((c: any) => col.title && col.field && c.field === col.field);
        if (columnIndex > -1) {
          const column = this.dataGrid.groupRefs[0].rows[0].columns[columnIndex];
          column.rowspanState = this.dataGrid.groupRefs[1].rows.length;
          this.dataGrid.groupRefs[0].rows[0].columns.splice(columnIndex, 1);
          this.dataGrid.groupRefs[1].rows[0].columns.splice(0, 0, column);
        }
      });

      //改变 leftGroup 的顺序
      columnInfo.leftHeaderColumns!.forEach((col: any, index: number) => {
        const columnIndex = this.dataGrid.groupRefs[0].rows[0].columns.findIndex((c: any) => col.title && col.field && c.field === col.field);
        if (columnIndex > -1 && columnIndex !== index) {
          const column = this.dataGrid.groupRefs[0].rows[0].columns[columnIndex];
          this.dataGrid.groupRefs[0].rows[0].columns.splice(columnIndex, 1);
          this.dataGrid.groupRefs[0].rows[0].columns.splice(index, 0, column);
        }
      });
      //改变 centerGroup 的顺序
      columnInfo.centerHeaderColumns!.forEach((col: any, index: number) => {
        const columnIndex = this.dataGrid.groupRefs[1].rows[0].columns.findIndex((c: any) => col.title && col.field && c.field === col.field);
        if (columnIndex > -1 && columnIndex !== index) {
          const column = this.dataGrid.groupRefs[1].rows[0].columns[columnIndex];
          this.dataGrid.groupRefs[1].rows[0].columns.splice(columnIndex, 1);
          this.dataGrid.groupRefs[1].rows[0].columns.splice(index, 0, column);
        }
      });

      //再处理所有列的状态
      const allColumnInfo = [...columnInfo.leftHeaderColumns!, ...columnInfo.centerHeaderColumns!];
      this.dataGrid.groupRefs[0].rows[0].columns.forEach((col: any) => {
        const column = allColumnInfo.find((c: any) => col.title && col.field && c.field === col.field);
        if (column) {
          this._restoreColumnInfo(col, column);
        }
      });

      this.dataGrid.groupRefs[1].rows[0].columns.forEach((col: any) => {
        const column = allColumnInfo.find((c: any) => col.title && col.field && c.field === col.field);
        if (column) {
          this._restoreColumnInfo(col, column);
        }
      });
    },

    //列初始化完成
    onColumnInited() {

    },
    onColumnInitedHandler() {
    },
    //------------------------------------------------------

    //分页改变
    onPageChange(params: { pageNumber: number, pageSize: number }) {
      this.isPageChanging = true;
      this.pageIndex = params.pageNumber;
      this.pageSize = params.pageSize;
    },

    //跳转到指定页
    onPageIndexBoxChange(value) {
      if (value.currentValue > 0) {
        this.pageIndex = value.currentValue;
      }
    },

    //排序改变
    onSortChange(sorts) {
      this.sorts = sorts;
    },

    //--------分页时保持选中---------
    //保持选中
    keepSelected(data) {
      this.isKeepSelected = true;
      data.forEach((item) => {
        if (this.selectedRows.findIndex(row => row[this.idField] === item[this.idField]) > -1) {
          item.selected = true;
        }
      })
      this.$nextTick(() => {
        this.isKeepSelected = false;
      })
    },

    //全选
    onAllCheckedClick() {
      var checked = !this.allChecked;
      this.dataGrid.data.forEach((item) => {
        if (item.checkBoxDisabled) {
          return;
        }
        item.selected = checked;
        this.changeSelectedRows(item);
      })
      this.$emit('seletedRowsChanged', this.selectedRows);
    },
    //行选中点击事件
    onCheckBoxClicked(row) {
      if (row.checkBoxDisabled) {
        return;
      }
      row.selected = !row.selected;
      this.changeSelectedRows(row);
      this.$emit('seletedRowsChanged', this.selectedRows);
      this.$emit('singleRowChange', row);
    },
    //更新selectedRows
    changeSelectedRows(row) {
      if (row.selected) {
        if (this.selectedRows.findIndex(item => item[this.idField] === row[this.idField]) === -1) {
          this.selectedRows.push(this.checkedRows.find(item => item[this.idField] === row[this.idField]));
        }
      } else {
        const index = this.selectedRows.findIndex(item => item[this.idField] === row[this.idField]);
        if (index > -1) {
          this.selectedRows.splice(index, 1);
        }
      }
    },
    //------------------------------

    //重新计算datagrid高度
    resizeDataGrid() {

      var datagrid = this.dataGrid.$el;
      var allTables = datagrid.querySelectorAll('.datagrid-btable') as Array<HTMLTableElement>;
      //除掉 detail 表格
      var tables = Array.from(allTables).filter(table => {
        return !table.closest('.datagrid-row-detail');
      });

      if (tables.length <= 1) {
        return;
      }
      var trList = Array<any>();
      tables.forEach((table) => {
        var allTrs = table.querySelectorAll('tr');
        //除掉 detail下的tr
        var trs = Array.from(allTrs).filter(tr => {
          return !tr.closest('.datagrid-row-detail');
        });
        trList.push(trs);
      })

      for (var j = 0; j < trList[0].length; j++) {
        var maxHeight = 0;
        for (var i = 0; i < tables.length; i++) {
          if (trList[i][j] != undefined) {
            var height = 0; //行的高度
            var allTd = trList[i][j].querySelectorAll('td');
            if (allTd.length > 0) {
              //所有td中的最大高度
              var tmpHeight = 0;
              for (var g = 0; g < allTd.length; g++) {
                //td的高度 / td的rowspan 作为行的高度
                var tmp = allTd[g].offsetHeight / (allTd[g].rowSpan ? allTd[g].rowSpan : 1);
                tmpHeight = Math.max(tmpHeight, tmp);
              }
              height = Math.ceil(tmpHeight);
            } else {
              //找上一行的tr高度
              if (j > 0 && trList[i][j - 1] != undefined) {
                height = trList[i][j - 1].style.height.replace('px', '');
              }
            }
            maxHeight = Math.max(maxHeight, height);
          }
        }
        if (maxHeight == 0) {
          maxHeight = 32;
        }
        for (var k = 0; k < tables.length; k++) {
          if (trList[k][j] != undefined) {
            trList[k][j].style.height = maxHeight + 'px';
          }
        }
      }


    },
    //检查数据完整性
    checkCheckBoxSelection() {
      if (this.isCheckBoxSelection && !this.idField) {
        this.$kingzon_messager.showError('请设置idField属性');
      }
    },
    //渲染完成
    onRendered() {
      //this.copyCell();
      this.checkCheckBoxSelection();

      if (this.isCheckBoxSelection) {
        this.keepSelected(this.$attrs.data);
      }
      this.resizeDataGrid();
      this.$emit('rendered');
      if (this.isDataLoading) {
        this.$nextTick(() => {
          this.isDataLoading = false;
          this.isPageChanging = false;
        })
      }
      this.initTop();

    },
    onRenderedHandler() {

    },
    //-----------------下载数据----------------
    initTop() {
      //获取表头的高度
      const table = this.$refs.dataGrid as EasyUI.DataGrid;
      if (!table) {
        return;
      }
      var header = table.$el.querySelectorAll(".datagrid-header-inner")
      var headerLeft = header[0] as HTMLElement
      setTimeout(() => {
        var height = headerLeft.style.height;
        var topValue = (parseInt(height.replace('px', '')) - 19) / 2
        var datagridMarginTop = table.$el.style.marginTop;
        if (!datagridMarginTop) {
          datagridMarginTop = '0px';
        }
        var marginTopValue = parseInt(datagridMarginTop.replace('px', '')); //获取datagrid的marginTop
        if (!isNumber(marginTopValue)) {
          marginTopValue = 0;
        }
        this.top = topValue + marginTopValue;
      })
    },
    downloadData() {

      const table = this.$refs.dataGrid as EasyUI.DataGrid;
      if (this.virtualScroll) {
        this.virtualScrollLocal = false;
        table.setData(this.$attrs.data as any[]);
      }

      setTimeout(() => {
        const headers = table.$el.querySelectorAll(".datagrid-htable")
        const headerLeft = headers[0]
        const headerCenter = headers[1]
        const headerRight = headers[2]

        const tables = table.$el.querySelectorAll(".datagrid-btable")
        const tableLeft = this.setTdNotNull(tables[0]);
        var tableCenter = this.setTdNotNull(tables[1]);
        const tableRight = this.setTdNotNull(tables[2]);

        const indexs = table.centerColumns.map((it, index) => {
          return { index, field: it.field }
        })
          .filter(it => this.splitFields.includes(it.field))
          .map(it => it.index)

        const { splitMax, splitUnits } = this.getTableArray(tableCenter, indexs)

        tableCenter = this.setMutltiTd(tableCenter, splitUnits)

        //用左表表头生成sheet
        const exportHeaderLeft = headerLeft.cloneNode(true) as HTMLElement;
        if (indexs.length != 0) {
          // 中间表加了一个单位行 左表头要补齐一行
          Array.from(Array.from(exportHeaderLeft.querySelectorAll<HTMLTableRowElement>("tr.datagrid-header-row"))[0].children).forEach((td: any) => {
            td.rowSpan = headerCenter.querySelectorAll(
              "tr.datagrid-header-row"
            ).length + 1
          })
        }

        var mainSheet = this.$xlsx.utils.table_to_sheet(exportHeaderLeft, { raw: true });
        var mainRange = this.$xlsx.utils.decode_range(mainSheet['!ref']);
        var endColumn = mainRange.e.c;


        //添加中间表表头
        var origin = { c: endColumn + 1, r: 0 };
        if (headerCenter) {
          const exportHeaderCenter = this.buildExportHeaderWithUnits(headerCenter as HTMLElement, splitUnits);

          this.$xlsx.utils.sheet_add_dom(mainSheet, exportHeaderCenter, { raw: true, origin: origin });
          mainRange = this.$xlsx.utils.decode_range(mainSheet['!ref']);
        }
        var endRow = mainRange.e.r;

        //添加左表数据
        origin = { c: 0, r: endRow + 1 };
        this.$xlsx.utils.sheet_add_dom(mainSheet, tableLeft, { raw: true, origin: origin });

        //添加中间表数据
        if (tableCenter) {
          origin = { c: endColumn + 1, r: endRow + 1 };
          this.$xlsx.utils.sheet_add_dom(mainSheet, tableCenter, { raw: true, origin: origin });
          endColumn = mainRange.e.c;
          endRow = mainRange.e.r;
        }

        //如果是下载右侧表格
        if (this.isDownloadRight && headerRight && tableRight) {
          //添加右表表头
          origin = { c: endColumn + 1, r: 0 };
          this.$xlsx.utils.sheet_add_dom(mainSheet, headerRight, { raw: true, origin: origin });
          mainRange = this.$xlsx.utils.decode_range(mainSheet['!ref']);

          //添加右表数据
          origin = { c: endColumn + 1, r: endRow + 1 };
          this.$xlsx.utils.sheet_add_dom(mainSheet, tableRight, { raw: true, origin: origin });
        }

        mainRange = this.$xlsx.utils.decode_range(mainSheet['!ref']);
        for (let i = 0; i <= mainRange.e.c; i++) {
          for (let j = 0; j <= mainRange.e.r; j++) {
            let cell = this.$xlsx.utils.encode_cell({ c: i, r: j });
            if (mainSheet[cell]) {
              mainSheet[cell].s = {
                border: {
                  top: { style: 'thin', color: { rgb: '000000' } },
                  bottom: { style: 'thin', color: { rgb: '000000' } },
                  left: { style: 'thin', color: { rgb: '000000' } },
                  right: { style: 'thin', color: { rgb: '000000' } },
                }
              }
            } else {
              mainSheet[cell] = {
                v: '',
                s: {
                  border: {
                    top: { style: 'thin', color: { rgb: '000000' } },
                    bottom: { style: 'thin', color: { rgb: '000000' } },
                    left: { style: 'thin', color: { rgb: '000000' } },
                    right: { style: 'thin', color: { rgb: '000000' } }
                  }
                }
              }
            }

          }
        }

        var columnWidths = [] as any[];

        for (const key in mainSheet) {
          if (key.indexOf('!') == -1) {
            var cell = mainSheet[key];
            var cellAddress = this.$xlsx.utils.decode_cell(key);
            var length = this.getCustomLength(cell.v.toString());
            if (cellAddress.c > columnWidths.length - 1) {
              columnWidths.push(length);
            } else {
              columnWidths[cellAddress.c] = Math.max(columnWidths[cellAddress.c], length);
            }

            //如果是表头，添加字体样式
            if (cellAddress.r <= endRow) {
              cell.s.font = { bold: true, name: '宋体', color: { rgb: "000000" } };
              cell.s.alignment = {
                //horizontal: 'center',
                vertical: 'center',
                wrapText: true,
              };
            } else {
              cell.s.font = { bold: false, name: '宋体', color: { rgb: "000000" } };
              cell.s.alignment = {
                vertical: 'center',
                wrapText: true,
              };
            }
          }
        }

        mainSheet['!cols'] = columnWidths.map((item) => {
          return { wch: item }
        });

        var workbook = this.$xlsx.utils.book_new();
        this.$xlsx.utils.book_append_sheet(workbook, mainSheet, 'sheet1');
        var time = Date.now() //当前时间戳
        this.$xlsx.writeFile(workbook, this.exportFileName + this.$utils.formatDateTime(time, 'yyyyMMddHHmmss') + '.xlsx');
        if (this.virtualScroll) {
          this.virtualScrollLocal = true;
          table.setData(this.$attrs.data as any[]);
        }
      }, 10)
    },
    getHeaderLogicalCells(header: HTMLElement) {
      const rows = Array.from(header.querySelectorAll<HTMLTableRowElement>(
        "tr.datagrid-header-row"
      )
      );

      const occupied: boolean[][] = [];

      const cells: Array<{
        td: HTMLTableCellElement;
        rowIndex: number;
        startColumn: number;
        endColumn: number;
        rowspan: number;
        colspan: number;
      }> = [];

      rows.forEach((row, rowIndex) => {
        if (!occupied[rowIndex]) {
          occupied[rowIndex] = [];
        }

        let logicalColumnIndex = 0;

        const tds = Array.from(row.children)
          .filter((element): element is HTMLTableCellElement => {
            if (element.tagName !== "TD") {
              return false;
            }
            const td = element as HTMLTableCellElement;
            return (
              td.style.display !== "none" &&
              td.getAttribute("rowspan") !== "0" &&
              td.getAttribute("colspan") !== "0"
            );
          });

        tds.forEach(td => {
          // 跳过被上一行 rowspan 占用的逻辑列
          while (occupied[rowIndex] && occupied[rowIndex][logicalColumnIndex]) {
            logicalColumnIndex++;
          }

          const rowspan = Math.max(
            Number.parseInt(td.getAttribute("rowspan") || "1", 10) || 1,
            Number.parseInt(td.getAttribute("realrowspan") || "1", 10) || 1,
            1
          );

          const colspan = Math.max(
            Number.parseInt(td.getAttribute("colspan") || "1", 10) || 1,
            Number.parseInt(td.getAttribute("realcolspan") || "1", 10) || 1, 1
          );

          const startColumn = logicalColumnIndex;
          const endColumn = startColumn + colspan - 1;

          // 标记该单元格在表头网格中占用的位置
          for (let targetRow = rowIndex; targetRow < rowIndex + rowspan; targetRow++) {
            if (!occupied[targetRow]) {
              occupied[targetRow] = [];
            }

            for (let targetColumn = startColumn; targetColumn <= endColumn; targetColumn++) {
              occupied[targetRow][targetColumn] = true;
            }
          }

          cells.push({
            td,
            rowIndex,
            startColumn,
            endColumn,
            rowspan,
            colspan
          });
          logicalColumnIndex += colspan;
        });
      });
      return {
        rows,
        cells
      };
    },
    buildExportHeaderWithUnits(
      header: HTMLElement,
      splitUnits: Map<number, string[]>
    ) {
      // 克隆表头，不修改页面正在显示的DataGrid
      const exportHeader = header.cloneNode(true) as HTMLElement;

      const { rows, cells } = this.getHeaderLogicalCells(exportHeader);

      const originalRowCount = rows.length;

      if (originalRowCount === 0) {
        return exportHeader;
      }

      const splitEntries = Array.from(
        splitUnits.entries()
      )
        .filter(([, units]) => {
          return units.length > 0;
        })
        .sort((a, b) => {
          return a[0] - b[0];
        });

      if (splitEntries.length === 0) {
        return exportHeader;
      }

      /*
       * 记录每个拆分字段对应的叶子表头。
       *
       * key：centerColumns中的逻辑下标
       * value：对应的表头单元格信息
       */
      const targetCellMap = new Map<
        number,
        {
          td: HTMLTableCellElement;
          rowIndex: number;
          startColumn: number;
          endColumn: number;
          rowspan: number;
          colspan: number;
        }
      >();

      splitEntries.forEach(([splitIndex]) => {
        const targetCell = cells
          .filter(cell => {
            return (
              splitIndex >= cell.startColumn &&
              splitIndex <= cell.endColumn
            );
          })
          .filter(cell => {
            /*
             * 只选择原来能够覆盖到表头最底部的单元格。
             * 这种单元格一般就是叶子表头。
             */
            return (
              cell.rowIndex + cell.rowspan ===
              originalRowCount
            );
          })
          .sort((a, b) => {
            /*
             * 优先精确匹配单列的表头。
             */
            const aExact =
              a.startColumn === splitIndex &&
              a.endColumn === splitIndex;

            const bExact =
              b.startColumn === splitIndex &&
              b.endColumn === splitIndex;

            if (aExact !== bExact) {
              return aExact ? -1 : 1;
            }

            /*
             * 优先选择层级更深的表头。
             */
            if (a.rowIndex !== b.rowIndex) {
              return b.rowIndex - a.rowIndex;
            }

            /*
             * 同一层级优先选择跨度更小的表头。
             */
            return a.colspan - b.colspan;
          })[0];

        if (!targetCell) {
          console.warn(
            `没有找到逻辑列 ${splitIndex} 对应的叶子表头`
          );

          return;
        }

        targetCellMap.set(
          splitIndex,
          targetCell
        );
      });

      const targetTdSet = new Set(
        Array.from(targetCellMap.values())
          .map(item => item.td)
      );

      /*
       * 调整原表头中所有单元格的colspan和rowspan。
       */
      cells.forEach(cell => {
        let extraColumnCount = 0;

        /*
         * 一个拆分字段原来占1列，现在占units.length列。
         * 因此其自身和所有父级表头都要增加units.length - 1列。
         */
        splitEntries.forEach(
          ([splitIndex, units]) => {
            const containsSplitColumn =
              splitIndex >= cell.startColumn &&
              splitIndex <= cell.endColumn;

            if (containsSplitColumn) {
              extraColumnCount += Math.max(
                units.length - 1,
                0
              );
            }
          }
        );

        if (extraColumnCount > 0) {
          const newColspan =
            cell.colspan + extraColumnCount;

          cell.td.setAttribute(
            "colspan",
            String(newColspan)
          );

          cell.td.setAttribute(
            "realcolspan",
            String(newColspan)
          );

          cell.td.colSpan = newColspan + 1;
        }

        const reachesOriginalBottom =
          cell.rowIndex + cell.rowspan ===
          originalRowCount;

        /*
         * 现在会新增一行单位表头。
         *
         * 普通叶子表头需要继续覆盖新增加的单位行，
         * 因此rowspan增加1。
         *
         * 拆分字段的标题不能覆盖单位行，
         * 所以保留原来的rowspan。
         */
        if (
          reachesOriginalBottom &&
          !targetTdSet.has(cell.td)
        ) {
          const newRowspan =
            cell.rowspan + 1;

          cell.td.setAttribute(
            "rowspan",
            String(newRowspan)
          );

          cell.td.setAttribute(
            "realrowspan",
            String(newRowspan)
          );

          cell.td.rowSpan = newRowspan;
        }
      });

      /*
       * 创建新的最底层单位行。
       */
      const unitRow = document.createElement("tr");

      unitRow.className = "datagrid-header-row export-unit-row";



      splitEntries.forEach(
        ([splitIndex, units]) => {
          const targetCell = targetCellMap.get(splitIndex);

          if (!targetCell) {
            return;
          }

          const mainUnitTd = targetCell.td.cloneNode(true) as HTMLTableCellElement;
          mainUnitTd.rowSpan = 1;
          mainUnitTd.colSpan = 1;
          mainUnitTd.classList.add("export-unit-header");
          mainUnitTd.textContent = '主单位'
          unitRow.appendChild(mainUnitTd);

          units.forEach((unit, unitIndex) => {
            /*
             * 深度克隆原表头td，保留DataGrid的样式结构。
             */
            const unitTd = targetCell.td.cloneNode(true) as HTMLTableCellElement;
            unitTd.rowSpan = 1;
            unitTd.colSpan = 1;
            unitTd.classList.add("export-unit-header");

            const cellElement = unitTd.querySelector<HTMLElement>(".datagrid-cell");
            if (cellElement) {
              cellElement.textContent = unit;
              cellElement.title = unit;
            } else {
              unitTd.textContent = unit;
            }

            unitRow.appendChild(unitTd);
          });
        }
      );

      const tbody = rows[0].parentElement;

      if (tbody) {
        tbody.appendChild(unitRow);
      }

      return exportHeader;
    },
    setMutltiTd(
      table: HTMLElement | null,
      splitUnits: Map<number, string[]>
    ) {
      if (!table) {
        return table;
      }

      const tmp =
        table.cloneNode(true) as HTMLElement;

      /*
       * 必须在修改DOM之前计算逻辑列位置，
       * 否则插入新的td以后，下标会发生变化。
       */
      const logicalRows =
        this.getLogicalTableRows(tmp);

      logicalRows.forEach(rowCells => {
        /*
         * 从右向左处理。
         * 避免同一行存在多个splitFields时，
         * 前面的扩展影响后面的DOM位置。
         */
        const targetCells = rowCells
          .filter(({ index }) => {
            return splitUnits.has(index);
          })
          .sort((a, b) => {
            return b.index - a.index;
          });

        targetCells.forEach(({ td, index }) => {
          const units =
            splitUnits.get(index) || [];

          if (units.length === 0) {
            return;
          }

          const parent = td.parentElement;

          if (!parent) {
            return;
          }

          const texts = this.getTextArray(td);

          const textMap =
            new Map<string, string>();

          texts.forEach(text => {
            const unit =
              this.getQuantityUnit(text);

            if (!unit) {
              return;
            }

            if (!textMap.has(unit)) {
              textMap.set(unit, text);
            }
          });

          /*
           * 按表头单位的固定顺序生成数据。
           *
           * 没有对应单位时使用“/”补齐。
           */
          const values = units.map(unit => {
            return textMap.get(unit) || "/";
          });

          td.textContent =
            texts[0] || "/";

          td.setAttribute("colspan", "1");
          td.setAttribute("realcolspan", "1");

          let lastTd: HTMLElement = td;

          for (
            let valueIndex = 0;
            valueIndex < values.length;
            valueIndex++
          ) {
            const newTd =
              td.cloneNode(false) as HTMLElement;
            newTd.textContent =
              values[valueIndex];
            newTd.setAttribute("colspan", "1");
            newTd.setAttribute("realcolspan", "1");
            parent.insertBefore(
              newTd,
              lastTd.nextSibling
            );
            lastTd = newTd;
          }
        });
      });
      return tmp;
    },
    getLogicalTableRows(table: HTMLElement) {
      const result: Array<Array<{
        td: HTMLElement,
        index: number
      }>> = [];

      // 当前行被上一行 rowspan 占用的逻辑列
      let occupiedColumns = new Map<number, number>();
      const rows = Array.from(
        table.querySelectorAll<HTMLElement>("tr.datagrid-row")
      );
      rows.forEach(row => {
        const nextOccupiedColumns = new Map<number, number>();
        // 上一行的 rowspan 继续占用后续行
        occupiedColumns.forEach((remainingRows, columnIndex) => {
          if (remainingRows > 1) {
            nextOccupiedColumns.set(
              columnIndex,
              remainingRows - 1
            );
          }
        });
        const rowResult: Array<{
          td: HTMLElement,
          index: number
        }> = [];
        const tdList = Array.from(row.children)
          .filter(element => {
            return (
              element.tagName === "TD" &&
              element.classList.contains("datagrid-td")
            );
          }) as HTMLElement[];

        let logicalColumnIndex = 0;
        tdList.forEach(td => {
          const colspan = Math.max(Number.parseInt(td.getAttribute("colspan") || "1", 10), 1);
          const rowspan = Math.max(Number.parseInt(td.getAttribute("rowspan") || "1", 10), Number.parseInt(td.getAttribute("realrowspan") || "1", 10), 1);
          // 检查当前位置到 colspan 范围内是否被 rowspan 占用
          const isRangeOccupied = (startColumn: number) => {
            for (let column = startColumn; column < startColumn + colspan; column++) {
              if ((occupiedColumns.get(column) || 0) > 0) {
                return true;
              }
            }
            return false;
          };
          // 跳过上一行 rowspan 占用的列
          while (isRangeOccupied(logicalColumnIndex)) {
            logicalColumnIndex++;
          }
          rowResult.push({ td, index: logicalColumnIndex });
          // 当前单元格存在 rowspan，登记后续行占用
          if (rowspan > 1) {
            for (let column = logicalColumnIndex; column < logicalColumnIndex + colspan; column++) {
              const oldRemaining = nextOccupiedColumns.get(column) || 0;
              nextOccupiedColumns.set(column, Math.max(oldRemaining, rowspan - 1));
            }
          }
          logicalColumnIndex += colspan;
        });
        result.push(rowResult);
        occupiedColumns = nextOccupiedColumns;
      });
      return result;
    },
    getTextArray(td: HTMLElement) {
      const result: string[] = [];
      const elements = Array.from(
        td.querySelectorAll<HTMLElement>("span, div")
      );
      elements.forEach(element => {
        const hasTextChild = Array.from(element.children).some(child => {
          return (child.textContent || "").trim().length > 0;
        });
        if (hasTextChild) {
          return;
        }
        const text = (element.textContent || "").trim();
        if (text) {
          result.push(text);
        }
      });
      if (result.length === 0) {
        const text = (td.textContent || "").trim();
        if (text) {
          result.push(text);
        }
      }
      return Array.from(new Set(result));
    },
    getQuantityUnit(value: string) {
      const text = String(value || "").trim();
      const match = text.match(/^([+-]?(?:\d+(?:\.\d+)?|\.\d+))\s*(.*)$/);
      if (!match) {
        return "";
      }
      return (match[2] || "").trim();
    },
    getTableArray(
      table: HTMLElement,
      splitIndexs: number[]
    ) {
      const splitIndexSet = new Set(splitIndexs);

      /*
       * 每个拆分字段有自己独立的单位列表。
       *
       * key：字段在centerColumns中的逻辑下标
       * value：该字段出现过的单位
       */
      const splitUnits = new Map<number, string[]>();

      splitIndexs.forEach(index => {
        splitUnits.set(index, []);
      });

      const rows = this.getLogicalTableRows(table);

      rows.forEach(rowCells => {
        rowCells.forEach(({ td, index }) => {
          if (!splitIndexSet.has(index)) {
            return;
          }
          const currentUnits = splitUnits.get(index) || [];

          const texts = this.getTextArray(td);
          texts.forEach(text => {
            const unit = this.getQuantityUnit(text);
            if (!unit) {
              return;
            }
            if (!currentUnits.includes(unit)) {
              currentUnits.push(unit);
            }
          });

          splitUnits.set(index, currentUnits);
        });
      });

      const splitMax = new Map<number, number>();

      splitIndexs.forEach(index => {
        const units = splitUnits.get(index) || [];

        splitMax.set(
          index,
          Math.max(units.length, 1)
        );
      });

      return {
        splitMax,
        splitUnits
      };
    },
    setTdNotNull(table) {
      if (table == null) {
        return table;
      }
      var tmp = table.cloneNode(true) as HTMLElement;
      var tdList = Array.from(tmp.querySelectorAll('td')) as any[];

      tdList.filter((item) => item.innerText == '').forEach((item) => {
        item.innerText = '/';
      })
      return tmp;
    },
    getCustomLength(str) {
      let length = 0;

      // 正则表达式用于匹配英文字符和中文字符
      // eslint-disable-next-line no-control-regex
      const englishPattern = /^[\u0000-\u007F]$/;
      const chinesePattern = /^[\u4E00-\u9FFF]$/;

      for (let i = 0; i < str.length; i++) {
        const char = str[i];

        // 判断字符是英文还是中文，并相应地增加长度
        if (englishPattern.test(char)) {
          length += 1;
        } else if (chinesePattern.test(char)) {
          length += 2;
        } else {
          // 其他字符的处理逻辑，例如特殊符号
          length += 1; // 或根据需要进行调整
        }
      }

      return length + 2;
    },
    //处理为空值的情况
    handleSheetBlank(sheetL) {
      const range = this.$xlsx.utils.decode_range(sheetL['!ref']);
      for (let R = range.s.r; R <= range.e.r; ++R) {
        for (let C = range.s.c; C <= range.e.c; ++C) {
          const cell_address = this.$xlsx.utils.encode_cell({ r: R, c: C });
          if (sheetL[cell_address].v == '') {
            sheetL[cell_address] = { t: 's', v: '-' }; // 设置默认值为空字符串
          }
        }
      }
    },
    //处理多级表头
    handeleHeader(headers) {
      // 判断几级表头
      var headerLevels = headers.reduce((prev, next) => {
        return prev > next.length ? prev : next.length
      }, 1)
      if (headerLevels == 1) {
        var tmp = headers.flat(2); //展开 深度为2
        return [tmp];
      }
      var result: any[] = [];
      // 每个表头处理为相同的层级
      for (var i = 0; i < headerLevels; i++) {
        var newHeader = headers.map((item) => {
          var headerItemQuantity = item[0].length;
          if (i < item.length) {
            return item[i]
          } else {
            return new Array(headerItemQuantity)
          }
        })
        //表头连接
        var concatHeader = newHeader.reduce((prev, next) => {
          return prev.concat(next)
        }, [])

        result.push(concatHeader)
      }
      return result;
    },

    //复制单元格
    copyCell() {
      var tds = document.querySelectorAll('.datagrid-cell-text-ellipsis');
      var icon = document.getElementById('copy_icon') as any;
      var table = document.querySelector('.scroll-body') as any;
      if (table == null) {
        var list = document.querySelectorAll('.datagrid-body') as any;
        if (list.length > 1) {
          table = list[1]
        }
        if (list.length == 1) {
          table = list[0]
        }
      }

      if (icon != null) {
        tds.forEach((x: any) => {
          const rect = x.getBoundingClientRect();
          const popupRect = icon.getBoundingClientRect();
          x.addEventListener('mouseenter', () => {
            icon.style.display = 'block';
            var left = rect.left - (table && table.contains(x) ? table.scrollLeft : 0) + rect.width / 2;
            var top = rect.top - (table && table.contains(x) ? table.scrollTop : 0) - popupRect.top - rect.height;
            icon.style.left = left + 'px';
            icon.style.top = top + 'px';
            icon.style.visibility = 'visible';
          });
          x.addEventListener('click', () => { navigator.clipboard.writeText(x.innerText); this.$kingzon_messager.showInfo("复制成功") });
          x.addEventListener('mouseleave', () => {
            icon.style.display = 'none';
            icon.style.left = '0px';
            icon.style.top = '0px';
          });
        })
      }
    },

    //显示字段过滤内容
    getFieldFilterContent() {
      var that = this;
      return {
        component: markRaw(FieldFilter),
        propsData: {
          dataGrid: this.$refs.dataGrid,
          onColumnChange: () => {
            that.saveColumnOrder();
          }
        },
        position: 'bottom',
        tooltipStyle: {
          'max-width': '500px'
        }
      }
    }
  }
})
</script>

<style scoped>
:deep(.datagrid-body td) {
  vertical-align: top;
  padding-top: 5px;
  position: relative;
}

:deep(.datagrid-body td>div) {
  position: sticky;
  top: 5px;
}

:deep(.datagrid-body td:has(> div.datagrid-editable)) {
  padding-top: 0;
}

/* :deep(.datagrid-cell-text-ellipsis:hover) {
  background-color: var(--color_blue_borderTitle);
  cursor: pointer;
} */
</style>
