using KingZon.Core.DataBase;
using KingZon.Core.Infrastructure;
using KingZon.Mes.Workflow.Entity;
using KingZon.Mes.Workflow.Enum;
using KingZon.Mes.Workflow.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KingZon.Mes.Workflow.Service
{
    [Service]
    public class WorkflowService : CommonService<WF_Workflow>
    {
        private IRepository<WF_Workflow> _workflowRepository;
        private IRepository<WF_NodeTemplate> _nodeTemplateRepository;
        private IRepository<WF_Node> _nodeRepository;
        private IRepository<WF_WorkflowTemplate> _workflowTemplateRepository;
        private IRepository<WF_WorkflowType> _workflowTypeRepository;
        private IRepository<WF_NodeLink> _lineRepository;
        private readonly IRepository<WF_NodePermission> _nodePermissionRepository;
        private readonly IRepository<WF_Operation> _operatorRepository;
        private readonly DbContext _dbContext;
        private IUnitOfWork _unitWork;

        public event EventHandler<Entity.WF_Workflow> OnWorkflowFinished;
        public event EventHandler<WF_Operation> OnNodePassed;
        public event EventHandler<WF_Node> OnNodeReject;

        public WorkflowService(
            IRepository<WF_Workflow> workflowRepository, 
            IRepository<WF_NodeTemplate> nodeTemplateRepository, 
            IRepository<WF_Node> nodeRepository, 
            IRepository<WF_WorkflowTemplate> workflowTemplateRepository, 
            IRepository<WF_WorkflowType> workflowTypeRepository, 
            IRepository<WF_NodeLink> lineRepository,
            IRepository<WF_NodePermission> nodePermissionRepository,
            IRepository<WF_Operation> operatorRepository,
            DbContext dbContext,
            IUnitOfWork unitWork)
            :base(workflowRepository)
        {
            _workflowRepository = workflowRepository;
            _nodeTemplateRepository = nodeTemplateRepository;
            _nodeRepository = nodeRepository;
            _workflowTemplateRepository = workflowTemplateRepository;
            _workflowTypeRepository = workflowTypeRepository;
            _lineRepository = lineRepository;
            _nodePermissionRepository = nodePermissionRepository;
            _operatorRepository = operatorRepository;
            _dbContext = dbContext;
            _unitWork = unitWork;
        }

        public void CompleteWorkflow(long workflowId)
        {
            var entity = _workflowRepository.GetById(workflowId);
            entity.State = WorkflowState.已结束;
            if (entity != null)
            {
                var nodes = entity.Nodes.ToList();
                foreach(var node in nodes)
                {
                    if (node.State != NodeState.已处理)
                    {
                        node.State = NodeState.已处理;
                    }
                }
            }
        }

        /// <summary>
        /// 创建工作流
        /// </summary>
        /// <param name="dataId"></param>
        /// <param name="workflowTemplateID"></param>
        /// <param name="note"></param>
        /// <param name="creatorID"></param>
        /// <param name="operatorID"></param>
        /// <returns></returns>
        public Entity.WF_Workflow CreateWorkflow(long dataId, long workflowTemplateID, string note)
        {
            var workflowTemplate = _workflowTemplateRepository.GetById(workflowTemplateID);

            //同一种类型且DataId相同的流，只能存在一个
            var existWorkflow = _workflowRepository.Table
                .FirstOrDefault(o => o.DataId == dataId
                    && o.WorkflowType.Id == workflowTemplate.WorkflowTypeId);

            if (existWorkflow != null)
            {
                //throw new Exception("流程已提交请不要重复添加！");
                return existWorkflow;
            }

            // 创建工作流
            Entity.WF_Workflow workflow = new Entity.WF_Workflow();
            workflow.WorkflowTemplate = workflowTemplate;
            workflow.Note = note;
            workflow.CreateTime = DateTime.Now;
            workflow.DataId = dataId;
            workflow.WorkflowType = workflowTemplate.WorkflowType;
            workflow.State = WorkflowState.新建;
            _workflowRepository.Insert(workflow);

            // 为工作流添加节点
            var nodeTemplates = _nodeTemplateRepository.Table
                .Include(t => t.NextLineTemplates)
                .Include(t => t.PreviousLineTemplates)
                .Include(o=>o.NodePermissionTemplates)
                .Where(o => o.WorkflowTemplate.Id == workflowTemplateID)
                .ToList();

            var nodes = nodeTemplates.Select(n => new WF_Node()
            {
                NodeType = n.NodeType,
                State = NodeState.未处理,
                Workflow = workflow,
                Name = n.Name,
                Type = n.Type,
                Left = n.Left,
                Top = n.Top,
                Width = n.Width,
                Height = n.Height,
                DataId = dataId,
                NodeTemplate = n,
                NodeTemplateId = n.Id,
            }).ToList();

            _nodeRepository.Insert(nodes);

            //判断设置前后节点关系
            List<WF_NodeLink> lines = new List<WF_NodeLink>(); 
            foreach (var node in nodes)
            {
                //获取节点对应的NodeTemplate
                var nodeTemplate = nodeTemplates.FirstOrDefault(n => n.Id == node.NodeTemplateId);

                //获取前置节点Id
                var previousTemplateIds = nodeTemplate.PreviousLineTemplates.Select(p => p.PreviousId);

                //查询出所有的前置节点，并建立与当前节点的关联
                var previousLine = nodes.Where(n => previousTemplateIds.Contains((int)n.NodeTemplateId))
                    .Select(x => new WF_NodeLink
                    {
                        Previous = x,
                        Next = node,
                    }).ToList();

                BuildLines(previousLine, lines);

                //获取所有后续节点Id
                var nextTemplateIds = nodeTemplate.NextLineTemplates.Select(n => n.NextId);
                //查询出对应的后续节点，并建立与当前节点的关联
                var nextLine = nodes.Where(n => nextTemplateIds.Contains((int)n.NodeTemplateId))
                    .Select(x => new WF_NodeLink
                    {
                        Previous = node,
                        Next = x,
                    }).ToList();
                BuildLines(nextLine, lines);

                //复制节点权限
                foreach(var permission in nodeTemplate.NodePermissionTemplates)
                {
                    var tmp = new WF_NodePermission()
                    {
                        Node = node,
                        PermissionType = permission.PermissionType,
                        PermissionId = permission.PermissionId
                    };
                    _nodePermissionRepository.Insert(tmp);
                }
            }
           
            _lineRepository.Insert(lines);

            return workflow;
        }
        /// <summary>按排产编辑后的线性路线创建实例；保留传入节点的派生类型和模板权限。</summary>
        public WF_Workflow CreateWorkflow(long dataId, long workflowTemplateId, string note, IReadOnlyList<WF_Node> route,
            IReadOnlyDictionary<WF_Node, long>? permissionTemplateIds = null)
        {
            if (route == null || route.Count == 0) throw new Exception("排产路线不能为空");
            var template = _workflowTemplateRepository.GetById(workflowTemplateId)
                ?? throw new Exception("工作流模板不存在");
            if (route.Any(x => x.NodeType == null || x.NodeType.WorkflowTypeId != template.WorkflowTypeId))
                throw new Exception("排产节点类型与工艺模板的工作流类型不一致，请检查 WF_NodeType 配置");
            if (_workflowRepository.Table.Any(x => x.DataId == dataId && x.WorkflowTypeId == template.WorkflowTypeId))
                throw new Exception("该排产单已创建工作流，请勿重复提交");
            var templates = _nodeTemplateRepository.Table.Include(x => x.NodeType)
                .Include(x => x.NodePermissionTemplates).Where(x => x.WorkflowTemplateId == workflowTemplateId).ToList();
            var starts = templates.Where(x => x.NodeType.Type == NodeType.start).ToList();
            var ends = templates.Where(x => x.NodeType.Type == NodeType.end).ToList();
            if (starts.Count != 1 || ends.Count != 1) throw new Exception("工艺模板必须配置唯一的开始和结束节点");
            var workflow = new WF_Workflow
            {
                DataId = dataId, WorkflowTemplateId = template.Id, WorkflowTypeId = template.WorkflowTypeId,
                Note = note, CreateTime = DateTime.Now, State = WorkflowState.新建,
                Nodes = new List<WF_Node>()
            };
            WF_Node Boundary(WF_NodeTemplate source) => new WF_Node
            {
                Name = source.Name, Type = source.Type, NodeType = source.NodeType,
                NodeTypeId = source.NodeTypeId, NodeTemplateId = source.Id
            };
            var nodes = new[] { Boundary(starts[0]) }.Concat(route).Append(Boundary(ends[0])).ToList();
            foreach (var node in nodes)
            {
                node.Workflow = workflow;
                node.DataId = dataId;
                node.State = NodeState.未处理;
                node.PreviousLines = new List<WF_NodeLink>();
                node.NextLines = new List<WF_NodeLink>();
                workflow.Nodes.Add(node);
            }
            var lines = new List<WF_NodeLink>();
            for (var index = 1; index < nodes.Count; index++)
            {
                var line = new WF_NodeLink { Previous = nodes[index - 1], Next = nodes[index], Workflow = workflow };
                line.Previous.NextLines.Add(line);
                line.Next.PreviousLines.Add(line);
                lines.Add(line);
            }
            _workflowRepository.Insert(workflow);
            _nodeRepository.Insert(nodes);
            _lineRepository.Insert(lines);
            foreach (var node in nodes)
            {
                var permissionTemplateId = permissionTemplateIds != null && permissionTemplateIds.TryGetValue(node, out var sourceId)
                    ? sourceId : node.NodeTemplateId;
                var source = templates.FirstOrDefault(x => x.Id == permissionTemplateId);
                if (source == null) continue;
                foreach (var permission in source.NodePermissionTemplates)
                    _nodePermissionRepository.Insert(new WF_NodePermission
                    {
                        Node = node, PermissionType = permission.PermissionType, PermissionId = permission.PermissionId
                    });
            }
            return workflow;
        }

        public void DeleteWorkflow(Workflow.Entity.WF_Workflow workflow)
        {
            _workflowRepository.Delete(workflow);
        }
        private void BuildLines(List<WF_NodeLink> sourceLines, List<WF_NodeLink> destLines)
        {
            sourceLines.ForEach(x =>
            {
                if (!destLines.Exists(y => y.Next.NodeTemplateId == x.Next.NodeTemplateId
                                        && y.Previous.NodeTemplateId == x.Previous.NodeTemplateId))
                    destLines.Add(x);
            });
        }
        /// <summary>
        /// 通过数据Id获取对应的工作流
        /// </summary>
        /// <param name="dataId"></param>
        /// <returns></returns>
        public Entity.WF_Workflow GetWorkflowByDataId(long dataId)
        {
            var entity = _workflowRepository.Table.FirstOrDefault(w => w.DataId == dataId);
            if (entity != null)
            {
                return entity;
            }
            return null;
        }
        /// <summary>
        /// 启动工作流
        /// </summary>
        /// <param name="workflow"></param>
        public void StartWorkflow(Entity.WF_Workflow workflow)
        {
            workflow.State = WorkflowState.进行中;
            var startNode = workflow.Nodes.First(o => o.NodeType.Type == NodeType.start);
            startNode.State = NodeState.已处理;
            foreach (var item in startNode.NextLines)
            {
                this.SetNodeStart(item.Next);
            }
        }

        /// <summary>
        /// 设置节点通过
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="handlerId"></param>
        public void SetNodePass(long nodeId, long handlerId)
        {
            //根据节点Id找到对应的节点
            var node = _nodeRepository.Table
                .Include(o=>o.NextLines)
                .ThenInclude(o=>o.Next)
                .ThenInclude(o=>o.NodeType)
                .Include(o=>o.Workflow)
                .First(o => o.Id == nodeId);
            //更新节点的状态为已处理
            node.HandlerId = handlerId;
            node.HandleDate = DateTime.Now;
            node.State =NodeState.已处理;

            //新建处理记录
            var op = new WF_Operation()
            {
                Node = node,
                OperateTime = DateTime.Now,
                UserId = handlerId,
                Type = OperatorType.通过
            };
            _operatorRepository.Insert(op);

            //设置后续节点为开始状态
            foreach(var nextLine in node.NextLines)
            {
                this.SetNodeStart(nextLine.Next);
            }
            //触发节点通过事件
            this.OnNodePassed?.Invoke(this, op);
        }

        public void SetNodeStart(WF_Node node)
        {
            node.State = NodeState.处理中;

            //如果节点是结束节点，则需要改变流程状态
            if (node.NodeType.Type == NodeType.end)
            {
                //如果结束节点的前置节点都通过，则结束节点通过，流程完成
                if(node.PreviousLines.All(o=>o.Previous.State == NodeState.已处理))
                {
                    node.State = NodeState.已处理;
                    node.Workflow.State = WorkflowState.已结束;
                    //触发流程结束事件
                    OnWorkflowFinished?.Invoke(this, node.Workflow);
                }
            }
        }

        /// <summary>
        /// 设置节点驳回
        /// </summary>
        /// <param name="nodeId">节点Id</param>
        /// <param name="handlerId">用户Id</param>
        /// <param name="handlerName">用户姓名</param>
        /// <param name="rejectReason">驳回理由</param>
        /// <returns></returns>
        public void SetNodeReject(long nodeId, long handlerId, string rejectReason)
        {
            //找到对应的节点
            var node = _nodeRepository.Table.First(o => o.Id == nodeId);

            //更改节点状态
            node.HandlerId = handlerId;
            node.HandleDate = DateTime.Now;
            node.State =NodeState.驳回;

            //新建处理记录
            var op = new WF_Operation()
            {
                Node = node,
                OperateTime = DateTime.Now,
                UserId = handlerId,
                Type = OperatorType.驳回,
                RejectReason = rejectReason
            };
            _operatorRepository.Insert(op);

            //修改流程状态
            node.Workflow.State = WorkflowState.驳回;

            //将开始节点置为处理中
            var startNode = node.Workflow.Nodes.First(o => o.NodeType.Type ==  NodeType.start);
            startNode.State = NodeState.处理中;

            //触发节点驳回事件
            this.OnNodeReject?.Invoke(this, node);
        }

        public class PermissionDto
        {
            /// <summary>
            /// 权限类型（用户、角色）
            /// </summary>
            public PermissionType PermissionType { get; set; }
            /// <summary>
            /// 对应的记录Id(用户Id、角色Id)
            /// </summary>
            public long PermissionId { get; set; }
        }

        /// <summary>
        /// 根据用户或角色，获取所有需要处理的节点
        /// </summary>
        /// <param name="handlerId"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public IQueryable<WF_Node> GetNodesToDeal(long handlerId,List<PermissionDto> permissions)
        {
            if (permissions.Count == 0)
            {
                throw new Exception("需传入需要查询的权限信息");
            }

            var q = from node in _nodeRepository.Table
                    where node.NodeType.Type == NodeType.node
                          && node.State == NodeState.处理中
                    select node;
            
            if (permissions.Count > 0)
            {
                q = q.Where(o => !o.HandlerId.HasValue 
                                    || (o.HandlerId.HasValue && o.HandlerId == handlerId));

                string sql = "select * from NodePermission ";
                for (int i = 0; i < permissions.Count; i++)
                {
                    var item = permissions[i];
                    if (i == 0)
                    {
                        sql += " where ";
                    }
                    else
                    {
                        sql += " or ";
                    }

                    sql += $" (PermissionType={(int)item.PermissionType} and PermissionId={item.PermissionId}) ";
                }

                var q1 = _dbContext.Database.SqlQueryRaw<WF_NodePermission>(sql);

                q = from node in q
                    join o in q1 on node.Id equals o.NodeId
                    select node;
            }
            else
            {
                q = q.Where(node => node.HandlerId.HasValue && node.HandlerId == handlerId);
            }

            return q;
        }
    }
}
