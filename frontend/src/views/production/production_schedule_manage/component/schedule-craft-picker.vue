<template>
    <Dialog ref="dialog" title="选择工序单及版本" :modal="true" :closed="true"
        :dialogStyle="{ width: '760px', maxWidth: '95vw', height: '560px', maxHeight: '90vh' }" @close="onClose">
        <div class="kingzon-page-content" style="padding: 8px; box-sizing: border-box; min-height: 0">
            <SearchPanel v-if="opened" :searchParams="searchParams" @search="search" />
            <KingZonDataGrid v-if="opened" ref="dataGrid" :data="orders" class="kingzon-page-fill" idField="id"
                :showFieldFilter="false" :showDownLoad="false" selectionMode="single" :selection="selectedOrder"
                @pageChange="getOrders" @sortChange="getOrders" @selectionChange="selectOrder">
                <GridColumnGroup><GridHeaderRow>
                    <GridColumn field="code" title="工序单编码" width="130" />
                    <GridColumn field="name" title="工序单名称" width="180" :overflowTextEllipsis="true" />
                    <GridColumn field="productNo" title="产品编码" width="130" />
                    <GridColumn field="productName" title="产品名称" width="180" :overflowTextEllipsis="true" />
                </GridHeaderRow></GridColumnGroup>
            </KingZonDataGrid>
            <div style="display: flex; align-items: center; gap: 8px; padding: 8px 0">
                <span>工艺版本：</span>
                <ComboBox v-model="versionId" :data="versionOptions" :editable="false"
                    :disabled="versionLoading || !versions.length" style="width: 300px" />
                <span v-if="versionLoading" class="kingzon-text-grey">加载中…</span>
            </div>
            <p v-if="error" style="color: var(--color-red)">{{ error }}</p>
            <div class="kingzon-btn-group kingzon-page-footer">
                <LinkButton @click="close">取消</LinkButton>
                <LinkButton :disabled="versionLoading || !versionId" @click="confirm">确定</LinkButton>
            </div>
        </div>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { CraftListOrderDto, CraftVersionDto, CraftOrderListFilterDTO, type DialogRef, type SearchGroup } from '@/types/craft'
import { CraftOrderVersionStatus, DataType, FilterType } from '@/types/enums'
import { Search } from '@/types/types'

export default defineComponent({
    name: 'ScheduleCraftPicker',
    emits: ['select'],
    data() {
        return {
            opened: false, orderRequest: 0, versionRequest: 0, versionLoading: false, error: '', versionId: 0,
            orders: [] as CraftListOrderDto[], selectedOrder: null as CraftListOrderDto | null,
            versions: [] as CraftVersionDto[], filter: new CraftOrderListFilterDTO(),
            searchParams: [new Search('code', '工序单编码', 'craftOrder', FilterType.Contains, DataType.Text),
                new Search('name', '工序单名称', 'craftOrder', FilterType.Contains, DataType.Text)]
        }
    },
    computed: {
        versionOptions(): Array<{ value: number; text: string }> {
            return this.versions.map(version => ({ value: version.id!, text: `${version.version}${version.isMain ? '（主版本）' : ''}` }))
        }
    },
    beforeUnmount() { this.onClose() },
    methods: {
        async open() {
            this.onClose()
            this.opened = true
            this.orders = []
            this.error = ''
            this.filter = new CraftOrderListFilterDTO()
            ;(this.$refs.dialog as DialogRef).open()
            await this.$nextTick()
            if (this.opened) await this.getOrders()
        },
        close() { (this.$refs.dialog as DialogRef).close(); this.onClose() },
        onClose() {
            this.opened = false
            this.orderRequest++
            this.versionRequest++
            this.versionLoading = false
            this.selectedOrder = null
            this.versions = []
            this.versionId = 0
        },
        search(groups: SearchGroup[]) {
            this.filter.craftOrderFilterList = groups.find(group => group.table === 'craftOrder')?.filter ?? []
            const grid = this.$refs.dataGrid as { pageIndex: number } | undefined
            if (grid) grid.pageIndex = 1
            this.getOrders()
        },
        async getOrders() {
            if (!this.opened) return
            const token = ++this.orderRequest
            this.selectOrder(null)
            try {
                const response = await this.$http.post('/api/technical/CraftOrderManage/GetCraftOrderList', this.filter, this.$refs.dataGrid)
                if (this.opened && token === this.orderRequest) this.orders = response.data ?? []
            } catch (error) {
                if (this.opened && token === this.orderRequest) this.$kingzon_messager.showError(error)
            }
        },
        async selectOrder(order: CraftListOrderDto | null) {
            const token = ++this.versionRequest
            this.selectedOrder = order
            this.versions = []
            this.versionId = 0
            this.error = ''
            this.versionLoading = false
            if (!this.opened || !order?.id) return
            this.versionLoading = true
            try {
                const response = await this.$http.get('/api/technical/CraftOrderManage/GetCraftVersionList', { craftOrderId: order.id })
                if (!this.opened || token !== this.versionRequest) return
                this.versions = (response.data as CraftVersionDto[]).filter(version => version.id && version.craftOrderId === order.id && version.status === CraftOrderVersionStatus.已发布)
                this.versionId = (this.versions.find(version => version.isMain) ?? this.versions[0])?.id ?? 0
                if (!this.versionId) this.error = '该工序单暂无已发布版本'
            } catch (error) {
                if (this.opened && token === this.versionRequest) this.$kingzon_messager.showError(error)
            } finally {
                if (token === this.versionRequest) this.versionLoading = false
            }
        },
        confirm() {
            if (!this.opened || this.versionLoading || !this.versions.some(version => version.id === this.versionId)) return
            this.$emit('select', this.versionId)
            this.close()
        }
    }
})
</script>
