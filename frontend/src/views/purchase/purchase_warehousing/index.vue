<template>
  <Loading v-if="isLoading"></Loading>
  <div class="kingzon-page">
    <div class="kingzon-page-title">
      <span>采购到货入库</span>
    </div>
    <div class="kingzon-page-body">
      <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search">
      </SearchPanel>
      <Tabs :plain="true" style="height:100%" @tabSelect="tabSelect">
        <TabPanel v-for="(item, index) in tabList" :key="index" :title="item.text">
          <div class="kingzon-page-content">
            <linkButton v-if="selectedTab === ArrivalStatus.未完成" iconCls="icon-add" @click="openBatchInboundDrawer"
              style="width: 90px; margin-left: 4px;margin-top:2px;margin-bottom:2px">批量入库</linkButton>
            <KingZonDataGrid v-if="selectedTab === ArrivalStatus.未完成" :showDownLoad="true" :exportFileName="'采购入库记录'"
              class="kingzon-page-fill" :id="'data' + index" :ref="'dataGrid' + item.value"
              :data="arrivalRecordList[item.value as number]" @pageChange="refreshData" @sortChange="refreshData"
              emptyMsg="没有符合条件的数据" :columnResizing="true"
              :splitFields="['receivableQuantity', 'inventoryQuantity', 'returnQuantity', 'concessionQuantity', 'qualifiedQuantity', 'arrivalQuantity']"
              :columnMoving="true" :sorts="[{ field: 'arrivalCreateTime', order: 'desc' }]" :isCheckBoxSelection="true"
              idField="arrivalRecordId" :leftGroupRowWidth="680" :rightGroupRowWidth="120"
              @seletedRowsChanged="seletedRowsChanged" :isDownLoad="true" :downloadLeft="50">
              <template #left>
                <GridColumn cellCss="datagrid-td-rownumber" field="index" title="" align="center" width="40">
                  <template #body="scope">
                    {{ scope.rowIndex + 1 }}
                  </template>
                </GridColumn>
                <GridColumn field="arrivalRecordNo" title="到货编号" align="left" :sortable="true" width="120">
                  <template #body="scope">
                    <KingZonSymbol :rowText="scope.row.arrivalRecordNo" :symbolText="'让'" :color="'var(--color-yellow)'"
                      :showSymbol="scope.row.concessionQuantity > 0">
                    </KingZonSymbol>
                  </template>
                </GridColumn>
                <GridColumn field="purchaseOrderNo" title="采购单号" align="left" :sortable="true" width="150">
                  <!-- <template #body="scope">
                      <span>{{ scope.row.purchaseOrderNo }}</span>
                      <font-awesome-icon :icon="['fas', 'print']" style="color: #74C0FC;margin-left: 5px;"
                        class="kingzon-pointer" title="入库单打印" @click="printOrder(scope.row)"
                        v-if="scope.row.inventoryQuantity" />
                      <font-awesome-icon :icon="['fas', 'tags']" style="color: #74C0FC;margin-left: 5px;"
                        class="kingzon-pointer" title="标签打印" @click="printOrderTag(scope.row)"
                        v-if="scope.row.inventoryQuantity" />
                    </template> -->
                </GridColumn>
                <GridColumn field="productNo" title="物料编码" align="left" width="110" :sortable="true"
                  :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productName" title="物料名称" align="left" width="120" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productTechnicalCode" title="技术型号" align="left" width="100" :sortable="true"
                  :overflowTextEllipsis="true">
                </GridColumn>
              </template>
              <template #center>
                <GridColumn field="productType" title="类型" width="70">
                  <template #body="scope">
                    {{ ProductType[scope.row.productType] }}
                  </template>
                </GridColumn>
                <GridColumn field="productCategory" align="left" width="120" title="物料类别" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productSpecification" align="left" width="120" title="规格型号"
                  :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="materialGrade" align="left" width="120" title="材质牌号" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productPackageWay" title="包装方式" align="left" width="120"
                  :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productRemark" title="物料备注" align="left" width="150" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productCustomAttributeInfoList" title="物料附加信息" align="left" width="150">
                  <template #body="scope">
                    <div class="kingzon-textEllipsis" style="display:flex;flex-wrap:wrap;justify-content:space-between;"
                      :title="scope.row.productCustomAttributeInfoList.filter(s => s.paraName && s.value).map(item => item.paraName + ':' + (item.value ?? '')).join('\n')">
                      <div style="width:50%" class="kingzon-textEllipsis"
                        v-for="(item, index) in scope.row.productCustomAttributeInfoList.filter(s => s.paraName && s.value)"
                        :key="index">
                        {{ item.paraName }}:{{ item.value ?? "" }};
                      </div>
                    </div>
                  </template>
                </GridColumn>
                <GridColumn field="dataStatus" title="物料状态" width="90">
                  <template #body="scope">
                    <span>{{ scope.row.dataStatus == InventoryDataStatus.None ? "" :
                      InventoryDataStatus[scope.row.dataStatus] }}</span>
                  </template>
                </GridColumn>
                <GridColumn field="purchaseType" title="采购类型" width="80" align="center">
                  <template #body="scope">
                    <span :class="scope.row.purchaseType == PurchaseType.需求采购 ? 'kingzon-tag'
                      : scope.row.purchaseType == PurchaseType.直接采购 ? 'kingzon-tag kingzon-tag-success'
                        : 'kingzon-tag kingzon-tag-warning'">
                      {{ PurchaseType[scope.row.purchaseType] }}</span>
                  </template>
                </GridColumn>
                <GridColumn field="supplierName" title="供应商" width="150" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="positionName" title="到货库位" width="150" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="inspectionConclusion" title="检验结论" align="center" width="80" :sortable="true">
                  <template #body="scope">
                    <span :class="scope.row.inspectionConclusion == InspectionConclusion.待检验 ? 'kingzon-tag kingzon-tag-info' :
                      scope.row.inspectionConclusion == InspectionConclusion.合格入库 ? 'kingzon-tag kingzon-tag-success' :
                        scope.row.inspectionConclusion == InspectionConclusion.让步接收 ? 'kingzon-tag kingzon-tag-warning' :
                          'kingzon-tag kingzon-tag-danger'">
                      {{ InspectionConclusion[scope.row.inspectionConclusion] }}
                    </span>
                  </template>
                </GridColumn>
                <GridColumn field="arrivalQuantity" title="到货数" width="90" align="right" :sortable="true">
                  <template #body="scope">
                    <UnitQuantityShow :quantity="scope.row.arrivalQuantity" :unitList="scope.row.unitInfoList"
                      :isChange="false">
                    </UnitQuantityShow>
                  </template>
                </GridColumn>
                <GridColumn field="qualifiedQuantity" title="合格数" width="90" align="right" :sortable="true">
                  <template #body="scope">
                    <UnitQuantityShow :quantity="scope.row.qualifiedQuantity" :unitList="scope.row.unitInfoList">
                    </UnitQuantityShow>
                  </template>
                </GridColumn>
                <GridColumn field="concessionQuantity" title="让步接收数" width="90" align="right" :sortable="true"
                  v-if="isConcede">
                  <template #body="scope">
                    <UnitQuantityShow :quantity="scope.row.concessionQuantity" :unitList="scope.row.unitInfoList">
                    </UnitQuantityShow>
                  </template>
                </GridColumn>
                <GridColumn field="returnQuantity" title="退货数" width="90" align="right" :sortable="true">
                  <template #body="scope">
                    <UnitQuantityShow :quantity="scope.row.returnQuantity" :unitList="scope.row.unitInfoList">
                    </UnitQuantityShow>
                  </template>
                </GridColumn>
                <GridColumn field="inventoryQuantity" title="已入库数" width="90" align="right"
                  cellCss="kingzon-detail-text" :sortable="true">
                  <template #body="scope">
                    <UnitQuantityShow :quantity="scope.row.inventoryQuantity" :unitList="scope.row.unitInfoList"
                      @click="inventoryDetail(scope.row)">
                    </UnitQuantityShow>
                  </template>
                </GridColumn>
                <GridColumn field="receivableQuantity" title="待入库量" width="90" align="right" :sortable="true">
                  <template #body="scope">
                    <UnitQuantityShow :quantity="scope.row.receivableQuantity" :unitList="scope.row.unitInfoList">
                    </UnitQuantityShow>
                  </template>
                </GridColumn>
                <GridColumn field="purchasePrice" title="采购单价" width="90" align="right" :overflowTextEllipsis="true"
                  v-if="isShowPrice">
                  <template #body="scope">
                    {{ scope.row.purchasePrice != null ? $utils.formatThousandSeparator(scope.row.purchasePrice ??
                      0) :
                      '' }}
                  </template>
                </GridColumn>
                <GridColumn field="inventoryPrice" title="最新入库单价" width="90" align="right" v-if="isShowPrice"
                  :sortable="true" :overflowTextEllipsis="true">
                  <template #body="scope">
                    {{ scope.row.inventoryPrice != null ? $utils.formatThousandSeparator(scope.row.inventoryPrice ??
                      0) :
                      '' }}
                  </template>
                </GridColumn>
                <GridColumn field="totalPrice" title="已入库金额" width="100" align="right" v-if="isShowPrice"
                  :overflowTextEllipsis="true" :sortable="true">
                  <template #body="scope">
                    {{ $utils.formatThousandSeparator(scope.row.totalPrice ?? 0) }}
                  </template>
                </GridColumn>
                <GridColumn field="insepctionReason" title="异常描述" width="150" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="createUserName" title="检验人" width="60" :overflowTextEllipsis="true"></GridColumn>
                <GridColumn field="createTime" title="检验时间" align="left" width="150" :sortable="true">
                  <template #body="scope">
                    {{ scope.row.inspectionConclusion == InspectionConclusion.待检验 || !scope.row.createTime ? '' :
                      $utils.formatDateTime(scope.row.createTime, 'yyyy-MM-dd HH:mm:ss') }}
                  </template>
                </GridColumn>
                <GridColumn field="arrivalCreateUserName" title="创建人" width="70" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="arrivalTime" title="到货时间" align="left" width="110" :sortable="true">
                  <template #body="scope">
                    {{ $utils.formatDateTime(scope.row.arrivalTime, 'yyyy-MM-dd') }}
                  </template>
                </GridColumn>
                <GridColumn field="arrivalCreateTime" title="创建时间" align="left" width="150">
                  <template #body="scope">
                    {{ $utils.formatDateTime(scope.row.arrivalCreateTime, 'yyyy-MM-dd HH:mm:ss') }}
                  </template>
                </GridColumn>
                <GridColumn field="remark" title="到货备注" width="200" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="purchaseRemark" title="采购备注" width="200" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn v-for="(para, index) in additionalParam" :key="index" :field="para.systemPara.id.toString()"
                  :title="para.systemPara.dataValue" align="left" width="100">
                </GridColumn>
              </template>
              <template #right>
                <GridColumn field="act" title="操作" align="center" width="120">
                  <template #body="scope">
                    <ButtonGroup style="height: 24px">
                      <LinkButton @click="warehousing(scope.row, false)" v-if="!scope.row.isComplete &&
                        scope.row.receivableQuantity > 0"> 入库 </LinkButton>
                      <LinkButton v-if="!scope.row.isComplete &&
                        scope.row.receivableQuantity > 0 && item.value == ArrivalStatus.未完成"
                        @click="modifyArrivalState(scope.row, true)">确认完成</LinkButton>
                      <LinkButton
                        v-if="scope.row.isComplete && item.value == ArrivalStatus.已完成 && scope.row.receivableQuantity > 0"
                        @click="modifyArrivalState(scope.row, false)">取消完成</LinkButton>
                      <!-- <LinkButton @click="printDetail(scope.row)" v-if="scope.row.inventoryQuantity > 0">入库单打印
                        </LinkButton>
                        <LinkButton @click="printTag(scope.row)" v-if="scope.row.inventoryQuantity > 0">
                          标签打印
                        </LinkButton> -->
                    </ButtonGroup>
                  </template>
                </GridColumn>
              </template>
            </KingZonDataGrid>
            <KingZonDataGrid v-else :showDownLoad="true" :exportFileName="'采购入库记录'" class="kingzon-page-fill"
              :id="'data' + index" :ref="'dataGrid' + item.value" :data="arrivalRecordList[item.value as number]"
              @pageChange="refreshData" @sortChange="refreshData" emptyMsg="没有符合条件的数据" :columnResizing="true"
              :splitFields="['receivableQuantity', 'inventoryQuantity', 'returnQuantity', 'concessionQuantity', 'qualifiedQuantity', 'arrivalQuantity']"
              :columnMoving="true" :sorts="[{ field: 'arrivalCreateTime', order: 'desc' }]" :isDownLoad="true">
              <GridColumnGroup :frozen="true" align="left" width="460">
                <GridHeaderRow>
                  <GridColumn cellCss="datagrid-td-rownumber" field="index" title="" align="center" width="40">
                    <template #body="scope">
                      {{ scope.rowIndex + 1 }}
                    </template>
                  </GridColumn>
                  <GridColumn field="arrivalRecordNo" title="到货编号" align="left" :sortable="true" width="120">
                    <template #body="scope">
                      <KingZonSymbol :rowText="scope.row.arrivalRecordNo" :symbolText="'让'"
                        :color="'var(--color-yellow)'" :showSymbol="scope.row.concessionQuantity > 0">
                      </KingZonSymbol>
                    </template>
                  </GridColumn>
                  <GridColumn field="purchaseOrderNo" title="采购单号" align="left" :sortable="true" width="150">
                    <!-- <template #body="scope">
                      <span>{{ scope.row.purchaseOrderNo }}</span>
                      <font-awesome-icon :icon="['fas', 'print']" style="color: #74C0FC;margin-left: 5px;"
                        class="kingzon-pointer" title="入库单打印" @click="printOrder(scope.row)"
                        v-if="scope.row.inventoryQuantity" />
                      <font-awesome-icon :icon="['fas', 'tags']" style="color: #74C0FC;margin-left: 5px;"
                        class="kingzon-pointer" title="标签打印" @click="printOrderTag(scope.row)"
                        v-if="scope.row.inventoryQuantity" />
                    </template> -->
                  </GridColumn>
                  <GridColumn field="productNo" title="物料编码" align="left" width="110" :sortable="true"
                    :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="productName" title="物料名称" align="left" width="120" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="productTechnicalCode" title="技术型号" align="left" width="100" :sortable="true"
                    :overflowTextEllipsis="true">
                  </GridColumn>
                </GridHeaderRow>
              </GridColumnGroup>
              <GridColumnGroup>
                <GridHeaderRow>

                  <GridColumn field="productType" title="类型" width="70">
                    <template #body="scope">
                      {{ ProductType[scope.row.productType] }}
                    </template>
                  </GridColumn>
                  <GridColumn field="productCategory" align="left" width="120" title="物料类别"
                    :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="productSpecification" align="left" width="120" title="规格型号"
                    :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="materialGrade" align="left" width="120" title="材质牌号" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="productPackageWay" title="包装方式" align="left" width="120"
                    :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="productRemark" title="物料备注" align="left" width="150" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="productCustomAttributeInfoList" title="物料附加信息" align="left" width="150">
                    <template #body="scope">
                      <div class="kingzon-textEllipsis"
                        style="display:flex;flex-wrap:wrap;justify-content:space-between;"
                        :title="scope.row.productCustomAttributeInfoList.filter(s => s.paraName && s.value).map(item => item.paraName + ':' + (item.value ?? '')).join('\n')">
                        <div style="width:50%" class="kingzon-textEllipsis"
                          v-for="(item, index) in scope.row.productCustomAttributeInfoList.filter(s => s.paraName && s.value)"
                          :key="index">
                          {{ item.paraName }}:{{ item.value ?? "" }};
                        </div>
                      </div>
                    </template>
                  </GridColumn>
                  <GridColumn field="dataStatus" title="物料状态" width="90">
                    <template #body="scope">
                      <span>{{ scope.row.dataStatus == InventoryDataStatus.None ? "" :
                        InventoryDataStatus[scope.row.dataStatus] }}</span>
                    </template>
                  </GridColumn>
                  <GridColumn field="purchaseType" title="采购类型" width="80" align="center">
                    <template #body="scope">
                      <span :class="scope.row.purchaseType == PurchaseType.需求采购 ? 'kingzon-tag'
                        : scope.row.purchaseType == PurchaseType.直接采购 ? 'kingzon-tag kingzon-tag-success'
                          : 'kingzon-tag kingzon-tag-warning'">
                        {{ PurchaseType[scope.row.purchaseType] }}</span>
                    </template>
                  </GridColumn>
                  <GridColumn field="supplierName" title="供应商" width="150" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="positionName" title="到货库位" width="150" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="inspectionConclusion" title="检验结论" align="center" width="80" :sortable="true">
                    <template #body="scope">
                      <span :class="scope.row.inspectionConclusion == InspectionConclusion.待检验 ? 'kingzon-tag kingzon-tag-info' :
                        scope.row.inspectionConclusion == InspectionConclusion.合格入库 ? 'kingzon-tag kingzon-tag-success' :
                          scope.row.inspectionConclusion == InspectionConclusion.让步接收 ? 'kingzon-tag kingzon-tag-warning' :
                            'kingzon-tag kingzon-tag-danger'">
                        {{ InspectionConclusion[scope.row.inspectionConclusion] }}
                      </span>
                    </template>
                  </GridColumn>
                  <GridColumn field="arrivalQuantity" title="到货数" width="90" align="right" :sortable="true">
                    <template #body="scope">
                      <UnitQuantityShow :quantity="scope.row.arrivalQuantity" :unitList="scope.row.unitInfoList"
                        :isChange="false">
                      </UnitQuantityShow>
                    </template>
                  </GridColumn>
                  <GridColumn field="qualifiedQuantity" title="合格数" width="90" align="right" :sortable="true">
                    <template #body="scope">
                      <UnitQuantityShow :quantity="scope.row.qualifiedQuantity" :unitList="scope.row.unitInfoList">
                      </UnitQuantityShow>
                    </template>
                  </GridColumn>
                  <GridColumn field="concessionQuantity" title="让步接收数" width="90" align="right" :sortable="true"
                    v-if="isConcede">
                    <template #body="scope">
                      <UnitQuantityShow :quantity="scope.row.concessionQuantity" :unitList="scope.row.unitInfoList">
                      </UnitQuantityShow>
                    </template>
                  </GridColumn>
                  <GridColumn field="returnQuantity" title="退货数" width="90" align="right" :sortable="true">
                    <template #body="scope">
                      <UnitQuantityShow :quantity="scope.row.returnQuantity" :unitList="scope.row.unitInfoList">
                      </UnitQuantityShow>
                    </template>
                  </GridColumn>
                  <GridColumn field="inventoryQuantity" title="已入库数" width="90" align="right"
                    cellCss="kingzon-detail-text" :sortable="true">
                    <template #body="scope">
                      <UnitQuantityShow :quantity="scope.row.inventoryQuantity" :unitList="scope.row.unitInfoList"
                        @click="inventoryDetail(scope.row)">
                      </UnitQuantityShow>
                    </template>
                  </GridColumn>
                  <GridColumn field="receivableQuantity" title="待入库量" width="90" align="right" :sortable="true">
                    <template #body="scope">
                      <UnitQuantityShow :quantity="scope.row.receivableQuantity" :unitList="scope.row.unitInfoList">
                      </UnitQuantityShow>
                    </template>
                  </GridColumn>
                  <GridColumn field="purchasePrice" title="采购单价" width="90" align="right" :overflowTextEllipsis="true"
                    v-if="isShowPrice">
                    <template #body="scope">
                      {{ scope.row.purchasePrice != null ? $utils.formatThousandSeparator(scope.row.purchasePrice ??
                        0) :
                        '' }}
                    </template>
                  </GridColumn>
                  <GridColumn field="inventoryPrice" title="最新入库单价" width="90" align="right" v-if="isShowPrice"
                    :sortable="true" :overflowTextEllipsis="true">
                    <template #body="scope">
                      {{ scope.row.inventoryPrice != null ? $utils.formatThousandSeparator(scope.row.inventoryPrice ??
                        0) :
                        '' }}
                    </template>
                  </GridColumn>
                  <GridColumn field="totalPrice" title="已入库金额" width="100" align="right" v-if="isShowPrice"
                    :overflowTextEllipsis="true" :sortable="true">
                    <template #body="scope">
                      {{ $utils.formatThousandSeparator(scope.row.totalPrice ?? 0) }}
                    </template>
                  </GridColumn>
                  <GridColumn field="insepctionReason" title="异常描述" width="150" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="createUserName" title="检验人" width="60" :overflowTextEllipsis="true"></GridColumn>
                  <GridColumn field="createTime" title="检验时间" align="left" width="150" :sortable="true">
                    <template #body="scope">
                      {{ scope.row.inspectionConclusion == InspectionConclusion.待检验 || !scope.row.createTime ? '' :
                        $utils.formatDateTime(scope.row.createTime, 'yyyy-MM-dd HH:mm:ss') }}
                    </template>
                  </GridColumn>
                  <GridColumn field="arrivalCreateUserName" title="创建人" width="70" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="arrivalTime" title="到货时间" align="left" width="110" :sortable="true">
                    <template #body="scope">
                      {{ $utils.formatDateTime(scope.row.arrivalTime, 'yyyy-MM-dd') }}
                    </template>
                  </GridColumn>
                  <GridColumn field="arrivalCreateTime" title="创建时间" align="left" width="150">
                    <template #body="scope">
                      {{ $utils.formatDateTime(scope.row.arrivalCreateTime, 'yyyy-MM-dd HH:mm:ss') }}
                    </template>
                  </GridColumn>
                  <GridColumn field="remark" title="到货备注" width="200" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn field="purchaseRemark" title="采购备注" width="200" :overflowTextEllipsis="true">
                  </GridColumn>
                  <GridColumn v-for="(para, index) in additionalParam" :key="index"
                    :field="para.systemPara.id.toString()" :title="para.systemPara.dataValue" align="left" width="100">
                  </GridColumn>
                </GridHeaderRow>
              </GridColumnGroup>
              <GridColumnGroup :frozen="true" align="right">
                <GridHeaderRow>
                  <GridColumn field="act" title="操作" align="center" width="120">
                    <template #body="scope">
                      <ButtonGroup style="height: 24px">
                        <LinkButton @click="warehousing(scope.row, false)" v-if="!scope.row.isComplete &&
                          scope.row.receivableQuantity > 0"> 入库 </LinkButton>
                        <LinkButton v-if="!scope.row.isComplete &&
                          scope.row.receivableQuantity > 0 && item.value == ArrivalStatus.未完成"
                          @click="modifyArrivalState(scope.row, true)">确认完成</LinkButton>
                        <LinkButton
                          v-if="scope.row.isComplete && item.value == ArrivalStatus.已完成 && scope.row.receivableQuantity > 0"
                          @click="modifyArrivalState(scope.row, false)">取消完成</LinkButton>
                        <!-- <LinkButton @click="printDetail(scope.row)" v-if="scope.row.inventoryQuantity > 0">入库单打印
                        </LinkButton>
                        <LinkButton @click="printTag(scope.row)" v-if="scope.row.inventoryQuantity > 0">
                          标签打印
                        </LinkButton> -->
                      </ButtonGroup>
                    </template>
                  </GridColumn>
                </GridHeaderRow>
              </GridColumnGroup>
            </KingZonDataGrid>
          </div>
        </TabPanel>
      </Tabs>
    </div>
  </div>

  <Dialog ref="inboundDialog" :title="rowArrivalInspectionRecordDTO.arrivalRecordNo + '-新增入库'" :modal="true" closed
    :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
    <Inbound :dataType="rowArrivalInspectionRecordDTO.dataType" :dataId="rowArrivalInspectionRecordDTO.dataId"
      :inboundUnitList="arrivalunitData" :showCost="false" :showExtend="false" @close="inboundDialog.close()"
      @confirm="addSubmit" :priceDisabled="true" :showPrice="false" @addInbound="addInbound"
      :positionId="(rowArrivalInspectionRecordDTO.positionId ?? 0) > 0 ? rowArrivalInspectionRecordDTO.positionId : undefined">
      <template #showForm>
        <KingZonShowForm :formData="infoData" :columnWidth="'25%'">
        </KingZonShowForm>
      </template>
      <template #extend="{ row }">
        <div style="display: flex; justify-content: left;flex-wrap: wrap;align-items: center;margin-bottom: 10px;">
          <div v-for="para in row.purchaseParaList" :key="para.systemPara.id" class="field"
            style="width: calc(50% - 10px);flex: none;margin: 5px 0;">
            <span class="label" style="flex-shrink: 0;">{{ para.systemPara.dataValue }}：</span>
            <MutiDisplayTypeInput v-if="para.systemPara.isEdit" :para="para"
              @update:value="para.systemPara.value = $event">
            </MutiDisplayTypeInput>
            <TextBox style="flex:1" v-else :disabled="true" v-model="para.systemPara.value"></TextBox>
            <!-- <MutiDisplayTypeInput :para="para" @update:value="row[para.systemPara.id] = $event">
            </MutiDisplayTypeInput> -->
          </div>
        </div>
      </template>
    </Inbound>
  </Dialog>


  <Dialog ref="inventoryDetailDialog" :title="rowArrivalInspectionRecordDTO.arrivalRecordNo + '-入库详情'" :modal="true"
    closed :draggable="true" :dialogStyle="{ height: '90%', width: '85%' }" @close="refreshData()">
    <SourceInventory :sourceId="rowArrivalInspectionRecordDTO.arrivalRecordId" :sourceType="InventorySourceType.采购入库"
      ref="sourceInventory" :inventoryType="InventoryType.入库" :typeLable="'入库'" :isShowPrice="true" :formData="infoData"
      :rightWidth="60" :isShowPurchaseExt="true">
      <template #showForm>
        <KingZonShowForm :formData="infoData" :columnWidth="'25%'">
        </KingZonShowForm>
        <div class="kingzon-btn-group" style="margin: 5px;" v-if="selectedTab == ArrivalStatus.未完成">
          <LinkButton iconCls="icon-add" @click="warehousing(rowArrivalInspectionRecordDTO, true)">新增入库
          </LinkButton>
        </div>
      </template>
      <template #right>
        <GridHeaderRow>
          <GridColumn field="act" title="操作" align="center" width="60">
            <template #body="scope">
              <ButtonGroup style="height: 24px">
                <LinkButton @click="deleteRecord(scope.row)">删除</LinkButton>
              </ButtonGroup>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </template>
    </SourceInventory>
  </Dialog>
  <!-- 批量入库 -->
  <Drawer ref="batchInboundDrawer" v-if="isShowBatchInboundDrawer" :closable="true" region="east"
    :drawerStyle="{ width: '90%' }" title="批量入库" @collapse="isShowBatchInboundDrawer = false">
    <BatchInbound :arrivalRecordIds="selectRowIds" @success="addBatchSubmit" @cancel="closeBatchInboundDrawer">
    </BatchInbound>
  </Drawer>
