<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page">
    <div class="kingzon-page-title">
      <span>工艺单管理</span>
    </div>
    <div class="kingzon-page-body">
      <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search">
      </SearchPanel>
      <div class="kingzon-page-content">
        <div class="kingzon-btn-group" style="margin: 5px">
          <LinkButton iconCls="icon-add" @click="create">新增</LinkButton>
        </div>
        <KingZonDataGrid ref="dataGrid" class="kingzon-page-fill" :data="craftOrderList" :showDownLoad="true"
          id="craftOrderDataGrid" exportFileName="工序单列表" emptyMsg="没有符合条件的数据" selectionMode="null" @pageChange="getData"
          @sortChange="getData">
          <GridColumnGroup align="left" :frozen="true" width="595">
            <GridHeaderRow>
              <GridColumn field="index" align="center" width="45">
                <template #body="scope">
                  {{ scope.rowIndex + 1 }}
                </template>
              </GridColumn>
              <GridColumn field="code" title="工艺单编码" align="left" width="120" :sortable="true" />
              <GridColumn field="name" title="工艺单名称" align="left" width="150" />
              <GridColumn field="productNo" title="产品编码" align="left" width="120" :sortable="true" />
              <GridColumn field="productName" title="产品名称" align="left" width="160" />
            </GridHeaderRow>
          </GridColumnGroup>
          <GridColumnGroup align="center" :columnMoving="true">
            <GridHeaderRow>
              <GridColumn field="mainVersion" title="主版本" align="left" width="80" />
              <GridColumn field="versionCount" title="版本数量" align="right" width="70" :sortable="true" />
              <GridColumn field="description" title="备注" align="left" width="200" :overflowTextEllipsis="true" />
              <GridColumn field="createUserName" title="创建人" align="left" width="70" />
              <GridColumn field="createTime" title="创建时间" align="left" width="150" :sortable="true">
                <template #body="scope">
                  {{ $utils.formatDateTime(scope.row.createTime, 'yyyy-MM-dd HH:mm:ss') }}
                </template>
              </GridColumn>
            </GridHeaderRow>
          </GridColumnGroup>
          <GridColumnGroup :frozen="true" align="right" width="140">
            <GridHeaderRow>
              <GridColumn field="options" title="操作" align="center" width="140">
                <template #body="scope">
                  <ButtonGroup style="height:24px">
                    <LinkButton @click="editCraftOrder(scope.row)">版本管理
                    </LinkButton>
                    <LinkButton @click="deleteCraftOrder(scope.row)">
                      删除</LinkButton>
                  </ButtonGroup>
                </template>
              </GridColumn>
            </GridHeaderRow>
          </GridColumnGroup>
        </KingZonDataGrid>
      </div>
    </div>
  </div>
  <Drawer v-for="(entry, index) in drawerStack" :key="entry.id" ref="craftDrawer"
    :drawerStyle="{ width: '98%', zIndex: 9000 + index }" region="east" :closable="true" :maskClosable="false"
    @collapse="onDrawerCollapse(index)">
    <CreateCraft :craftOrderId="entry.craftOrderId" @submit="submitDrawer"
      @createCraftVersion="handleCreateCraftVersion" @cancel="onCancel(index)" />
  </Drawer>
  <CraftCreateDialog ref="createDialog" @created="onCraftCreated" />
</template>

<script lang="ts">
import { type CreateDialogRef, type ProductOption, type DrawerEntry, type DrawerRef, type SearchGroup, type CraftOrderDto, CraftOrderListFilterDTO, CraftListOrderDto } from '@/types/craft'
import { DataType, FilterType } from '@/types/enums'
import { Search } from '@/types/types'
import { defineComponent } from 'vue';
import CreateCraft from './create-craft.vue';
import CraftCreateDialog from './component/craft-create-dialog.vue';

