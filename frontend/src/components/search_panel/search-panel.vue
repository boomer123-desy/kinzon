<template>
  <div>
    <div class="kingzon-page-panel">
      <div style="display:flex;align-items:center;margin-right: 0;">
        <slot name="leftCustom"></slot>
      </div>
      <div v-for="item in searchData" :key="item.key" :style="{ display: 'flex', width: item.width }">
        <Label v-if="item.label" :for="item.key" align="left">{{ item.label + ':' }}</Label>
        <TextBox v-if="item.dataType === DataType.Text" :inputId="item.key" v-model="item.value"
          :placeholder="item.label" style="flex:1;"></TextBox>
        <NumberBox v-if="item.dataType === DataType.Integer" :inputId="item.key" v-model="item.value" :spinners="true"
          style="flex:1;">
        </NumberBox>
        <NumberBox v-if="item.dataType === DataType.Decimal" :inputId="item.key" v-model="item.value" :spinners="true"
          :precision="3" style="flex:1;"></NumberBox>
        <ComboBox v-if="item.dataType === DataType.SelectNum" :inputId="item.key" v-model="item.value"
          :data="item.optionList" :total="item.optionList?.length ?? 0" style="flex:1;">
        </ComboBox>
        <ComboBox v-if="item.dataType === DataType.SelectText" :inputId="item.key" v-model="item.value"
          :data="item.optionList" :total="item.optionList?.length ?? 0" style="flex:1;">
        </ComboBox>
        <ComboBox v-if="item.dataType === DataType.SelectBoolean" :inputId="item.key" v-model="item.value"
          :data="item.optionList" style="flex:1;"></ComboBox>
        <DateBox v-if="item.dataType === DataType.DateTime" :inputId="item.key" v-model="item.value" format="yyyy-MM-dd"
          :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
          :placeholder="item.placeholder" style="flex:1;"></DateBox>
        <ComboBox v-if="item.dataType === DataType.multiSelectNum" :inputId="item.key" v-model="item.value"
          :data="item.optionList" :multiple="true" :virtualScroll="true" :total="item.optionList?.length ?? 0"
          :pageSize="pageSize" :rowHeight="rowHeight" :panelStyle="{ height: '200px !important' }" style="flex:1;">
        </ComboBox>
      </div>
      <div style="display:flex;align-items:center">
        <slot name="customer"></slot>
      </div>
      <div class="kingzon-btn-group" style="margin-left: 50px;">
        <LinkButton btnCls="c1" @click="search">查询</LinkButton>
        <LinkButton @click="reset" v-show="showResetButton">重置</LinkButton>
        <LinkButton iconCls="icon-search" style="height:100%;min-width:unset;width:auto" @click="exactQuery"
          v-show="showIcon">
        </LinkButton>
      </div>
    </div>
    <Transition :name="transitionName" @before-enter="collapseBeforeEnter" @enter="collapseEnter"
      @after-enter="collapseAfterEnter" @before-leave="collapseBeforeLeave" @leave="collapseLeave"
      @after-leave="collapseAfterLeave">
      <div class="kingzon-page-panel" v-show="showAccordion" style="background-color: var(--color-grey-bg)">
        <div v-for="item in searchDataHidden" :key="item.key" :style="{ display: 'flex', width: item.width }">
          <Label v-if="item.label" :for="item.key" align="left">{{ item.label + ':' }}</Label>
          <TextBox v-if="item.dataType == 0" :inputId="item.key" v-model="item.value" :placeholder="item.label"
            style="flex:1;">
          </TextBox>
          <NumberBox v-if="item.dataType === DataType.Integer" :inputId="item.key" v-model="item.value" :spinners="true"
            style="flex:1;">
          </NumberBox>
          <NumberBox v-if="item.dataType === DataType.Decimal" :inputId="item.key" v-model="item.value" :spinners="true"
            :precision="3" style="flex:1;"></NumberBox>
          <ComboBox v-if="item.dataType === DataType.SelectNum" :inputId="item.key" v-model="item.value"
            :data="item.optionList" :total="item.optionList?.length ?? 0" style="flex:1;">
          </ComboBox>
          <ComboBox v-if="item.dataType === DataType.SelectText" :inputId="item.key" v-model="item.value"
            :data="item.optionList" :total="item.optionList?.length ?? 0" style="flex:1;">
          </ComboBox>
          <ComboBox v-if="item.dataType === DataType.SelectBoolean" :inputId="item.key" v-model="item.value"
            :data="item.optionList" style="flex:1;"></ComboBox>
          <DateBox v-if="item.dataType === DataType.DateTime" :inputId="item.key" v-model="item.value"
            format="yyyy-MM-dd"
            :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
            :placeholder="item.placeholder" style="flex:1;"></DateBox>
          <ComboBox v-if="item.dataType === DataType.multiSelectNum" :inputId="item.key" v-model="item.value"
            :data="item.optionList" :multiple="true" :virtualScroll="true" :total="item.optionList?.length ?? 0"
            :pageSize="pageSize" :rowHeight="rowHeight" :panelStyle="{ height: '200px !important' }" style="flex:1;">
          </ComboBox>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { type FilterTmp, Search, Filter, CustomAttribute } from '@/types/types';