</template>
<script lang='ts'>
import { defineComponent } from 'vue';
import { cloneDeep } from 'lodash';
import { SystemParaTreeDto } from '@/types/common';
import { CustomAttribute, Filter, Search, ShowFormData, type FilterTmp, type SelectOption, } from '@/types/types'
import {
  InspectionConclusion, PurchaseType, ProductType, SystemParaType, FilterType, DataType,
  CategoryType, ShowFormDataType, InventorySourceType,
  InventoryDataType,
  InventoryDataStatus,
  InventoryType,
} from '@/types/enums'
import { ArrivalInspectionRecordDTO, InventoryUnitDto, PostInventoryRecordDTO } from '@/types/inventory'
import SourceInventory from '@/views/distribute_material/source-inventory-detail.vue';
import UnitQuantityShow from '@/views/arrival_record_management/unit-quantity-show.vue';
import Inbound from '@/views/inventory_management/inbound.vue'
import Decimal from 'decimal.js';
import BatchInbound from './batch-inbound.vue';
enum ArrivalStatus {
  未完成 = 0,
  已完成 = 1
}
export default defineComponent({
  name: "PurchaseWarehousing",
  components: {
    Inbound, SourceInventory, UnitQuantityShow, BatchInbound
  },
  data() {
    return {
      isLoading: false,
      searchParams: [] as Search[],
      searchParamsHidden: [] as Search[],
      filterList: [] as Filter[],
      tabList: [] as SelectOption[],
      selectedTab: ArrivalStatus.未完成,
      arrivalRecordList: {} as { [key: string]: ArrivalInspectionRecordDTO[] },
      additionalParam: [] as SystemParaTreeDto[],
      isFinished: false,

      inspectionConclusionList: [] as SelectOption[],
      productTypeList: [] as SelectOption[],
      purchaseTypeList: [] as SelectOption[],
      productCategoryList: [] as SelectOption[],

      isInspectBeforInvent: false,
      rowArrivalInspectionRecordDTO: {} as ArrivalInspectionRecordDTO,

      infoData: [] as ShowFormData[],
      warehouseData: [] as any[],
      postData: {} as {
        arrivalRecordId: number,
        dataType: InventoryDataType,
        dataId: number,
        inventoryRecordList: PostInventoryRecordDTO[]
      },
      arrivalunitData: {} as any,
      isConcede: false,

      inventoryDetailDialog: {} as EasyUI.Dialog,
      inboundDialog: {} as EasyUI.Dialog,
      isShowPrice: false,
      isInnerCreate: false,
      batchInboundDrawer: {} as EasyUI.Drawer,
      isShowBatchInboundDrawer: false,
      selectRowIds: [] as number[]
    }
  },
  computed: {
    InspectionConclusion() {
      return InspectionConclusion
    },
    PurchaseType() {
      return PurchaseType
    },
    ProductType() {
      return ProductType
    },
    InventorySourceType() {
      return InventorySourceType;
    },
    InventoryDataType() {
      return InventoryDataType;
    },
    InventoryDataStatus() {
      return InventoryDataStatus;
    },
    ArrivalStatus() {
      return ArrivalStatus;
    },
    InventoryType() {
      return InventoryType;
    }
  },
  created() {
    this.tabList = this.$utils.getEnumKeyValueArray(ArrivalStatus);
    this.inspectionConclusionList = this.$utils.getEnumKeyValueArray(InspectionConclusion);
    this.inspectionConclusionList.unshift({ value: '', text: '全部' });
    this.productTypeList = this.$utils.getEnumKeyValueArray(ProductType);
    this.purchaseTypeList = this.$utils.getEnumKeyValueArray(PurchaseType);
  },
  mounted() {
    this.init() //页面数据初始化
    this.inventoryDetailDialog = this.$refs.inventoryDetailDialog as EasyUI.Dialog;
    this.inboundDialog = this.$refs.inboundDialog as EasyUI.Dialog;
  },
  methods: {

    //初始化
    async init() {
      try {
        this.isLoading = true;
        var resAddition = await this.getAdditionalParam();
        var totalParas = resAddition.data as SystemParaTreeDto[];
        this.additionalParam = totalParas.filter((item: SystemParaTreeDto) => item.systemPara.dataType == SystemParaType.外购物料信息);
        this.isConcede = totalParas.some((item: SystemParaTreeDto) => item.systemPara.dataType == SystemParaType.是否让步接收 && Number(item.systemPara.dataValue) == 1);
        this.isInspectBeforInvent = totalParas.some((item: SystemParaTreeDto) => item.systemPara.dataType == SystemParaType.是否检验通过才能入库 && Number(item.systemPara.dataValue) == 1);
        this.isShowPrice = totalParas.some((item: SystemParaTreeDto) => item.systemPara.dataType == SystemParaType.采购到货单价显示 && Number(item.systemPara.dataValue) == 1);
        var resCategoryList = await this.getCategories();
        this.productCategoryList = (resCategoryList.data as any).map((item: any) => {
          return { value: item.id, text: item.name }
        }) as SelectOption[];
        this.addSearchParam();
        await this.refreshData();
      }
      catch (err: any) {
        this.$kingzon_messager.showError(err);
        this.isLoading = false;
      }
    },

    // 1、搜索相关
    search(params: FilterTmp[]): void {
      let commonFilterList = params.find((item) => item.table == 'common')?.filter ?? [];
      this.filterList = commonFilterList;
      this.refreshData();
    },

    addSearchParam() {
      this.searchParams = [];
      this.searchParamsHidden = [];
      this.searchParams.push(new Search('arrivalRecordNo', '到货编号', 'common', FilterType.Contains, DataType.Text));
      this.searchParams.push(new Search('purchaseOrderNo', '采购单号', 'common', FilterType.Contains, DataType.Text));
      this.searchParams.push(new Search('productNo', '物料编码', 'common', FilterType.Contains, DataType.Text));
      this.searchParams.push(new Search('productName', '物料名称', 'common', FilterType.Contains, DataType.Text));
      this.searchParams.push(new Search('productTechnicalCode', '技术型号', 'common', FilterType.Contains, DataType.Text));

      this.searchParamsHidden.push(new Search('productType', '类型', 'common', FilterType.Equals, DataType.SelectNum, '', this.productTypeList));
      this.searchParamsHidden.push(new Search('productCategoryId', '物料类别', 'common', FilterType.Equals, DataType.SelectNum, '', this.productCategoryList));
      this.searchParamsHidden.push(new Search('productSpecification', '规格型号', 'common', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('materialGrade', '材质牌号', 'common', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('productPackageWay', '包装方式', 'common', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('productRemark', '物料备注', 'common', FilterType.Contains, DataType.Text));

      this.searchParamsHidden.push(new Search('purchaseType', '采购类型', 'common', FilterType.IntArrayContains, DataType.multiSelectNum, "", this.purchaseTypeList));
      this.searchParamsHidden.push(new Search('supplierName', '供应商', 'common', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('inspectionConclusion', '检验结论', 'common', FilterType.Equals, DataType.SelectNum, '', this.isConcede ? this.inspectionConclusionList : this.inspectionConclusionList.filter((item) => item.value != InspectionConclusion.让步接收)));
      this.searchParamsHidden.push(new Search('inspectionReason', '异常描述', 'common', FilterType.Contains, DataType.Text));

      this.searchParamsHidden.push(new Search('createUserName', '检验人', 'common', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('createTime', '检验时间', 'common', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '起始日期'));
      this.searchParamsHidden.push(new Search('createTime', '', 'common', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '截止日期'));

      this.searchParamsHidden.push(new Search('arrivalCreateUserName', '到货人', 'common', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('arrivalTime', '到货时间', 'common', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '起始日期'));
      this.searchParamsHidden.push(new Search('arrivalTime', '', 'common', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '截止日期'));

      this.searchParamsHidden.push(new Search('arrivalCreateTime', '创建时间', 'common', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '起始日期'));
      this.searchParamsHidden.push(new Search('arrivalCreateTime', '', 'common', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '截止日期'));



      this.searchParamsHidden.push(new Search('remark', '到货备注', 'common', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('purchaseRemark', '采购备注', 'common', FilterType.Contains, DataType.Text));
      this.$utils.additionalParam2SearchParam(this.additionalParam, this.searchParamsHidden, 'common');
    },

    getData() {
      this.filterList = this.filterList.filter((item) => item.key != 'isFinished');
      this.filterList.push(new Filter('isFinished', this.selectedTab == ArrivalStatus.已完成, FilterType.Equals, DataType.SelectBoolean));
      var postData = {
        filterList: this.filterList,
        isPaged: true,
      }
      return this.$http.post(
        '/api/inventory/PurchasingWarehousing/GetArrivalInspectionInventoryRecords',
        postData,
        this.$refs['dataGrid' + this.selectedTab]
      );
    },

    getCategories(): Promise<HttpResponseType> {
      var types = [] as CategoryType[]
      types.push(CategoryType.原材料类别)
      types.push(CategoryType.产品类别)
      return this.$http.post('/api/base/Category/GetChildrenCategoryListByType', types);
    },
    // 数据刷新
    async refreshData() {
      try {
        this.isLoading = true;
        var resArrivalRecord = await this.getData();
        this.arrivalRecordList[this.selectedTab] = resArrivalRecord.data as ArrivalInspectionRecordDTO[];
        this.arrivalRecordList[this.selectedTab].forEach((item) => {
          //展开附加参数（用于dataGrid显示）
          this.additionalParam.forEach((para: SystemParaTreeDto) => {
            item[para.systemPara.id.toString()] = item.customAttributeList.find((p: CustomAttribute) => p.systemParaId == para.systemPara.id)?.value;
          });
        });
      }
      catch (err: any) {
        this.$kingzon_messager.showError(err);
      }
      finally {
        this.isLoading = false;
      }
    },

    getAdditionalParam(): Promise<HttpResponseType> {
      var types = [] as SystemParaType[];
      types.push(SystemParaType.外购物料信息, SystemParaType.是否让步接收, SystemParaType.是否检验通过才能入库, SystemParaType.采购到货单价显示);
      return this.$http.post('/api/base/SystemPara/GetAdditionalParamByTypes', types)
    },


    // 3、操作相关
    tabSelect(tabPanel: EasyUI.TabPanel): void {
      this.selectedTab = ArrivalStatus[tabPanel.title] as ArrivalStatus;
      this.refreshData();
    },


    // 4.1 入库
    warehousing(row: any, isInnerCreate: boolean): void {
      this.rowArrivalInspectionRecordDTO = cloneDeep(row);
      this.initFormInfo(row);
      this.rowArrivalInspectionRecordDTO.dataType = !this.rowArrivalInspectionRecordDTO.specificationId ? InventoryDataType.Product : InventoryDataType.Specification;
      this.rowArrivalInspectionRecordDTO.dataId = !this.rowArrivalInspectionRecordDTO.specificationId ? this.rowArrivalInspectionRecordDTO.productId : this.rowArrivalInspectionRecordDTO.specificationId;
      this.getWarehouseList();

      //到货单位信息
      this.arrivalunitData = row.unitInfoList.map((item: InventoryUnitDto) => {
        return {
          id: item.id,
          unitId: item.unitId,
          unitName: item.unitName,
          ratio: item.ratio,
          remark: item.remark,
          conversionType: item.conversionType,
          quantity: item.ratio != 0 ? new Decimal(row.receivableQuantity).div(item.ratio).toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber() : 0
        } as InventoryUnitDto
      });
      this.arrivalunitData.push({
        id: row.arrivalUnitConversionId,
        unitId: row.arrivalUnitId,
        unitName: row.arrivalUnit,
        ratio: row.ratio,
        remark: undefined,
        conversionType: undefined,
        quantity: row.ratio != 0 ? new Decimal(row.receivableQuantity).div(row.ratio).toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber() : 0
      } as InventoryUnitDto)
      //this.arrivalunitData.push(...row.unitInfoList.filter((item: InventoryUnitDto) => item.id != row.arrivalUnitConversionId));

      //继承选中到货行的扩展参数
      this.additionalParam.forEach(para => {
        const attr = row.customAttributeList?.find(attr => attr.systemParaId === para.systemPara.id);
        if (attr) {
          para.systemPara.value = attr.value as any;
        } else {
          para.systemPara.value = undefined;
        }
        para.systemPara.isEdit = para.systemPara.value === undefined || para.systemPara.value === null || para.systemPara.value === '';
        if (!para.systemPara.isEdit) {
          para.systemPara.value = this.$utils.formatSystemParaValue(para.systemPara, para.systemPara.value);
        }
      });

      this.isInnerCreate = isInnerCreate;
      this.inboundDialog.open()
    },


    async addSubmit(info) {
      if (info.dataList.length <= 0) {
        this.$kingzon_messager.showError("请添加入库记录");
        return;
      }
      var inventorys = info.dataList.flatMap((item) => item.unitQtyList).filter(x => x.id == -1);
      var totalQuantity = inventorys.reduce((sum, item) => sum.plus(item.quantity), new Decimal(0)).toNumber();
      if (totalQuantity <= 0) {
        this.$kingzon_messager.showError("入库数量必须大于0");
        return;
      }
      if (new Decimal(totalQuantity).toDecimalPlaces(6, Decimal.ROUND_HALF_UP).greaterThan(new Decimal(this.rowArrivalInspectionRecordDTO.receivableQuantity).toDecimalPlaces(6, Decimal.ROUND_HALF_UP))) {
        this.$kingzon_messager.showError("入库数量不能大于待入库量");
        return;
      }

      var postData = {
        arrivalRecordId: this.rowArrivalInspectionRecordDTO.arrivalRecordId,
        totalQuantity: totalQuantity,
        inventoryRecordList: info.dataList.map((item) => {
          return {
            unitQtyList: item.unitQtyList,
            remark: item.remark ?? '',
            positionId: item.positionId,
            customAttributeList: item.purchaseParaList.map((x) => {
              return {
                systemParaId: x.systemPara.id,
                value: x.systemPara.value
              }
            })
          }
        })
      }
      this.isLoading = true;
      this.$http.post('/api/inventory/PurchasingWarehousing/PostPurchaseWarehousingInventoryRecord', postData)
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          this.inboundDialog.close();
          var source = this.$refs.sourceInventory as any;
          if (this.isInnerCreate && source) {
            source.getDataAndRefresh();
            this.getCurrentInfo();
          }
          this.refreshData();
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err);
          this.isLoading = false;
        })
    },

    // 4.2完成到货入库
    modifyArrivalState(row: ArrivalInspectionRecordDTO, isComplete: boolean): void {
      this.$kingzon_messager.showConfim("是否确认" + (isComplete ? '' : '取消') + "完成到货记录【" + row.arrivalRecordNo + "】的入库")
        .then(() => {
          this.isLoading = true;
          var postData = { id: row.arrivalRecordId, isComplete: isComplete };
          return this.$http.post('/api/inventory/PurchasingWarehousing/ModifyArrivalState', postData)
            .then((res: any) => {
              this.$kingzon_messager.showInfo(res.data);
              this.refreshData();
            })
            .catch((err) => {
              this.$kingzon_messager.showError(err);
              this.isLoading = false;
            })
        })
    },

    //显示入库详情
    inventoryDetail(row: any): void {
      if (row.inventoryQuantity <= 0) {
        this.$kingzon_messager.showInfo("暂无入库记录")
        return
      }
      this.rowArrivalInspectionRecordDTO = cloneDeep(row);
      this.initFormInfo(row)
      this.inventoryDetailDialog.open();
    },

    getCurrentInfo() {
      this.isLoading = true;
      var postData = {
        filterList: [new Filter('arrivalRecordId', this.rowArrivalInspectionRecordDTO.arrivalRecordId, FilterType.Equals, DataType.SelectNum)],
        isPaged: true,
      }
      this.$http.post(
        '/api/inventory/PurchasingWarehousing/GetArrivalInspectionInventoryRecords',
        postData,
        this.$refs['dataGrid' + this.selectedTab]
      )
        .then((res: any) => {
          var data = res.data as ArrivalInspectionRecordDTO[];
          if (data.length > 0) {
            this.rowArrivalInspectionRecordDTO = data[0];
            this.initFormInfo(this.rowArrivalInspectionRecordDTO);
          }
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err);
        })
        .finally(() => {
          this.isLoading = false;
        })
    },

    initFormInfo(row) {
      this.infoData = [
        {
          label: '采购单号',
          value: row.purchaseOrderNo,
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: '供应商',
          value: row.supplierName,
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: '技术型号',
          value: row.productTechnicalCode,
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: '物料类别',
          value: row.productCategory,
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: '物料编码',
          value: row.productNo,
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: '物料名称',
          value: row.productName,
          type: ShowFormDataType.文本,
          validate: true,
        },

        {
          label: '规格型号',
          value: row.productSpecification,
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: '材质牌号',
          value: row.materialGrade,
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: this.isShowPrice ? '单价' : '物料状态',
          value: this.isShowPrice ? (this.rowArrivalInspectionRecordDTO.purchasePrice ? this.$utils.formatThousandSeparator(this.rowArrivalInspectionRecordDTO.purchasePrice) : 0) + '元/' + this.rowArrivalInspectionRecordDTO.productUnit
            + (this.rowArrivalInspectionRecordDTO.inventoryQuantity ? '，最新入库单价：' + (this.rowArrivalInspectionRecordDTO.inventoryPrice ? this.$utils.formatThousandSeparator(this.rowArrivalInspectionRecordDTO.inventoryPrice) : 0) + '元/' + this.rowArrivalInspectionRecordDTO.productUnit : '')
            : this.rowArrivalInspectionRecordDTO.dataStatus ? InventoryDataStatus[this.rowArrivalInspectionRecordDTO.dataStatus] : '',
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: '到货量',
          value: this.getQuantityList(this.rowArrivalInspectionRecordDTO.arrivalQuantity, this.rowArrivalInspectionRecordDTO.unitInfoList).map(x => x.text + x.extension).join(','),
          type: ShowFormDataType.文本,
          validate: true,
        },
        {
          label: '已入库量',
          value: this.getQuantityList(this.rowArrivalInspectionRecordDTO.inventoryQuantity, this.rowArrivalInspectionRecordDTO.unitInfoList).map(x => x.text + x.extension).join(','),
          type: ShowFormDataType.文本,
          validate: true,
        },

      ] as ShowFormData[];

      if (this.selectedTab == ArrivalStatus.未完成) {
        this.infoData.push({
          label: '待入库量',
          value: this.getQuantityList(this.rowArrivalInspectionRecordDTO.receivableQuantity, this.rowArrivalInspectionRecordDTO.unitInfoList).map(x => x.text + x.extension).join(','),
          type: ShowFormDataType.文本,
          validate: true,
        } as ShowFormData);
      }

      if (this.selectedTab == ArrivalStatus.已完成 && this.isShowPrice) {
        this.infoData.push({
          label: '已入库金额',
          value: (this.rowArrivalInspectionRecordDTO.totalPrice ? this.$utils.formatThousandSeparator(this.rowArrivalInspectionRecordDTO.totalPrice) : 0) + '元/' + this.rowArrivalInspectionRecordDTO.productUnit,
          type: ShowFormDataType.文本,
          validate: true,
        } as ShowFormData);
      }

      //将最后一项的宽度设置撑满一行
      var lastOne = this.infoData.at(-1);
      if (lastOne) {
        lastOne.columnWidth = (this.infoData.length % 4) > 0 ? (this.infoData.length % 4) * 25 + "%" : "25%"
      }
    },

    getWarehouseList() {
      this.isLoading = true;
      this.$http.post('/api/inventory/InventoryManagement/GetAvailableAllPosition', {
        dataType: this.rowArrivalInspectionRecordDTO.dataType,
        dataId: this.rowArrivalInspectionRecordDTO.dataId,
      })
        .then((res) => {
          this.warehouseData = res.data as any[];
        })
        .catch((err) => {
          console.error('获取数据失败:', err);
        })
        .finally(() => {
          this.isLoading = false;
        });
    },
    printOrder(row: any): void {
      this.$kingzon_messager.showInfo("功能开发中！")
    },
    printOrderTag(row: any): void {
      this.$kingzon_messager.showInfo("功能开发中！");
    },
    printDetail(row: any): void {
      this.$kingzon_messager.showInfo("功能开发中！");
    },
    printTag(row: any): void {
      this.$kingzon_messager.showInfo("功能开发中！");
    },
    deleteRecord(row: any): void {
      this.$kingzon_messager.showConfim("是否确认删除入库记录")
        .then(() => {
          this.isLoading = true;
          return this.$http.get('/api/inventory/PurchasingWarehousing/DeletePurchaseInventoryRecord', { id: row.id })
            .then((res: any) => {
              this.$kingzon_messager.showInfo(res.data);
              var source = this.$refs.sourceInventory as any;
              if (source) {
                source.getDataAndRefresh();
              }
              this.getCurrentInfo();
            })
            .catch((err) => {
              this.$kingzon_messager.showError(err);
              this.isLoading = false;
            })
        })
    },
    getQuantityList(quantity, unitList) {
      var list = [] as SelectOption[];
      for (var item of unitList) {
        var unitQuantity = item.ratio != 0 ? new Decimal(quantity).dividedBy(item.ratio).toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber() : 0;
        list.push({ text: this.$utils.formatThousandSeparator(unitQuantity), value: unitQuantity, code: item.id, extension: item.unitName.replace(/【[\s\S]*?】/g, "") });
      }
      return list;
    },
    addInbound(newData) {
      //入库记录继承到货记录的扩展参数
      newData.purchaseParaList = cloneDeep(this.additionalParam);
    },
    seletedRowsChanged() {
      var table = this.$refs['dataGrid' + this.selectedTab] as any
      if (table && table.length > 0) {
        this.selectRowIds = table[0].selectedRows.map((x: ArrivalInspectionRecordDTO) => x.arrivalRecordId)
      } else {
        this.selectRowIds = []
      }
    },
    openBatchInboundDrawer() {
      if (this.selectRowIds.length <= 0) {
        this.$kingzon_messager.showError("请至少选择一条到货记录");
        return;
      }
      this.isShowBatchInboundDrawer = true
      this.$nextTick(() => {
        this.batchInboundDrawer = this.$refs.batchInboundDrawer as EasyUI.Drawer
        this.batchInboundDrawer.expand()
      })
    },
    closeBatchInboundDrawer() {
      this.batchInboundDrawer.collapse()
    },
    async addBatchSubmit() {
      this.closeBatchInboundDrawer()
      this.$kingzon_messager.showInfo("入库完成")
      var table = this.$refs['dataGrid' + this.selectedTab] as any
      var dataGrid = table[0] as any as EasyUI.DataGrid
      dataGrid.selectedRows = []
      await this.refreshData()
    }
  }
})
</script>

<style scoped>
.field {
  display: flex;
  flex-direction: row;
  align-items: center;
  flex: 1;
  min-width: 0;
}

.field .label {
  width: 80px;
  text-align: right;
}

.field .box {
  flex: 1;
}
</style>