export default defineComponent({
  name: "CraftOrderManage",
  components: { CreateCraft, CraftCreateDialog },
  data() {
    return {
      isLoading: false,
      searchParams: [] as Search[],
      searchParamsHidden: [] as Search[],
      filterList: new CraftOrderListFilterDTO(),
      craftOrderList: [] as CraftListOrderDto[],
      drawerStack: [] as DrawerEntry[],
      drawerIdCounter: 0
    }
  },
  created() {
    this.addSearchParam()

  },
  mounted() {
    this.getData()
  },
  methods: {
    /** 组装查询面板字段 */
    addSearchParam(): void {
      this.searchParams.push(
        new Search('code', '工艺单编码', 'craftOrder', FilterType.Contains, DataType.Text),
        new Search('name', '工艺单名称', 'craftOrder', FilterType.Contains, DataType.Text),
      )
      this.searchParamsHidden.push(
        new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text),
        new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text),
      )
    },
    search(params: SearchGroup[]): void {
      this.filterList.craftOrderFilterList = params.find(item => item.table === 'craftOrder')?.filter ?? []
      this.filterList.productFilterList = params.find(item => item.table === 'product')?.filter ?? []
      this.getData()
    },
    getData(): void {
      this.isLoading = true
      this.$http.post('api/technical/CraftOrderManage/GetCraftOrderList', this.filterList, this.$refs.dataGrid)
        .then(res => {
          this.craftOrderList = (res.data ?? []) as CraftListOrderDto[]
        })
        .catch((err: unknown) => {
          this.$kingzon_messager.showError(err || '查询失败')
        })
        .finally(() => {
          this.isLoading = false
        })
    },
    getDrawerTitle(mode: string): string {
      if (mode === 'edit') return '版本管理'
      return '工艺单编辑'
    },
    pushDrawer(mode: string, craftOrderId: number) {
      this.drawerStack.push({
        id: ++this.drawerIdCounter,
        mode,
        craftOrderId
      })
      this.$nextTick(() => {
        const refs = (this.$refs.craftDrawer as DrawerRef | DrawerRef[] | undefined)
        if (refs) {
          const last = Array.isArray(refs) ? refs[refs.length - 1] : refs
          last?.expand()
        }
      })
    },
    onDrawerCollapse(index: number) {
      this.drawerStack.splice(index, 1)
    },
    create() {
      (this.$refs.createDialog as CreateDialogRef | undefined)?.open()
    },
    onCraftCreated(order: CraftOrderDto) {
      this.getData()
      if (order.id) this.pushDrawer('edit', order.id)
    },
    async deleteCraftOrder(row: CraftListOrderDto) {
      if (!row?.id) return
      this.$kingzon_messager.showConfim(`确定要删除工序单"${row.name || row.code}",删除后不可恢复`)
        .then(async () => {
          this.isLoading = true
          await this.$http.post('/api/technical/CraftOrderManage/DeleteCraftOrder', { id: row.id })
          this.$kingzon_messager.showInfo('删除成功')
          this.getData()
        })
        .catch((err: unknown) => {
          this.$kingzon_messager.showError(err || '删除失败')
          this.isLoading = false
        })
    },
    editCraftOrder(row: CraftListOrderDto) {
      if (!row?.id) return
      this.pushDrawer('edit', row.id)
    },
    async handleCreateCraftVersion(product: ProductOption) {
      try {
        const result = await this.$http.get('/api/technical/CraftOrderManage/GetCraftOrderByProductId',
          { dataType: product.dataType, dataId: product.dataId }
        )
        const existingOrder = result?.data
        if (existingOrder?.id) {
          this.pushDrawer('edit', existingOrder.id)
        } else {
          ; (this.$refs.createDialog as CreateDialogRef | undefined)?.open(product)
        }
      } catch (error) {
        this.$kingzon_messager.showError(error)
      }
    },
    onCancel(index: number) {
      const refs = this.$refs.craftDrawer as DrawerRef[] | DrawerRef | undefined

      if (!refs) return

      const drawer = Array.isArray(refs) ? refs[index] : refs

      drawer?.collapse()
    },
    submitDrawer() {
      this.getData()
    }
  }
})
</script>