import { cloneDeep } from 'lodash';
import { DataType, FilterType } from '@/types/enums'
/**
 * 元素折叠过度效果
 */
export default defineComponent({
  name: 'CollapseTransition',
  emits: ['search', 'reset'],
  props: {
    transitionName: {
      type: String,
      default: 'collapse-transition'
    },
    searchParams: {
      type: Array<Search>,
      default: [] as Search[],
      required: false,
    },
    searchParamsHidden: {
      type: Array<Search>,
      default: [] as Search[],
      required: false,
    },
    otherParams: {
      type: Array<Search>,
      default: [] as Search[],
      required: false,
    },
    showIcon: {
      type: Boolean,
      default: true,
      required: false,
    },

    showResetButton: {
      type: Boolean,
      default: true,
      required: false,
    }

  },
  watch: {
    searchParams: {
      handler(val) {
        this.searchData = cloneDeep(val);
      },
      deep: true
    },
    searchParamsHidden: {
      handler(val) {
        this.searchDataHidden = cloneDeep(val);
      },
      deep: true
    },
    otherParams: {
      handler(val) {
        this.otherData = cloneDeep(val);
      },
      deep: true
    }
  },
  computed: {
    DataType() {
      return DataType;
    }
  },
  data() {
    return {
      oldPaddingTop: '',
      oldPaddingBottom: '',
      oldOverflow: '',
      showAccordion: false,
      searchData: cloneDeep(this.searchParams),
      // searchData: this.searchParams,
      // searchData:[...this.searchParams],
      // searchData:JSON.parse(JSON.stringify(this.searchParams)),
      searchDataHidden: cloneDeep(this.searchParamsHidden),
      otherData: cloneDeep(this.otherParams),
      pageSize: 20,
      rowHeight: 30,
    }
  },
  created() {
  },
  mounted() {
  },
  methods: {
    collapseBeforeEnter(el: Element) {
      // console.log('11, collapseBeforeEnter');
      const element = el as HTMLElement;
      this.oldPaddingBottom = element.style.paddingBottom;
      this.oldPaddingTop = element.style.paddingTop;
      // 过渡效果开始前设置元素的maxHeight为0，让元素maxHeight有一个初始值
      element.style.paddingTop = '0';
      element.style.paddingBottom = '0';
      element.style.maxHeight = '0';
    },
    collapseEnter(el: Element, done: () => void) {
      // console.log('22, collapseEnter');
      const element = el as HTMLElement;
      this.oldOverflow = element.style.overflow;
      let elHeight = element.scrollHeight;
      // 过渡效果进入后将元素的maxHeight设置为元素本身的高度，将元素maxHeight设置为auto不会有过渡效果
      if (elHeight > 0) {
        element.style.maxHeight = elHeight + 'px';
      } else {
        element.style.maxHeight = '0';
      }
      element.style.paddingTop = this.oldPaddingTop;
      element.style.paddingBottom = this.oldPaddingBottom;

      element.style.overflow = 'hidden';

      let onTransitionDone = function () {
        done();
        el.removeEventListener('transitionend', onTransitionDone, false);
        el.removeEventListener('transitioncancel', onTransitionDone, false);
      };
      // 绑定元素的transition完成事件，在transition完成后立即完成vue的过度动效
      el.addEventListener('transitionend', onTransitionDone, false);
      el.addEventListener('transitioncancel', onTransitionDone, false);
    },
    collapseAfterEnter(el: Element) {
      // 过渡效果完成后恢复元素的maxHeight
      const element = el as HTMLElement;
      element.style.maxHeight = '';
      element.style.overflow = this.oldOverflow;
    },

    collapseBeforeLeave(el: Element) {
      const element = el as HTMLElement;
      this.oldPaddingBottom = element.style.paddingBottom;
      this.oldPaddingTop = element.style.paddingTop;
      this.oldOverflow = element.style.overflow;

      element.style.maxHeight = el.scrollHeight + 'px';
      element.style.overflow = 'hidden';
    },
    collapseLeave(el: Element, done: () => void) {
      const element = el as HTMLElement;

      if (element.scrollHeight !== 0) {
        element.style.maxHeight = '0';
        element.style.paddingBottom = '0';
        element.style.paddingTop = '0';
      }
      let onTransitionDone = function () {
        done();
        el.removeEventListener('transitionend', onTransitionDone, false);
        el.removeEventListener('transitioncancel', onTransitionDone, false);
      };
      // 绑定元素的transition完成事件，在transition完成后立即完成vue的过度动效
      el.addEventListener('transitionend', onTransitionDone, false);
      el.addEventListener('transitioncancel', onTransitionDone, false);
    },
    collapseAfterLeave(el: Element) {
      const element = el as HTMLElement;

      element.style.maxHeight = '';
      element.style.overflow = this.oldOverflow;
      element.style.paddingBottom = this.oldPaddingBottom;
      element.style.paddingTop = this.oldPaddingTop;

      this.oldOverflow = this.oldPaddingBottom = this.oldPaddingTop = '';
    },
    //查询
    search() {
      let params = this.handleSearchParams(this.searchData, this.searchDataHidden, this.otherData);
      window.sessionStorage.setItem('searchPanel_searchButtonClick', 'true');
      this.$emit('search', params);
    },
    //处理查询参数
    handleSearchParams(searchParams: Search[], searchParamsHidden: Search[], otherParams: Search[] = []): FilterTmp[] {
      // console.log(otherParams, "otherParams")
      //汇总所有查询条件(value不为空)
      let allFilters: Search[] = searchParams.concat(searchParamsHidden).concat(otherParams)
        .filter(item => (item.value ||
          (item.dataType == DataType.SelectBoolean ? (item.value === true || item.value === false) : false))
          && (item.dataType == DataType.multiSelectNum ? item.value.length : true))
        .map(item => {
          let entity = cloneDeep(item);
          //将value转换为number
          if (entity.dataType === DataType.SelectNum) {
            entity.value = Number(entity.value);
          }
          //将value转换为boolean
          if (entity.dataType === DataType.SelectBoolean) {
            // console.log(entity.value, "entity.value")
            entity.value = entity.value == true;
          }
          if (entity.dataType === DataType.multiSelectNum) {
            entity.value = entity.value.map(item => Number(item));
          }
          return entity;
        })

      //按table分组
      const groupedData = allFilters.reduce((acc: FilterTmp[], item: Search) => {
        let table = item.table;
        let filter = acc.find(item => item.table == table)?.filter ?? [];
        if (filter.length == 0) {
          acc.push({ table: table, filter: filter } as FilterTmp)
        }
        filter.push(new Filter(item.key, item.value, item.filterType, item.dataType));
        return acc;
      }, []);

      //处理附加参数
      groupedData.forEach(item => {
        let additionalParams = item.filter.filter(param => param.key.includes('additional_') == true); //找出每个table的附加参数
        if (additionalParams.length <= 0) {
          return;
        }
        let customAttributeList: CustomAttribute[] = additionalParams.map(param => {
          return new CustomAttribute({ systemParaId: Number(param.key.split('_')[1]), value: param.value });
        }); //处理附加参数
        //附加参数 用于过滤 customAttribute字段
        item.filter.push(new Filter('customAttribute', JSON.stringify(customAttributeList), FilterType.Json, DataType.SelectText));
        //删除原附加参数
        item.filter = item.filter.filter(param => param.key.includes('additional_') == false);
      });
      return groupedData;
    },
    //重置
    reset() {
      this.searchData = cloneDeep(this.searchParams);
      this.searchDataHidden = cloneDeep(this.searchParamsHidden);
      this.$emit('reset');
    },
    // 控制精确查询面板展开/关闭
    exactQuery(): void {
      this.showAccordion = !this.showAccordion;
    },
  }
});
</script>

<style>
.collapse-transition-enter-active,
.collapse-transition-leave-active {
  transition: height .3s ease-in-out, padding .3s ease-in-out, max-height .3s ease-in-out;
}

.scroll-content.f-noshrink>div {
  height: unset !important;
}
</style>
