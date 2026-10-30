<template>
  <Dialog ref="dialog" title="复制工艺版本" :modal="true" :closed="true" :closable="!saving"
    :dialogStyle="{ width: '680px', maxWidth: '95vw', height: '510px', maxHeight: '85vh' }" @close="onClose">
    <div class="kingzon-page-content" style="padding: 8px; box-sizing: border-box; min-height: 0">
      <div style="padding-bottom: 6px">复制到：{{ targetOrder.code }} {{ targetOrder.name }}</div>
      <SearchPanel v-if="opened" :searchParams="searchParams" @search="search" />
      <KingZonDataGrid v-if="opened" ref="dataGrid" :data="orders" class="kingzon-page-fill" idField="id"
        :showFieldFilter="false" :showDownLoad="false" selectionMode="single" :selection="sourceOrder"
        emptyMsg="没有符合条件的工艺单" @pageChange="getOrders" @sortChange="getOrders" @selectionChange="selectOrder">
        <GridColumnGroup>
          <GridHeaderRow>
            <GridColumn field="code" title="工艺单编码" width="120" />
            <GridColumn field="name" title="工艺单名称" width="200" :overflowTextEllipsis="true" />
            <GridColumn field="productNo" title="产品编码" width="120" />
            <GridColumn field="productName" title="产品名称" width="180" :overflowTextEllipsis="true" />
          </GridHeaderRow>
        </GridColumnGroup>
      </KingZonDataGrid>
      <div style="padding: 8px 0; line-height: 26px">
        <div>来源工艺单：{{ sourceOrder ? `${sourceOrder.code} ${sourceOrder.name}` : '请在上方选择' }}</div>
        <div style="display: flex; align-items: center; gap: 8px">
          <span>来源版本：</span>
          <ComboBox v-model="form.sourceVersionId" :data="versionOptions" :editable="false"
            :disabled="saving || versionLoading || !versions.length" style="width: 300px" />
          <span v-if="versionLoading" class="kingzon-text-grey">加载中…</span>
        </div>
        <div class="kingzon-text-grey">复制来源版本已保存的工序、物料及设备配置，新版本为暂存状态。</div>
        <div v-if="error" style="color: var(--color-red)">{{ error }}</div>
      </div>
      <div class="kingzon-btn-group kingzon-page-footer">
        <LinkButton :disabled="saving" @click="close">取消</LinkButton>
        <LinkButton :disabled="saving || versionLoading || !form.sourceVersionId" @click="confirm">
          {{ saving ? '复制中…' : '确认复制' }}
        </LinkButton>
      </div>
    </div>
  </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { CraftOrderDto, CraftListOrderDto, CraftVersionDto, CraftOrderListFilterDTO, CopyCraftVersionDto,
  type DialogRef, type SearchGroup } from '@/types/craft'
import { CraftOrderVersionStatus, DataType, FilterType } from '@/types/enums'
import { Search } from '@/types/types'

