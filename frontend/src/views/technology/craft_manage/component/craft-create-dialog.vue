<template>
    <Dialog class="craft-dialog" ref="dialog" title="新增工艺单" :modal="true" :closed="true" :closable="!saving"
        :dialogStyle="{ width: '560px', maxWidth: '95vw' }">
        <div class="create-body">
            <Form :model="form" :labelWidth="100" labelAlign="right">
                <FormField name="productId" label="产品：">
                    <div class="product-picker">
                        <span :title="productText">{{ productText || '请选择产品或半成品' }}</span>
                        <LinkButton iconCls="icon-search" :disabled="saving || productLocked" @click="selectProduct">选择</LinkButton>
                    </div>
                </FormField>
                <FormField name="name" label="工艺单名称：">
                    <TextBox v-model="form.name" :disabled="saving" placeholder="请输入工艺单名称" style="width: 100%" />
                </FormField>
            </Form>
            <div class="dialog-actions">
                <LinkButton :disabled="saving" @click="closeDialog">取消</LinkButton>
                <LinkButton iconCls="icon-save" class="craft-primary" :disabled="saving" @click="save">{{ saving ? '保存中…' : '保存并编辑' }}</LinkButton>
            </div>
        </div>
    </Dialog>
    <Dialog class="craft-dialog" ref="productDialog" title="选择产品或半成品" :modal="true" :closed="true"
        :dialogStyle="{ width: '720px', height: '500px', maxWidth: '95vw' }">
        <SearchPanel :searchParams="searchParams" @search="searchProducts" />
        <KingZonDataGrid ref="productGrid" :data="products" style="height: 380px" selectionMode="single"
            @pageChange="loadProducts" @sortChange="loadProducts" @rowDblClick="applyProduct">
            <GridColumn field="productNo" title="产品编码" width="180" />
            <GridColumn field="productName" title="产品名称" width="260" />
            <GridColumn title="操作" width="80">
                <template #body="{ row }"><LinkButton @click="applyProduct(row)">选择</LinkButton></template>
            </GridColumn>
        </KingZonDataGrid>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { DataType, FilterType } from '@/types/enums'
import { InventoryDataType } from '@/types/common'
import { Search } from '@/types/types'
import { CraftOrderDto, type DialogRef, type SearchGroup, type ProductOption } from '@/types/craft'
import type { Filter } from '@kingzon/base-pc'

export default defineComponent({
    name: 'CraftCreateDialog',
    emits: ['created'],
    data() {
        return {
            saving: false,
            productLocked: false,
            form: new CraftOrderDto(),
            products: [] as ProductOption[],
            filters: [] as Filter[],
            productRequest: 0,
            searchParams: [
                new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text),
                new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text)
            ]
        }
    },
    computed: {
        productText(): string {
            return [this.form.productNo, this.form.productName].filter(Boolean).join(' / ')
        }
    },
    methods: {
        closeDialog() { (this.$refs.dialog as DialogRef | undefined)?.close() },
        open(product: ProductOption | null = null) {
            if (this.saving) return
            this.form = new CraftOrderDto()
            this.productLocked = false
            if (product) {
                this.applyProduct(product)
                this.productLocked = !!this.form.productId
            }
            ;(this.$refs.dialog as DialogRef | undefined)?.open()
        },
        selectProduct() {
            this.filters = []
            ;(this.$refs.productDialog as DialogRef | undefined)?.open()
            this.$nextTick(() => this.loadProducts())
        },
        searchProducts(params: SearchGroup[]) {
            this.filters = params.find(item => item.table === 'product')?.filter ?? []
            this.loadProducts()
        },
        async loadProducts() {
            const request = ++this.productRequest
            try {
                const result = await this.$http.post('/api/technical/CraftOrderManage/GetProductList', this.filters, this.$refs.productGrid)
                if (request === this.productRequest) this.products = result?.data ?? []
            } catch (error) {
                if (request === this.productRequest) this.$kingzon_messager.showError(error)
            }
        },
        applyProduct(product: ProductOption | null) {
            if (!product || this.saving) return
            const isSpecification = product.dataType === InventoryDataType.Specification
            const productId = Number(product.productId ?? product.materialInfo?.dataProductId
                ?? (isSpecification ? 0 : product.dataId ?? product.id))
            if (!productId) return
            const oldDefault = `${this.form.productName}工艺单`
            const productName = product.productName ?? product.materialName ?? product.name ?? ''
            const name = !this.form.name || this.form.name === oldDefault ? `${productName}工艺单` : this.form.name
            this.form = new CraftOrderDto({
                productId, name, productName,
                productNo: product.productNo ?? product.materialNo ?? product.code ?? '',
                specificationId: isSpecification ? product.dataId : product.specificationId
            })
            ;(this.$refs.productDialog as DialogRef | undefined)?.close()
        },
        async save() {
            if (this.saving) return
            const name = this.form.name.trim()
            if (!this.form.productId || !name || name.length > 100) {
                this.$kingzon_messager.showInfo('请选择产品，并填写不超过100个字符的工艺单名称')
                return
            }
            this.saving = true
            try {
                const result = await this.$http.post('/api/technical/CraftOrderManage/CreateCraftOrder', {
                    productId: this.form.productId, specificationId: this.form.specificationId, name
                })
                const order = result?.data
                if (!order?.id) throw new Error('创建结果缺少工艺单编号，请刷新列表确认')
                // 主单已经保存成功，即使初始化版本失败，也打开该单，避免重试时重复创建。
                try {
                    await this.$http.post('/api/technical/CraftOrderManage/CreateCraftVersion', { craftOrderId: order.id })
                } catch (error) {
                    this.$kingzon_messager.showInfo('工艺单已创建，初始版本创建失败，请在编辑界面点击“新增版本”')
                }
                this.closeDialog()
                this.$emit('created', order)
            } catch (error) {
                this.$kingzon_messager.showError(error)
            } finally {
                this.saving = false
            }
        }
    }
})
</script>

<style scoped>
.create-body { padding: 16px; }
.product-picker { display: flex; align-items: center; gap: 8px; min-width: 0; }
.product-picker > span { flex: 1; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; }
.dialog-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
</style>
