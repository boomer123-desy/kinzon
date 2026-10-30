<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page">
    <div class="kingzon-page-title">
      <span>生产派工管理</span>
    </div>
    <div class="kingzon-page-body">
      <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search">
      </SearchPanel>
      <Tabs :plain="true" style="height: 100%;" ref="tabs" @tabSelect="tabSelect" :selectedIndex="1">
        <TabPanel v-for="(item, index) in tabList" :key="index" :title="item.text">
          <div class="kingzon-page-content" style="flex-direction: row;">
            <Panel title="派工部门" style="width: 150px; flex-grow: 1;" :bodyStyle="{ height: '100%' }">
              <div :class="!selectdDepartment ? 'selected-department' : 'department-info'"
                style="padding: 5px;cursor: pointer;" @click="selecteDepartment(undefined)">
                全部部门
              </div>
              <div v-for="department in totalDepartments" :key="department.value"
                :class="selectdDepartment?.value == department.value ? 'selected-department' : 'department-info'"
                style="padding: 5px;cursor: pointer;" @click="selecteDepartment(department)">
                {{ department.text }}
              </div>
            </Panel>
            <div style="display: flex;flex-direction: column;height: 100%;" class="kingzon-page-fill">
              <div class="kingzon-btn-group" style="margin: 5px">
                <LinkButton iconCls="icon-add" @click="showSeleteDrawer"
                  v-if="seletedTab == CustomDispatchOrderStatus.待开始 || seletedTab == CustomDispatchOrderStatus.进行中">新增
                </LinkButton>
              </div>
              <KingZonDataGrid :id="`dispatchOrderNotOverDataGrid_${index}`" class="kingzon-page-fill"
                v-if="item.value != CustomDispatchOrderStatus.已完成" :ref="'dataGrid' + item.value"
                :data="orderList[item.value as string]" :showDownLoad="true" :exportFileName="'生产派工单'"
                @pageChange="getDataAndRefresh" @sortChange="getDataAndRefresh" :virtualScroll="true"
                :virtualPageSize="20" :dblclickToEdit='true' editMode="cell" :showFooter="true" :footerData="footerData"
                :sorts="[{ field: 'dispatchDate', order: 'desc' }]" :rowCss="getRowCss" style="overflow-y: unset;">
                <GridColumnGroup :frozen="true" width="680" align="left">
                  <GridHeaderRow>
                    <GridColumn align="center" width="40">
                      <template #body="scope">
                        {{ scope.rowIndex + 1 }}
                      </template>
                    </GridColumn>
                    <GridColumn field="orderNo" title="派工单号" width="130" align="left" :sortable="true"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="technicalCode" title="技术型号" align="left" width="100"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productNo" title="产品编码" width="100" align="left" :sortable="true"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productName" title="产品名称" width="190" align="left" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="specification" title="规格型号" align="left" width="120"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                  </GridHeaderRow>
                </GridColumnGroup>
                <GridColumnGroup :columnMoving="true" align="center">
                  <GridHeaderRow>
                    <GridColumn field="productType" title="类型" width="70" :rowspan="2">
                      <template #body="scope">
                        {{ ProductType[scope.row.productType] }}
                      </template>
                    </GridColumn>
                    <GridColumn field="packageWay" :rowspan="2" title="包装方式" align="left" width="80"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productCategory" :rowspan="2" title="产品类别" align="left" width="100"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productRemark" :rowspan="2" title="产品备注" width="120" align="left"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="packageSpecification" title="外包装方案" width="120" :rowspan="2"
                      :editable="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始"
                      :cellCss="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始 ? 'kingzon-cell-editable' : ''">
                      <template #body="scope">
                        <div style="display: flex; align-items: center; width: 100%;">
                          <span
                            style="flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                            {{
                              scope.row.pacakgeList && scope.row.pacakgeList.find(x => x.value == scope.row.packageId)
                                ? scope.row.pacakgeList.find(x => x.value == scope.row.packageId)?.text
                                : scope.row.packageId ? scope.row.packageSpecification : ''
                            }}
                          </span>
                          <FilePreview v-if="scope.row.packageId">
                            <template v-slot="preview">
                              <font-awesome-icon :icon="['fas', 'file-pdf']"
                                style="color: #1677c8; cursor: pointer; margin-left: 6px; flex-shrink: 0;" title="预览"
                                @click.stop="preViewPdf(preview, scope.row.packageId)" />
                            </template>
                          </FilePreview>
                        </div>
                      </template>
                      <template #edit="scope">
                        <ComboBox v-model="scope.row.packageId" :data="scope.row.pacakgeList"
                          @valueChange="onChangePackage(scope.row)">
                          <Addon>
                            <span v-if="scope.row.packageId" class="textbox-icon icon-clear"
                              @click="scope.row.packageId = undefined; modifyDispatch(scope.row)"></span>
                          </Addon>
                        </ComboBox>
                      </template>
                    </GridColumn>
                    <GridColumn field="dispatchDate" title="派工时间" width="120" :sortable="true"
                      :editable="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始"
                      :rowspan="2"
                      :cellCss="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始 ? 'kingzon-cell-editable' : ''">
                      <template #body="scope">
                        {{ scope.row.dispatchDate ? $utils.formatDateTime(scope.row.dispatchDate, "yyyy-MM-dd") : ''
                        }}
                      </template>
                      <template #edit="scope">
                        <DateBox v-model="scope.row.dispatchDate" format="yyyy-MM-dd"
                          @selectionChange="modifyDispatch(scope.row)"
                          :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
                          style="width: 100%;">
                        </DateBox>
                      </template>
                    </GridColumn>
                    <GridColumn field="planCompleteDate" title="预计完工时间" width="120" :sortable="true"
                      :editable="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始"
                      :rowspan="2"
                      :cellCss="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始 ? 'kingzon-cell-editable' : ''">
                      <template #body="scope">
                        {{ scope.row.planCompleteDate ? $utils.formatDateTime(scope.row.planCompleteDate,
                          "yyyy-MM-dd")
                          :
                          '' }}
                      </template>
                      <template #edit="scope">
                        <DateBox v-model="scope.row.planCompleteDate" format="yyyy-MM-dd"
                          @selectionChange="modifyDispatch(scope.row);"
                          :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
                          style="width: 100%;">
                        </DateBox>
                      </template>
                    </GridColumn>
                    <GridColumn field="departmentName" :rowspan="2" title="派工部门" align="left" width="100"
                      :sortable="true"
                      :editable="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始"
                      :cellCss="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始 ? 'kingzon-cell-editable' : ''">
                      <template #body="scope">
                        {{departmentList.find(x => x.value == scope.row.departmentId) ? departmentList.find(x =>
                          x.value
                          ==
                          scope.row.departmentId)?.text : scope.row.departmentId ? scope.row.departmentName : ''}}
                      </template>
                      <template #edit="scope">
                        <ComboBox v-model="scope.row.departmentId" :data="scope.row.departmentList"
                          v-if="scope.row.completeQuantity <= 0" @valueChange="onChangeDepartment(scope.row)">
                        </ComboBox>
                        <span v-else>{{ scope.row.departmentName }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="progress" :rowspan="2" title="生产进度" align="right" width="100">
                      <template #body="scope">
                        <div style="text-align:right" v-if="scope.row.dispatchQuantity">{{
                          Number(((scope.row.completeQuantity /
                            scope.row.dispatchQuantity) * 100).toFixed(2)) }}%</div>
                        <ProgressBar v-if="scope.row.dispatchQuantity" barCls="c6" style="height: 15px;"
                          :value="Number(((scope.row.completeQuantity / scope.row.dispatchQuantity) * 100).toFixed(2))"
                          :showValue="false">
                        </ProgressBar>
                      </template>
                    </GridColumn>
                    <GridColumn field="totalInboundQuantity" :rowspan="2" title="入库总量" align="right" width="85">
                      <template #body="scope">
                        {{ scope.row.totalInboundQuantity }}{{ scope.row.dispatchUnitInfo.productionUnitName
                        }}</template>
                    </GridColumn>
                    <GridColumn :colspan="4" title="生产单位" align="center">
                    </GridColumn>
                    <GridColumn :colspan="4" title="收入计价单位" align="center">
                    </GridColumn>
                    <GridColumn :colspan="4" title="产品单位" align="center">
                    </GridColumn>
                    <GridColumn :colspan="4" title="成品主单位" align="center">
                    </GridColumn>
                    <GridColumn field="price" :rowspan="2" title="加工单价" align="right" width="80" :sortable="true"
                      :cellCss="'kingzon-detail-text'">
                      <template #body="scope">
                        <span class="kingzon-textEllipsis" :title="'更新加工单价'" @click="showModifyPrice(scope.row)">{{
                          scope.row.price }}
                          <font-awesome-icon icon="fa-regular fa-pen-to-square" />
                        </span>
                      </template>
                    </GridColumn>
                    <GridColumn field="formulaName" :rowspan="2" title="领料配方" align="right" width="80">
                    </GridColumn>
                    <GridColumn field="saleOrders" :rowspan="2" title="销售单" width="130">
                      <template #body="scope">
                        <div v-if="scope.row.saleOrders && scope.row.saleOrders.length > 0"
                          :title="scope.row.saleOrders.map(item => `${item.orderNo}${item.customerName}：${item.dispatchQuantity}${item.dispatchUnit}`).join('\n')">
                          <div v-for="(item, index) in scope.row.saleOrders" :key="index" class="kingzon-textEllipsis">
                            {{ item.orderNo }}{{ item.customerName }}：{{ item.dispatchQuantity }}{{ item.dispatchUnit }}
                          </div>
                        </div>
                      </template>
                    </GridColumn>
                    <GridColumn v-for="para in productionParaList" :key="para.systemPara.id"
                      :field="para.systemPara.id.toString()" :title="para.systemPara.dataValue" width="100" :rowspan="2"
                      :editable="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始"
                      :cellCss="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始 ? 'kingzon-cell-editable' : ''">
                      <template #body="scope">
                        <div class="kingzon-textEllipsis"
                          :title="scope.row.productionAttributeList?.find(attr => attr.systemParaId === para.systemPara.id)?.value">
                          {{scope.row.productionAttributeList?.find(attr => attr.systemParaId ===
                            para.systemPara.id)?.value}}
                        </div>
                      </template>
                      <template #edit="scope">
                        <TextBox
                          v-if="scope.row.productionAttributeList?.find(attr => attr.systemParaId === para.systemPara.id)"
                          :value="scope.row.productionAttributeList.find(attr => attr.systemParaId === para.systemPara.id).value"
                          style="width: 100%;" @blur="modifyDispatch(scope.row)"
                          @valueChange="scope.row.productionAttributeList.find(attr => attr.systemParaId === para.systemPara.id).value = $event.currentValue">
                        </TextBox>
                        <div v-else></div>
                      </template>
                    </GridColumn>
                    <GridColumn field="remark" :rowspan="2" title="备注" align="left" width="150"
                      :editable="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始"
                      :cellCss="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始 ? 'kingzon-cell-editable' : ''"
                      :overflowTextEllipsis="true">
                      <template #edit="scope">
                        <TextBox v-model="scope.row.remark" @blur="modifyDispatch(scope.row)">
                        </TextBox>
                      </template>
                    </GridColumn>
                    <GridColumn field="createUserName" :rowspan="2" title="创建人" width="70" align="left"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="createTime" :rowspan="2" title="创建时间" align="left" width="150" :sortable="true">
                      <template #body="scope">
                        {{ $utils.formatDateTime(scope.row.createTime, 'yyyy-MM-dd HH:mm:ss') }}
                      </template>
                    </GridColumn>
                  </GridHeaderRow>
                  <GridHeaderRow>
                    <!-- 生产 -->
                    <GridColumn field="dispatchQuantity" title="派工数量" align="right" width="90" :sortable="true"
                      :editable="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始"
                      :cellCss="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始 ? 'kingzon-cell-editable' : ''">
                      <template #edit="scope">
                        <NumberBox v-model="scope.row.dispatchQuantity" :spinners="true" :min="0" :precision="4"
                          @valueChange="onChangeDispatchQuantity(scope.row)">
                        </NumberBox>
                      </template>
                    </GridColumn>
                    <GridColumn field="receiveQuantity" title="已接单数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span
                          :style="{ color: scope.row.receiveQuantity <= scope.row.dispatchQuantity ? '' : 'var(--color-red)' }"
                          :class="scope.row.receiveQuantity ? 'kingzon-detail-text' : ''"
                          @click="showOperations(scope.row, OperateType.接单)">{{ scope.row.
                            receiveQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="completeQuantity" title="已完工数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span :class="scope.row.completeQuantity ? 'kingzon-detail-text' : ''"
                          @click="showOperations(scope.row, OperateType.出单)">{{ scope.row.
                            completeQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="dispatchUnitInfo.productionUnitName" title="单位" align="left" width="100"
                      :editable="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始"
                      :cellCss="seletedTab == CustomDispatchOrderStatus.进行中 || seletedTab == CustomDispatchOrderStatus.待开始 ? 'kingzon-cell-editable' : ''">
                      <template #edit="scope">
                        <ComboBox v-model="scope.row.dispatchUnitInfo.unitConversionId" :data="scope.row.unitList"
                          v-if="scope.row.completeQuantity <= 0" @valueChange="onChangeProductUnit(scope.row)">
                        </ComboBox>
                        <span v-else>{{ scope.row.dispatchUnitInfo.productionUnitName }}</span>
                      </template>
                    </GridColumn>


                    <!-- 计价单位 -->
                    <GridColumn field="incomeQuantity" title="派工数量" align="right" width="90" :sortable="true">
                      <!-- <template #edit="scope">
                      <NumberBox v-model="scope.row.incomeQuantity" @valueChange="onChangeIncomeQuantity(scope.row)"
                        :spinners="true" :min="0" :disabled="disabledInputIncomQuantity(scope.row)" :precision="4">
                      </NumberBox>
                    </template> -->
                    </GridColumn>
                    <GridColumn field="incomeReceiveQuantity" title="已接单数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span
                          :style="{ color: scope.row.receiveQuantity <= scope.row.dispatchQuantity ? '' : 'var(--color-red)' }">{{
                            scope.row.incomeReceiveQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="incomeCompleteQuantity" title="已完工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="dispatchUnitInfo.incomeUnitName" title="单位" align="left" width="80">
                    </GridColumn>

                    <!-- 产品单位 -->
                    <GridColumn field="baseQuantity" title="派工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="baseReceiveQuantity" title="已接单数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span
                          :style="{ color: scope.row.receiveQuantity <= scope.row.dispatchQuantity ? '' : 'var(--color-red)' }">{{
                            scope.row.baseReceiveQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="baseCompleteQuantity" title="已完工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="dispatchUnitInfo.baseUnitName" title="单位" align="left" width="80">
                    </GridColumn>
                    <!-- 主单位 -->
                    <GridColumn field="mainQuantity" title="派工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="mainReceiveQuantity" title="已接单数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span
                          :style="{ color: scope.row.receiveQuantity <= scope.row.dispatchQuantity ? '' : 'var(--color-red)' }">{{
                            scope.row.mainReceiveQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="mainCompleteQuantity" title="已完工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="dispatchUnitInfo.mainUnitName" title="单位" align="left" width="80">
                    </GridColumn>
                  </GridHeaderRow>
                </GridColumnGroup>
                <GridColumnGroup :frozen="true" align="right">
                  <GridHeaderRow>
                    <GridColumn title="操作" align="center" width="230">
                      <template #body="scope">
                        <ButtonGroup style="height: 24px">
                          <LinkButton @click="showMaterialDemand(scope.row)">领料需求
                          </LinkButton>
                          <LinkButton v-if="seletedTab == CustomDispatchOrderStatus.进行中"
                            @click="resetProduction(scope.row, CustomDispatchOrderStatus.已暂停)">暂停
                          </LinkButton>
                          <LinkButton
                            v-if="seletedTab == CustomDispatchOrderStatus.进行中 && scope.row.completeQuantity > 0"
                            @click="resetProduction(scope.row, CustomDispatchOrderStatus.已完成)">确认完成</LinkButton>
                          <LinkButton
                            v-if="seletedTab != CustomDispatchOrderStatus.待开始 && seletedTab != CustomDispatchOrderStatus.进行中"
                            @click="resetProduction(scope.row, CustomDispatchOrderStatus.进行中)">恢复生产</LinkButton>
                          <LinkButton
                            v-if="seletedTab != CustomDispatchOrderStatus.已作废 && scope.row.receiveQuantity > 0"
                            @click="resetProduction(scope.row, undefined)">作废
                          </LinkButton>
                          <LinkButton
                            v-if="seletedTab != CustomDispatchOrderStatus.已完成 && scope.row.receiveQuantity <= 0"
                            @click="deleteOrder(scope.row)">删除</LinkButton>
                        </ButtonGroup>
                      </template>
                    </GridColumn>
                  </GridHeaderRow>
                </GridColumnGroup>
              </KingZonDataGrid>
              <!-- 已完成 -->
              <KingZonDataGrid :id="`dispatchOrderOverDataGrid_${index}`" class="kingzon-page-fill"
                :ref="'dataGrid' + item.value" v-else :data="orderList[item.value]" :showDownLoad="true"
                :exportFileName="'生产派工单'" @pageChange="getDataAndRefresh" @sortChange="getDataAndRefresh"
                :virtualScroll="true" :virtualPageSize="20" :showFooter="true" :footerData="footerData"
                :sorts="[{ field: 'dispatchDate', order: 'desc' }]">
                <GridColumnGroup :frozen="true" width="560" align="left">
                  <GridHeaderRow>
                    <GridColumn align="center" width="40">
                      <template #body="scope">
                        {{ scope.rowIndex + 1 }}
                      </template>
                    </GridColumn>
                    <GridColumn field="orderNo" title="派工单号" width="130" align="left" :sortable="true"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="technicalCode" title="技术型号" align="left" width="100"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productNo" title="产品编码" width="100" align="left" :sortable="true"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productName" title="产品名称" width="190" align="left" :overflowTextEllipsis="true">
                    </GridColumn>
                  </GridHeaderRow>
                </GridColumnGroup>
                <GridColumnGroup :columnMoving="true" align="center">
                  <GridHeaderRow>
                    <GridColumn field="specification" :rowspan="2" title="规格型号" align="left" width="120"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productType" title="类型" width="70" :rowspan="2">
                      <template #body="scope">
                        {{ ProductType[scope.row.productType] }}
                      </template>
                    </GridColumn>
                    <GridColumn field="packageWay" :rowspan="2" title="包装方式" align="left" width="80"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productCategory" :rowspan="2" title="产品类别" align="left" width="100"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productRemark" :rowspan="2" title="产品备注" width="120" align="left"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="packageSpecification" title="外包装方案" width="120" :rowspan="2">
                    </GridColumn>
                    <GridColumn field="dispatchDate" title="派工时间" width="120" :sortable="true" :rowspan="2">
                      <template #body="scope">
                        {{ scope.row.dispatchDate ? $utils.formatDateTime(scope.row.dispatchDate, "yyyy-MM-dd") : ''
                        }}
                      </template>
                    </GridColumn>
                    <GridColumn field="planCompleteDate" title="预计完工时间" width="120" :sortable="true" :rowspan="2">
                      <template #body="scope">
                        {{ scope.row.planCompleteDate ? $utils.formatDateTime(scope.row.planCompleteDate,
                          "yyyy-MM-dd")
                          :
                          '' }}
                      </template>
                    </GridColumn>
                    <GridColumn field="completeDate" title="实际完工时间" width="120" :sortable="true" :rowspan="2">
                      <template #body="scope">
                        {{ scope.row.completeDate ? $utils.formatDateTime(scope.row.completeDate, "yyyy-MM-dd") :
                          '' }}
                      </template>
                    </GridColumn>
                    <GridColumn field="departmentName" :rowspan="2" title="派工部门" align="left" width="100"
                      :sortable="true">
                    </GridColumn>
                    <GridColumn :colspan="4" title="生产单位" align="center">
                    </GridColumn>
                    <GridColumn :colspan="4" title="收入计价单位" align="center">
                    </GridColumn>
                    <GridColumn :colspan="4" title="产品单位" align="center">
                    </GridColumn>
                    <GridColumn :colspan="4" title="成品主单位" align="center">
                    </GridColumn>

                    <GridColumn field="price" :rowspan="2" title="加工单价" align="right" width="80" :sortable="true">
                    </GridColumn>
                    <GridColumn field="remark" :rowspan="2" title="备注" align="left" width="150"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="createUserName" :rowspan="2" title="创建人" width="70" align="left"
                      :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="createTime" :rowspan="2" title="创建时间" align="left" width="150" :sortable="true">
                      <template #body="scope">
                        {{ $utils.formatDateTime(scope.row.createTime, 'yyyy-MM-dd') }}
                      </template>
                    </GridColumn>
                  </GridHeaderRow>
                  <GridHeaderRow>
                    <!-- 生产 -->
                    <GridColumn field="dispatchQuantity" title="派工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="receiveQuantity" title="已接单数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span
                          :style="{ color: scope.row.receiveQuantity <= scope.row.dispatchQuantity ? '' : 'var(--color-red)' }"
                          :class="scope.row.receiveQuantity ? 'kingzon-detail-text' : ''"
                          @click="showOperations(scope.row, OperateType.接单)">{{ scope.row.
                            receiveQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="completeQuantity" title="已完工数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span :class="scope.row.completeQuantity ? 'kingzon-detail-text' : ''"
                          @click="showOperations(scope.row, OperateType.出单)">{{ scope.row.
                            completeQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="dispatchUnitInfo.productionUnitName" title="单位" align="left" width="80">
                    </GridColumn>

                    <!-- 计价单位 -->
                    <GridColumn field="incomeQuantity" title="派工数量" align="right" width="90" :sortable="true">
                      <!-- <template #edit="scope">
                      <NumberBox v-model="scope.row.incomeQuantity" @valueChange="onChangeIncomeQuantity(scope.row)"
                        :spinners="true" :min="0" :disabled="disabledInputIncomQuantity(scope.row)" :precision="4">
                      </NumberBox>
                    </template> -->
                    </GridColumn>
                    <GridColumn field="incomeReceiveQuantity" title="已接单数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span
                          :style="{ color: scope.row.receiveQuantity <= scope.row.dispatchQuantity ? '' : 'var(--color-red)' }">{{
                            scope.row.incomeReceiveQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="incomeCompleteQuantity" title="已完工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="dispatchUnitInfo.incomeUnitName" title="单位" align="left" width="80">
                    </GridColumn>

                    <!-- 产品单位 -->
                    <GridColumn field="baseQuantity" title="派工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="baseReceiveQuantity" title="已接单数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span
                          :style="{ color: scope.row.receiveQuantity <= scope.row.dispatchQuantity ? '' : 'var(--color-red)' }">{{
                            scope.row.baseReceiveQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="baseCompleteQuantity" title="已完工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="dispatchUnitInfo.baseUnitName" title="单位" align="left" width="80">
                    </GridColumn>
                    <!-- 主单位 -->
                    <GridColumn field="mainQuantity" title="派工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="mainReceiveQuantity" title="已接单数量" align="right" width="90" :sortable="true">
                      <template #body="scope">
                        <span
                          :style="{ color: scope.row.receiveQuantity <= scope.row.dispatchQuantity ? '' : 'var(--color-red)' }">{{
                            scope.row.mainReceiveQuantity }}</span>
                      </template>
                    </GridColumn>
                    <GridColumn field="mainCompleteQuantity" title="已完工数量" align="right" width="90" :sortable="true">
                    </GridColumn>
                    <GridColumn field="dispatchUnitInfo.mainUnitName" title="单位" align="left" width="80">
                    </GridColumn>
                  </GridHeaderRow>
                </GridColumnGroup>
                <GridColumnGroup :frozen="true" align="right" width="140">
                  <GridHeaderRow>
                    <GridColumn title="操作" align="center" width="140">
                      <template #body="scope">
                        <ButtonGroup style="height: 24px">
                          <LinkButton
                            v-if="seletedTab != CustomDispatchOrderStatus.已作废 && scope.row.receiveQuantity > 0"
                            @click="resetProduction(scope.row, undefined)">作废
                          </LinkButton>
                          <LinkButton @click="resetProduction(scope.row, CustomDispatchOrderStatus.进行中)">恢复生产
                          </LinkButton>
                        </ButtonGroup>
                      </template>
                    </GridColumn>
                  </GridHeaderRow>
                </GridColumnGroup>
              </KingZonDataGrid>
            </div>
          </div>
        </TabPanel>
      </Tabs>
    </div>
  </div>

  <!-- 选择产品 -->
  <Drawer v-if="isShowSeleteDrawer" @collapse="isShowSeleteDrawer = false;" ref="seleteProduct"
    :title="(selectdDepartment?.text ?? '') + ' 选择产品'" @expand="$utils.updateModal()" :drawerStyle="{ width: '90%' }"
    region="east" :maskClosable="false" :closable="true">
    <SeleteProduct :selectList="selectList" :deptId="selectdDepartment?.value" @close="seleteProductDrawer.collapse()"
      @next="getSeleteList">
    </SeleteProduct>
  </Drawer>

  <!-- 新增 -->
  <Drawer v-if="isShowAddDrawer" @collapse="isShowAddDrawer = false;" ref="addDispatch" title="新增派工单"
    :drawerStyle="{ width: '98%' }" region="east" :maskClosable="false" :closable="true">
    <AddDispatch :selectList="selectList" @close="addDispatchDrawer.collapse()" :selectdDepartment="selectdDepartment"
      @submit="addDispatchDrawer.collapse(); getDataAndRefresh()" @select="seleteOtherProducts">
    </AddDispatch>
  </Drawer>


  <!-- 报工记录详情 -->
  <Dialog ref="detailDialog" :title="'【' + selectRow.orderNo + '】' + OperateType[operateType] + '记录详情'" :modal="true"
    closed :dialogStyle="{ height: '85%', width: '80%' }" :draggable="true">
    <OperationDetail :selectRow="selectRow" :operateType="operateType"></OperationDetail>
  </Dialog>

  <!-- 更新加工单价 -->
  <Dialog ref="modifyPriceDialog" :title="'【' + selectRow.orderNo + '】加工单价详情'" :modal="true" closed
    :dialogStyle="{ height: '85%', width: '80%' }" :draggable="true">
    <ModifyPrice :selectRow="selectRow" :newPrice="newPrice" @close="modifyPriceDialog.close()"
      @submit="modifyPriceDialog.close(); getDataAndRefresh();"></ModifyPrice>
  </Dialog>
  <Drawer v-if="isShowMaterialDemandDrawer" @collapse="isShowMaterialDemandDrawer = false;" ref="materialDemandDrawer"
    :title="'【' + selectRow.orderNo + '】领料需求详情'" :drawerStyle="{ width: '90%' }" region="east" :maskClosable="false"
    :closable="true">
    <div class="kingzon-page-content">
      <MaterialDemandDetail ref="materialDemandDetailComponent" :selectRow="selectRow"
        @close="($refs.materialDemandDrawer as EasyUI.Drawer).collapse()"></MaterialDemandDetail>
    </div>
  </Drawer>

</template>

<script lang="ts">
import { FileStorage, type ProductIdDto, type SystemParaTreeDto } from '@/types/common';
import { CategoryType, ComputedType, DataType, DispatchOrderStatus, FilterType, OperateType, ProductType, UnitConversionType, SystemParaType, FileStorageContentType } from '@/types/enums';
import { GetProductListByProductionDto, type GetDispatchOrderDto } from '@/types/production';
import { Filter, Search, type FilterTmp, type SelectOption } from '@/types/types';
import { defineComponent } from 'vue';
import SeleteProduct from './selete-product.vue';
import AddDispatch from './add-dispatch.vue';
import OperationDetail from './operation-detail.vue';
import ModifyPrice from './modify-price.vue';
import MaterialDemandDetail from './material-demand-detail.vue';
import { cloneDeep } from 'lodash';
enum CustomDispatchOrderStatus {
  待开始 = 0,
  进行中 = 1,
  已暂停 = 2,
  已完成 = 3,
  已作废 = 4,
}
export default defineComponent({
  data() {
    return {
      isLoading: false,
      searchParams: [] as Search[],
      searchParamsHidden: [] as Search[],
      filterList: [] as Filter[],
      orderList: {} as { [key: string]: GetDispatchOrderDto[] },
      isShowAddDrawer: false,
      isShowSeleteDrawer: false,
      tabList: [] as SelectOption[],
      seletedTab: CustomDispatchOrderStatus.进行中,
      departmentList: [] as any[],
      totalDepartments: [] as SelectOption[],
      unitList: [] as ProductIdDto[],
      selectList: [] as GetProductListByProductionDto[],
      seleteProductDrawer: {} as EasyUI.Drawer,
      addDispatchDrawer: {} as EasyUI.Drawer,
      categoryOptions: [] as SelectOption[],
      unitConversitions: [] as any[],
      selectRow: {} as GetDispatchOrderDto,
      detailDialog: {} as EasyUI.Dialog,
      modifyPriceDialog: {} as EasyUI.Dialog,
      newPrice: 0 as number,
      packageInfos: [] as ProductIdDto[],
      operateType: OperateType.接单,
      selectdDepartment: undefined as SelectOption | undefined,
      isShowMaterialDemandDrawer: false,
      materialDemandDrawer: {} as EasyUI.Drawer,
      productionParaList: [] as Array<SystemParaTreeDto>,//扩展参数
      saleFilterList: [] as Filter[],
      customerList: [] as SelectOption[]
    };
  },
  components: {
    SeleteProduct, AddDispatch, OperationDetail, ModifyPrice, MaterialDemandDetail
  },
  computed: {
    CustomDispatchOrderStatus() {
      return CustomDispatchOrderStatus;
    },

    ProductType() {
      return ProductType;
    },
    OperateType() {
      return OperateType;
    },
    footerData() {
      if (this.orderList[this.seletedTab]) {
        return [{
          index: '合计',
          dispatchQuantity: this.orderList[this.seletedTab].reduce((sum, row) => sum + (row.dispatchQuantity ?? 0), 0).toFixed(4),
          completeQuantity: this.orderList[this.seletedTab].reduce((sum, row) => sum + (row.completeQuantity ?? 0), 0).toFixed(4),
          baseQuantity: this.orderList[this.seletedTab].reduce((sum, row) => sum + (row.baseQuantity ?? 0), 0).toFixed(4),
          baseCompleteQuantity: this.orderList[this.seletedTab].reduce((sum, row) => sum + (row.baseCompleteQuantity ?? 0), 0).toFixed(4),
          incomeQuantity: this.orderList[this.seletedTab].reduce((sum, row) => sum + (row.incomeQuantity ?? 0), 0).toFixed(4),
          incomeCompleteQuantity: this.orderList[this.seletedTab].reduce((sum, row) => sum + (row.incomeCompleteQuantity ?? 0), 0).toFixed(4),
        }]
      }
      return [{
        index: '合计'
      }]
    },
  },
  mounted() {
    this.init();
    this.detailDialog = this.$refs.detailDialog as EasyUI.Dialog;
    this.modifyPriceDialog = this.$refs.modifyPriceDialog as EasyUI.Dialog;
    this.materialDemandDrawer = this.$refs.materialDemandDrawer as EasyUI.Drawer;
  },
  created() {
    this.tabList = this.$utils.getEnumKeyValueArray(CustomDispatchOrderStatus);
  },
  methods: {

    init() {
      this.isLoading = true;
      this.getTotalDepartmentList()
        .then((res: any) => {
          this.totalDepartments = res.data.filter(x => x.computedType == ComputedType.包装统计 || x.computedType == ComputedType.生产统计)
            .map(s => {
              return {
                text: s.name,
                value: s.id
              }
            }) as SelectOption[];
          return this.getProductCategoryList();
        })
        .then((res: any) => {
          this.categoryOptions = res.data.map((item: any) => {
            return { value: item.id, text: item.name } as SelectOption;
          });
          return this.getCustomers();
        }).then((res: any) => {
          this.customerList = res.data?.map((item: any) => {
            return { value: item.id, text: item.customerCode + '-' + item.customerName }
          }) as SelectOption[];
          this.customerList.unshift({ value: '', text: '全部' });
          this.addSearchParam();

          return this.getExtendParamData()
        }).then((res) => {
          this.productionParaList = res.data as Array<SystemParaTreeDto>;

          this.getDataAndRefresh();
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
          this.isLoading = false;
        })
    },


    getDepartmentList() {
      var products = this.orderList[this.seletedTab].map(x => {
        return {
          productId: x.productId,
          specificationId: x.specificationId,
        }
      })
      return this.$http.post('/api/production/DispatchOrderManage/GetDepartmentsByPrcessPrice', products);
    },


    getTotalUnitList() {
      var productIdList = this.orderList[this.seletedTab].map(x => {
        return {
          idField: (x.productId + (x.specificationId ? ('_' + x.specificationId) : '')).toString(),
          productId: x.productId,
          specificationId: x.specificationId,
        }
      });
      return this.$http.post('/api/sale/SaleOrderManage/GetProductUnitList', productIdList)
    },


    getProductUnits(row) {
      var that = this;
      var idField = (row.productId + (row.specificationId ? ('_' + row.specificationId) : '')).toString();
      var list = that.unitList.find(x => x.idField == idField);
      var options = list ? (list.unitList ?? []).map(x => {
        return {
          value: Number(x.code),
          text: x.text + (x.extension ? '【' + x.extension + '】' : ''),
          code: x.packageId,
          extension: x.unitId.toString(),
          isMain: x.isMainUnit
        }
      }) as SelectOption[] : [];
      if (!options.find(x => x.value == row.dispatchUnitInfo.unitConversionId) && options.length > 0) {
        var info = {
          value: row.dispatchUnitInfo.unitConversionId,
          text: row.dispatchUnitInfo.productionUnitName,
          code: undefined,
          extension: row.dispatchUnitInfo.productionUnitId.toString()
        } as SelectOption;
        options.push(info);
      }
      return options;
    },

    getPackageList(row) {
      var that = this;
      var idField = (row.productId + (row.specificationId ? ('_' + row.specificationId) : '') + (row.goodsId ? ('_' + row.goodsId) : '')).toString();
      var list = that.packageInfos.find(x => x.idField == idField) as ProductIdDto;
      var options = list ? (list.packageList ?? []).map(x => {
        return {
          value: x.id,
          text: x.packageSpecification,
          code: x.packageUnitId
        }
      }) as SelectOption[] : [];
      if (options.length <= 0 && row.packageId) {
        options.push({
          value: row.packageId,
          text: row.packageSpecification
        })
      }
      return options;
    },

    getDepartments(row) {
      var that = this;
      var list = row ? that.departmentList.filter(x => x.productId == row.productId && x.specificationId == row.specificationId) : that.departmentList;
      var departments = [...new Map(list.map(item => [item.value, item])).values()];
      if (!departments.find(x => x.value == row.departmentId) && departments.length > 0) {
        var info = {
          id: row.processPriceId,
          value: row.departmentId,
          text: row.departmentName,
          productId: row.productId,
          specificationId: row.specificationId,
          price: row.price,
          unitId: row.dispatchUnitInfo.incomeUnitId,
          unitName: row.dispatchUnitInfo.incomeUnitName,
        };
        departments.push(info);
      }
      return departments;
    },


    getTotalDepartmentList() {
      return this.$http.get('/api/base/Department/GetAllDepartment');
    },

    getProductCategoryList() {
      return this.$http.post('/api/base/Category/GetChildrenCategoryListByType', [CategoryType.产品类别]);
    },


    getUnitConversition() {
      var postData = {
        productIds: this.orderList[this.seletedTab].map(x => x.productId),
        specificationIds: this.orderList[this.seletedTab].filter(x => x.specificationId).map(x => x.specificationId),
        processPriceIds: this.departmentList.map(x => x.id),
      };
      return this.$http.post('/api/sale/SaleMonthPlan/GetUnitConversionListByTargetIds', postData);
    },

    getProductPackageList() {
      var productIdList = this.orderList[this.seletedTab].map(x => {
        return {
          idField: (x.productId + (x.specificationId ? ('_' + x.specificationId) : '')).toString(),
          productId: x.productId,
          specificationId: x.specificationId,
          isFilterGoods: false,
        }
      });
      return this.$http.post('/api/sale/SaleOrderManage/GetProductPackageList', productIdList)
    },

    getData() {
      this.filterList = this.filterList.filter(x => x.key != 'isDeleted' && x.key != 'status' && x.key != 'isStart' && x.key != 'departmentId');
      this.filterList.push(new Filter('isDeleted', (this.seletedTab == CustomDispatchOrderStatus.已作废 as CustomDispatchOrderStatus) ? true : false, FilterType.Equals, DataType.SelectBoolean));
      if (this.seletedTab == CustomDispatchOrderStatus.待开始 || this.seletedTab == CustomDispatchOrderStatus.进行中) {
        this.filterList.push(new Filter('isStart', this.seletedTab == CustomDispatchOrderStatus.进行中 ? true : false, FilterType.Equals, DataType.SelectBoolean));
        this.filterList.push(new Filter('status', DispatchOrderStatus.进行中, FilterType.Equals, DataType.SelectNum));
      }
      if (this.seletedTab != CustomDispatchOrderStatus.已作废 && this.seletedTab != CustomDispatchOrderStatus.待开始) {
        this.filterList.push(new Filter('status', this.seletedTab, FilterType.Equals, DataType.SelectNum));
      }
      if (this.selectdDepartment) {
        this.filterList.push(new Filter('departmentId', this.selectdDepartment.value, FilterType.Equals, DataType.SelectNum));
      }
      return this.$http.post('/api/production/DispatchOrderManage/GetDispatchOrderList',
        {
          filterList: this.filterList,
          saleFilterList: this.saleFilterList
        }, this.$refs['dataGrid' + this.seletedTab]);
    },

    getDataAndRefresh() {
      this.isLoading = true;
      this.getData()
        .then(res => {
          this.orderList[this.seletedTab] = res.data as GetDispatchOrderDto[];
          this.orderList[this.seletedTab].forEach(x => {
            x.dispatchDate = new Date(x.dispatchDate);
            x.planCompleteDate = x.planCompleteDate ? new Date(x.planCompleteDate) : undefined;
            x.completeDate = x.completeDate ? new Date(x.completeDate) : undefined;
          })
          return this.getTotalUnitList()
        })
        .then((res) => {
          this.unitList = res.data as ProductIdDto[];
          return this.getUnitConversition()
        })
        .then(res => {
          this.unitConversitions = res.data as any;
          return this.getDepartmentList();
        })
        .then((res) => {
          this.departmentList = res.data as any;
          return this.getProductPackageList();
        })
        .then((res) => {
          this.packageInfos = res.data as ProductIdDto[];
          this.orderList[this.seletedTab].forEach((row: any) => {
            row.unitList = this.getProductUnits(row);
            row.pacakgeList = this.getPackageList(row);
            row.departmentList = this.getDepartments(row);
          })
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
        })
        .finally(() => {
          this.isLoading = false;
        })
    },

    tabSelect(tab) {
      this.seletedTab = CustomDispatchOrderStatus[tab.title as string] as CustomDispatchOrderStatus;
      this.getDataAndRefresh();
    },

    //查询面板
    addSearchParam(): void {
      this.searchParams.push(new Search('orderNo', '派工单号'));
      this.searchParams.push(new Search('technicalCode', '技术型号'));
      this.searchParams.push(new Search('productNo', '产品编码'));
      this.searchParams.push(new Search('productName', '产品名称'));
      this.searchParamsHidden.push(new Search('specification', '规格型号'));
      var productTypeList = this.$utils.getEnumKeyValueArray(ProductType);
      this.searchParamsHidden.push(new Search('productType', '类型', 'default', FilterType.Equals, DataType.SelectNum, "", productTypeList));
      this.searchParamsHidden.push(new Search('saleOrder.orderNo', '销售订单', 'sale'));
      this.searchParamsHidden.push(new Search('saleOrder.customerId', '销售客户', 'sale', FilterType.Equals, DataType.SelectNum, "", this.customerList, undefined, "300px"));
      this.searchParamsHidden.push(new Search('packageWay', '包装方式'));
      this.searchParamsHidden.push(new Search('productCategoryId', '产品类别', 'default', FilterType.IntArrayContains, DataType.multiSelectNum, "", this.categoryOptions));
      this.searchParamsHidden.push(new Search('packageSpecification', '外包装方案'));
      this.searchParamsHidden.push(new Search('dispatchDate', "派工时间", "default", FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'));
      this.searchParamsHidden.push(new Search('dispatchDate', "", "default", FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'));
      this.searchParamsHidden.push(new Search('planCompleteDate', "预计完工时间", "default", FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'));
      this.searchParamsHidden.push(new Search('planCompleteDate', "", "default", FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'));
      //this.searchParamsHidden.push(new Search('departmentId', '派工部门', 'default', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, this.totalDepartments));
      this.searchParamsHidden.push(new Search('remark', '备注'));
      this.searchParamsHidden.push(new Search('createUserName', '创建人'));
      this.searchParamsHidden.push(new Search('createTime', "创建时间", "default", FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'));
      this.searchParamsHidden.push(new Search('createTime', "", "default", FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'));
    },

    //查询方法
    search(params: FilterTmp[]) {
      this.filterList = params.find((item) => item.table == 'default')?.filter ?? [] //单表查询
      this.saleFilterList = params.find((item) => item.table == 'sale')?.filter ?? [] //销售订单查询
      this.getDataAndRefresh();
    },


    showSeleteDrawer() {
      this.selectList = [];
      this.isShowSeleteDrawer = true;
      this.$nextTick(() => {
        this.seleteProductDrawer = this.$refs.seleteProduct as EasyUI.Drawer;
        this.seleteProductDrawer.expand();
      })
    },

    getSeleteList(list) {
      //新增新记录
      list.forEach(e => {
        var info = this.selectList.find(x => x.id == e.id);
        if (!info) {
          e.dispatchUnitInfo = {
            baseUnitId: e.productUnitId,
            baseUnitName: e.productUnit,
            baseUnitConversionId: -1,
            mainUnitId: e.productMainUnitId,
            mainUnitName: e.productMainUnit,
            mainUnitConversionId: 0,
          };
          e.dispatchDate = new Date();
          e.unitList = [];
          e.pacakgeList = [];
          e.departmentList = [];
          this.selectList.push(e);
        }
      });

      //删除已取消选择的记录
      this.selectList.forEach(e => {
        var info = list.find(x => x.id == e.id);
        if (!info) {
          this.selectList = this.selectList.filter(x => x.id != e.id);
        }
      });
      //this.selectList = cloneDeep(postData);
      this.seleteProductDrawer.collapse();
      this.isShowAddDrawer = true;
      this.$nextTick(() => {
        this.addDispatchDrawer = this.$refs.addDispatch as EasyUI.Drawer;
        this.addDispatchDrawer.expand();
      })
    },


    //删除
    deleteOrder(row: GetDispatchOrderDto) {
      this.$kingzon_messager.showConfim("是否确认删除派工单" + row.orderNo)
        .then(() => {
          this.isLoading = true;
          this.$http.get("/api/production/DispatchOrderManage/DeleteDispatchOrder", { id: row.id })
            .then((res: any) => {
              this.$kingzon_messager.showInfo(res.data);
              this.getDataAndRefresh();
            })
            .catch((err) => {
              this.$kingzon_messager.showError(err);
              this.isLoading = false;
            })
        })
    },


    //设置派工单状态
    resetProduction(row, status?: CustomDispatchOrderStatus) {
      var str = status == CustomDispatchOrderStatus.已暂停 ? "是否确认暂停生产派工单" : status == CustomDispatchOrderStatus.进行中 ? "是否确认恢复生产派工单" : !status ? "是否确认作废派工单" : "是否确认完成生产派工单";
      this.$kingzon_messager.showConfim(str + row.orderNo)
        .then(() => {
          this.isLoading = true;
          this.$http.get("/api/production/DispatchOrderManage/SetDispatchOrderStatus", { id: row.id, status: status })
            .then((res: any) => {
              this.$kingzon_messager.showInfo(res.data);
              this.getDataAndRefresh();
            })
            .catch((err) => {
              this.$kingzon_messager.showError(err);
              this.isLoading = false;
            })
        })
    },


    //修改
    modifyDispatch(row: GetDispatchOrderDto) {
      if (!row.departmentId) {
        this.$kingzon_messager.showError("请输入派工单" + row.orderNo + '的派工部门');
        return this.getDataAndRefresh();
      }
      if (!row.dispatchQuantity) {
        this.$kingzon_messager.showError("请输入派工单" + row.orderNo + '条记录的生产单位的派工数量');
        return this.getDataAndRefresh();
      }
      if (!row.dispatchUnitInfo?.productionUnitId) {
        this.$kingzon_messager.showError("请选择派工单" + row.orderNo + '的生产单位');
        return this.getDataAndRefresh();
      }
      if (row.dispatchQuantity < row.completeQuantity) {
        this.$kingzon_messager.showError("派工单" + row.orderNo + '的派工数量不可小于已完工数量');
        return this.getDataAndRefresh();
      }
      var postData = {
        id: row.id,
        productId: row.productId,
        specificationId: row.specificationId,
        dispatchDate: row.dispatchDate,
        departmentId: row.departmentId,
        dispatchQuantity: row.dispatchQuantity,
        baseQuantity: row.baseQuantity,
        incomeQuantity: row.incomeQuantity,
        dispatchUnit: JSON.stringify(row.dispatchUnitInfo),
        dispatchUnitInfo: row.dispatchUnitInfo,
        orderRemark: row.remark,
        processPriceId: row.processPriceId,
        planCompleteDate: row.planCompleteDate,
        mainQuantity: row.mainQuantity,
        packageId: row.packageId,
        productionAttributeList: row.productionAttributeList
      };
      this.isLoading = true;
      this.$http.post("/api/production/DispatchOrderManage/ModifyDispatchOrder", postData)
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          this.getDataAndRefresh();
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
          this.getDataAndRefresh();
        })
    },

    getBaseRatio(x, toUnitId, fromUnitId, id) {
      if (id < 0 || !id) {
        return 1;
      }
      var baseToMain = this.unitConversitions.find(u => u.sourceId == x.specificationId && u.conversionType == UnitConversionType.SpecToProduct);
      var baseToMainRatio = baseToMain ? baseToMain.ratio : 1;
      var ratioInfos = x.specificationId ? this.unitConversitions.filter(u => (u.targetId == x.specificationId &&
        (u.conversionType == UnitConversionType.ProductSpec ||
          u.conversionType == UnitConversionType.PackageToProductSpec) ||
        (u.targetId == x.productId && u.conversionType == UnitConversionType.PackageToProduct)) ||
        (u.sourceId == x.specificationId && u.conversionType == UnitConversionType.SpecToProduct)) :
        this.unitConversitions.filter(u => u.targetId == x.productId && (u.conversionType == UnitConversionType.Product ||
          u.conversionType == UnitConversionType.PackageToProduct));

      var mainConv = ratioInfos.find(r => r.toUnitId == toUnitId && r.fromUnitId == fromUnitId && r.id == id);

      // if (mainConv && mainConv.conversionType == UnitConversionType.SpecToProduct) {
      //   return mainConv.ratio ? 1 / mainConv.ratio : 0;
      // }
      //生产单位为成品包装单位
      if (mainConv && x.specificationId && mainConv.conversionType == UnitConversionType.PackageToProduct) {
        return baseToMainRatio ? mainConv.ratio / baseToMainRatio : 0;
      }
      if (mainConv) {
        return mainConv.ratio;
      }
      //生产单位为成品主单位
      var revConv = ratioInfos.find(r => r.toUnitId == fromUnitId && r.fromUnitId == toUnitId && r.id == id);
      if (revConv && revConv.conversionType == UnitConversionType.SpecToProduct) {
        return revConv.ratio ? 1 / revConv.ratio : 0;
      }
      return 0;
    },

    getIncomeRatio(x) {
      if (!x.dispatchUnitInfo.incomeUnitId) {
        return 0;
      }
      if (x.dispatchUnitInfo.incomeUnitConversionId == x.dispatchUnitInfo.unitConversionId) {
        return 1;
      }
      var incomeToBaseRatio = this.getBaseRatio(x, x.dispatchUnitInfo.baseUnitId, x.dispatchUnitInfo.incomeUnitId, x.dispatchUnitInfo.incomeUnitConversionId);
      return incomeToBaseRatio ? (x.dispatchUnitInfo.baseRatio ?? 0) / incomeToBaseRatio : 0;
    },

    //更新部门
    onChangeDepartment(row) {
      var event = this.getDepartments(row).find(x => x.value == row.departmentId);
      row.price = event ? event.price : 0;
      row.dispatchUnitInfo.incomeUnitId = event ? event.unitId : undefined;
      row.dispatchUnitInfo.incomeUnitName = event ? event.unitName : undefined;
      row.processPriceId = event ? event.id : undefined;
      row.dispatchUnitInfo.incomeUnitConversionId = event ? event.priceUnitConversionId : undefined;
      row.dispatchUnitInfo.incomeRatio = this.getIncomeRatio(row);
      row.incomeQuantity = Number(((row.dispatchUnitInfo.incomeRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      this.modifyDispatch(row);
    },


    //生产单位派工数量
    onChangeDispatchQuantity(row) {
      row.incomeQuantity = Number(((row.dispatchUnitInfo.incomeRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      row.baseQuantity = Number(((row.dispatchUnitInfo.baseRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      row.mainQuantity = Number(((row.dispatchUnitInfo.mainRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      this.modifyDispatch(row);
    },


    //更新生产单位
    onChangeProductUnit(row) {
      var event = this.getProductUnits(row).find(x => x.value == row.dispatchUnitInfo.unitConversionId) as any;
      row.dispatchUnitInfo.productionUnitId = event ? Number(event.extension) : undefined;
      row.dispatchUnitInfo.productionUnitName = event ? event.text : undefined;

      if (!event) {
        row.dispatchUnitInfo.incomeRatio = 0;
        row.dispatchUnitInfo.baseRatio = 0;
        row.dispatchUnitInfo.mainRatio = 0;
      }

      var baseToMain = this.unitConversitions.find(u => u.sourceId == row.specificationId && u.conversionType == UnitConversionType.SpecToProduct);
      var baseToMainRatio = baseToMain ? baseToMain.ratio : 1;
      row.dispatchUnitInfo.baseRatio = this.getBaseRatio(row, row.dispatchUnitInfo.baseUnitId, row.dispatchUnitInfo.productionUnitId, row.dispatchUnitInfo.unitConversionId);
      row.dispatchUnitInfo.incomeRatio = this.getIncomeRatio(row);
      row.dispatchUnitInfo.mainRatio = row.dispatchUnitInfo.baseRatio * baseToMainRatio;

      row.incomeQuantity = Number(((row.dispatchUnitInfo.incomeRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      row.baseQuantity = Number(((row.dispatchUnitInfo.baseRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      row.mainQuantity = Number(((row.dispatchUnitInfo.mainRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      this.$nextTick(() => {
        this.modifyDispatch(row);
      })
    },


    //更新包装规格 =》更新生产单位
    onChangePackage(row) {
      var event = this.getPackageList(row).find(x => x.value == row.packageId);
      if (!event) {
        return;
      }
      var unit = this.getProductUnits(row).find(x => x.code == event?.value && Number(x.extension) == Number(event?.code));
      row.dispatchUnitInfo.unitConversionId = unit ? unit.value : undefined;
      this.onChangeProductUnit(row);
    },


    seleteOtherProducts(list) {
      this.selectList = cloneDeep(list);
      this.isShowSeleteDrawer = true;
      this.$nextTick(() => {
        this.seleteProductDrawer = this.$refs.seleteProduct as EasyUI.Drawer;
        this.seleteProductDrawer.expand();
      })
    },



    showOperations(row, operateType) {
      if (row.completeQuantity <= 0) {
        return this.$kingzon_messager.showInfo("暂无完工记录");
      }
      this.operateType = operateType;
      this.selectRow = cloneDeep(row);
      this.detailDialog.open();
    },


    showModifyPrice(row) {
      this.selectRow = cloneDeep(row);
      var info = this.getDepartments(row).find(x => x.value == row.departmentId);
      this.newPrice = info ? info.price : 0;
      this.modifyPriceDialog.open();
    },



    getRowCss(row) {
      if (this.seletedTab == CustomDispatchOrderStatus.进行中 && row.planCompleteDate && row.planCompleteDate < new Date()) {
        return {
          'background-color': 'var(--color-red-border)'
        }
      }
      return '';
    },


    selecteDepartment(department?: SelectOption) {
      this.selectdDepartment = department;
      this.getDataAndRefresh();
    },

    showMaterialDemand(row) {
      this.selectRow = cloneDeep(row);
      this.isShowMaterialDemandDrawer = true;
      this.$nextTick(() => {
        this.materialDemandDrawer = this.$refs.materialDemandDrawer as EasyUI.Drawer;
        this.materialDemandDrawer.expand();
      });
    },
    getExtendParamData() {
      return this.$http.post('/api/base/SystemPara/GetAdditionalParamByTypes', [SystemParaType.生产物料信息]);
    },
    getCustomers() {
      return this.$http.get('api/base/Customer/GetSalesManAllCustomer', { includeMain: false })
    },

    preViewPdf(perview: any, id: number) {
      this.isLoading = true
      this.$http
        .getFile('/api/base/Product/PreviewPackagePdf', {
          packegeId: id
        }).then((res) => {
          this.isLoading = false
          var file = new FileStorage()
          file.tmpUrl = res.url
          file.contentType = FileStorageContentType.pdf
          perview.preview(file)
        }).catch((err) => {
          // this.$kingzon_messager.showError(err);
          this.$kingzon_messager.showError("打印失败，请联系管理员");
        }).finally(() => {
          this.isLoading = false
        })
    }
  },
});
</script>

<style scoped>
.department-info:hover {
  background-color: var(--color-blue-bg);
}

.selected-department {
  background-color: var(--color-blue-bg);
  color: var(--color-blue);
}
</style>