export default defineComponent({
  name: 'CopyCraftVersion',
  emits: ['copied'],
  data() {
    return {
      opened: false,
      saving: false,
      orderRequest: 0,
      versionRequest: 0,
      versionLoading: false,
      error: '',
      targetOrder: new CraftOrderDto(),
      sourceOrder: null as CraftListOrderDto | null,
      form: new CopyCraftVersionDto(),
      orders: [] as CraftListOrderDto[],
      versions: [] as CraftVersionDto[],
      filter: new CraftOrderListFilterDTO(),
      searchParams: [
        new Search('code', '工艺单编码', 'craftOrder', FilterType.Contains, DataType.Text),
        new Search('name', '工艺单名称', 'craftOrder', FilterType.Contains, DataType.Text)
      ]
    }
  },
  computed: {
    versionOptions(): Array<{ value: number; text: string }> {
      return this.versions.filter(version => !!version.id).map(version => ({
        value: version.id!,
        text: `${version.version}${version.isMain ? '（主版本）' : ''} · ${CraftOrderVersionStatus[version.status]}`
      }))
    }
  },
  beforeUnmount() { this.onClose() },
  methods: {
    async open(order: CraftOrderDto, versionId?: number) {
      if (this.saving || !order.id) return
      this.onClose()
      this.targetOrder = new CraftOrderDto(order)
      this.form = new CopyCraftVersionDto({ craftOrderId: order.id })
      const form = this.form
      this.filter = new CraftOrderListFilterDTO()
      this.orders = []
      this.error = ''
      this.opened = true
      const dialog = this.$refs.dialog as DialogRef | undefined
      dialog?.open()
      await this.$nextTick()
      if (!this.opened || this.form !== form) return
      await Promise.all([
        this.getOrders(),
        this.selectOrder(new CraftListOrderDto({ id: order.id, code: order.code, name: order.name }), versionId)
      ])
    },
    close() {
      if (this.saving) return
      const dialog = this.$refs.dialog as DialogRef | undefined
      dialog?.close()
      this.onClose()
    },
    onClose() {
      this.opened = false
      this.orderRequest++
      this.versionRequest++
      this.versionLoading = false
      this.sourceOrder = null
      this.versions = []
    },
    search(params: SearchGroup[]) {
      if (this.saving) return
      this.filter.craftOrderFilterList = params.find(item => item.table === 'craftOrder')?.filter ?? []
      const grid = this.$refs.dataGrid as { pageIndex: number } | undefined
      if (grid) grid.pageIndex = 1
      this.getOrders()
    },
    async getOrders() {
      if (!this.opened || this.saving) return
      const token = ++this.orderRequest
      try {
        const result = await this.$http.post('/api/technical/CraftOrderManage/GetCraftOrderList', this.filter, this.$refs.dataGrid)
        if (token === this.orderRequest && this.opened) this.orders = (result.data ?? []) as CraftListOrderDto[]
      } catch (error) {
        if (token === this.orderRequest && this.opened) this.$kingzon_messager.showError(error)
      }
    },
    async selectOrder(order: CraftListOrderDto | null, versionId?: number) {
      if (this.saving || !this.opened || !order?.id) return
      if (order.id === this.sourceOrder?.id && (this.versionLoading || this.versions.length)) return
      const token = ++this.versionRequest
      this.sourceOrder = order
      this.form.sourceCraftOrderId = order.id
      this.form.sourceVersionId = 0
      this.versions = []
      this.error = ''
      this.versionLoading = true
      try {
        const result = await this.$http.get('/api/technical/CraftOrderManage/GetCraftVersionList', { craftOrderId: order.id })
        if (token !== this.versionRequest || !this.opened) return
        this.versions = ((result.data ?? []) as CraftVersionDto[]).filter(version => version.craftOrderId === order.id)
        const selected = this.versions.find(version => version.id === versionId)
          ?? this.versions.find(version => version.isMain) ?? this.versions[0]
        this.form.sourceVersionId = selected?.id ?? 0
        if (!selected) this.error = '该工艺单暂无可复制版本'
      } catch (error) {
        if (token === this.versionRequest && this.opened) {
          this.error = '版本加载失败，请重新选择工艺单'
          this.$kingzon_messager.showError(error)
        }
      } finally {
        if (token === this.versionRequest) this.versionLoading = false
      }
    },
    async confirm() {
      if (!this.opened || this.saving || this.versionLoading) return
      if (!this.versions.some(version => version.id === this.form.sourceVersionId
        && version.craftOrderId === this.form.sourceCraftOrderId)) {
        this.error = '请选择来源工艺单及版本'
        return
      }
      const input = new CopyCraftVersionDto(this.form)
      this.saving = true
      this.error = ''
      try {
        const result = await this.$http.post('/api/technical/CraftOrderManage/CopyCraftVersion', input)
        if (!this.opened) return
        const version = result.data as CraftVersionDto
        if (!version?.id) throw new Error('复制结果未返回版本编号，请刷新版本列表核对')
        this.$emit('copied', { craftOrderId: input.craftOrderId, versionId: version.id })
        const dialog = this.$refs.dialog as DialogRef | undefined
        dialog?.close()
        this.onClose()
      } catch (error) {
        if (this.opened) this.$kingzon_messager.showError(error)
      } finally {
        this.saving = false
      }
    }
  }
})
</script>
