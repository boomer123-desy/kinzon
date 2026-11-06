using KingZon.Core.DataBase;
using KingZon.Core.Infrastructure;
using KingZon.Mes.Workflow.Entity;
using KingZon.Mes.Workflow.Enum;
using KingZon.Mes.Workflow.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace KingZon.Mes.Workflow.Service
{
    public class OnFlowIsEndArg
    {
        public WF_Workflow Workflow { get; set; }
    }

    [Service]
    internal class NodeService : CommonService<WF_Node>
    {
        private IRepository<WF_Node> _nodeRepository;
        private IRepository<WF_NodeType> _nodeTypeRepository;
        private IRepository<WF_NodeTemplate> _nodeTemplateRepository;
        private IRepository<WF_Operation> _operatorRepository;
        private IUnitOfWork _unitWork;

        public NodeService(
            IRepository<WF_Node> nodeRepository, 
            IRepository<WF_NodeType> nodeTypeRepository, 
            IRepository<WF_NodeTemplate> nodeTemplateRepository, 
            IRepository<WF_Operation> operatorRepository, 
            IUnitOfWork unitWork)
            :base(nodeRepository)
        {
            _nodeRepository = nodeRepository;
            _nodeTypeRepository = nodeTypeRepository;
            _nodeTemplateRepository = nodeTemplateRepository;
            _operatorRepository = operatorRepository;
            _unitWork = unitWork;
        }

        public IQueryable<WF_Node> GetAll()
        {
            return _nodeRepository.Table;
        }

        public event EventHandler<OnFlowIsEndArg> OnFlowIsEnd;

        public NodeComplexModel GetAllNodesByWorkflowId(long workflowID)
        {
            var entities = _nodeRepository.Table.Where(o => o.Workflow.Id == workflowID);
            var lines = new List<LineOM>();
            foreach (var node in entities)
            {
                //if (node.Next.Count > 0)
                if (node.NextLines.Count > 0)
                {
                    //lines.AddRange(node.Next.Select(n => new LineOM()
                    lines.AddRange(node.NextLines.Select(n => new LineOM()
                    {
                        Id = "line_" + n.Next.Id + "_" + node.Id,
                        From = node.Id,
                        To = n.Next.Id
                    }));
                }
            }
            return new NodeComplexModel()
            {
                Nodes = entities.Select(n => new NodeOM()
                {
                    Id = n.Id,
                    Name = n.Name,
                    Type = n.Type,
                    Left = n.Left,
                    Top = n.Top,
                    Width = n.Width,
                    Height = n.Height,
                    DataId = n.DataId,
                    State = n.State,
                    IsRedo = n.IsRedo
                }).ToList(),
                Lines = lines
            };
        }

        /// <summary>
        /// 获取单个流程的节点排序
        /// </summary>
        /// <param name="worktemplateId"></param>
        /// <returns></returns>
        public Dictionary<long, long> GetNodeSortByWorkFlowId(long workFlowId)
        {
            var node = _nodeRepository.Table.Where(o => o.Workflow.Id == workFlowId);
            Dictionary<long, long> keyValues = new Dictionary<long, long>();
            var start = node.FirstOrDefault(o => o.Type == "start");
            this.GetSort(start, keyValues, 0);
            return keyValues;
            // return _nodeTemplateRepository.Table.Where(n => n.WorkflowTemplate.Id == worktemplateId && n.Type != "start" && n.Type != "end");
        }

        public void Delete(WF_Node node)
        {
            _nodeRepository.Delete(node);
        }

        private void GetSort(WF_Node node, Dictionary<long, long> keyValues, int sort)
        {
            if (node != null)
            {
                var thisSort = sort + 1;
                if (!keyValues.ContainsKey(node.Id))
                {
                    keyValues.Add(node.Id, thisSort);
                }
                if (node.NextLines.Any())
                {
                    foreach (var item in node.NextLines)
                    {
                        GetSort(item.Next, keyValues, thisSort);
                    }
                }
            }
        }

        public void GetPrevNodeByNodeId()
        {

        }

        public NodeComplexModel GetNodesByWorkflowId(long workflowID)
        {
            var entities = _nodeRepository.Table.Where(o => o.Workflow.Id == workflowID);
            var lines = new List<LineOM>();
            foreach (var node in entities)
            {
                //if (node.Next.Count > 0)
                //{
                //    lines.AddRange(node.Next.Select(n => new LineOM()
                //    {
                //        Id = "line_" + n.Id + "_" + node.Id,
                //        From = node.Id,
                //        To = n.Id,
                //        source = node.Id.ToString(),
                //        target = n.Id.ToString()
                //    }));
                //}
                if (node.NextLines != null && node.NextLines.Count > 0)
                {
                    lines.AddRange(node.NextLines.Select(n => new LineOM()
                    {
                        //Id = "line_" + n.Id + "_" + nodeTemplate.Id,
                        //From = nodeTemplate.Id,
                        //To = n.Id,
                        //source = nodeTemplate.Id.ToString(),
                        //target = n.Id.ToString()
                        Id = "line_" + n.NextId + "_" + n.PreviousId,
                        From = n.PreviousId,
                        To = n.NextId,
                        source = n.PreviousId.ToString(),
                        target = n.NextId.ToString()
                    }));
                }
            }
            return new NodeComplexModel()
            {
                Nodes = entities.Select(n => new NodeOM()
                {
                    Id = n.Id,
                    Name = n.Name,
                    Type = n.Type,
                    Left = n.Left,
                    Top = n.Top,
                    Width = n.Width,
                    Height = n.Height,
                    DataId = n.DataId,
                    State = n.State,
                    IsRedo = n.IsRedo,
                    NodeTemplateId = (int)n.NodeTemplateId
                }).ToList(),
                Lines = lines
            };
        }

        public WF_Node GetNodeByID(long nodeID)
        {
            return _nodeRepository.GetById(nodeID);
        }


        public List<WF_Node> GetNextNodes(long currentNodeId)
        {
            var currentNode = _nodeRepository.GetById(currentNodeId);
            //return currentNode.Next.ToList();
            return currentNode.NextLines.Select(x => x.Next).ToList();
        }

        public List<WF_Node> GetNextNodes(List<long> currentNodeId)
        {
            var currentNode = _nodeRepository.Table.Where(o => currentNodeId.Any(c => c == o.Id));
            //return currentNode.SelectMany(o => o.Next)).ToList();
            return currentNode.SelectMany(o => o.NextLines.Select(y => y.Next)).ToList();
        }

        public long GetWorkFlowID(long NodeID)
        {
            var node = _nodeRepository.GetById(NodeID);
            if (node != null)
            {
                return node.Workflow.Id;
            }
            return 0;
        }

        public bool IsWorkFlowFinished(long workflowId)
        {
            var isWorkflowFinished = false;
            var endNode = _nodeRepository.Table.FirstOrDefault(n => n.Type == "end" && n.Workflow.Id == workflowId);
            //if (endNode != null && !endNode.Previous.Any(n => n.State != (int)NodeState.已处理))
            if (endNode != null && !endNode.PreviousLines.Any(n => n.Previous.State != NodeState.已处理))
            {
                isWorkflowFinished = true;
                endNode.State = NodeState.已处理;
                _unitWork.SaveChange();
            }
            return isWorkflowFinished;
        }

        public void UpdateCurrentNode(long nodeId, NodeState targetState, long userId = 0)
        {
            var currentNode = _nodeRepository.GetById(nodeId);
            if (currentNode != null)
            {
                currentNode.State = targetState;
                //如果节点已结束，则记录最后一位操作者
                if (targetState == NodeState.已处理)
                {
                    currentNode.HandlerId = userId;
                    currentNode.HandleDate = DateTime.Now;
                }
                _unitWork.SaveChange();
            }
        }

        public void UpdateNextNodes(long currentNodeId)
        {
            var currentNode = _nodeRepository.GetById(currentNodeId);
            if (currentNode != null)
            {
                //foreach (var nextNode in currentNode.Next)
                foreach (var nextNode in currentNode.NextLines.Select(x => x.Next))
                {
                    if (nextNode.Type != "end")
                    {
                        nextNode.State = NodeState.处理中;
                    }
                    else
                    {
                        this.OnFlowIsEnd?.Invoke(this, new OnFlowIsEndArg() { Workflow = currentNode.Workflow });
                    }
                }
                _unitWork.SaveChange();
            }
        }



        public long InsertNodeOperator(long nodeId, long userId)
        {
            var result = 0L;
            var node = _nodeRepository.GetById(nodeId);
            if (node != null)
            {
                var entity = new WF_Operation()
                {
                    UserId = userId,
                    Node = node,
                    OperateTime = DateTime.Now
                };
                _operatorRepository.Insert(entity);
                _unitWork.SaveChange();
                result = entity.Id;
            }
            return result;
        }

        public NodeComplexModel GetAllPreviousNodesByCurrentNodeId(long nodeId)
        {
            var currentNode = _nodeRepository.GetById(nodeId);
            if (currentNode != null)
            {
                //获取所有之前的节点
                var previousNodeList = new List<WF_Node>();
                //var tempList = currentNode.Previous.AsEnumerable();
                var tempList = currentNode.PreviousLines.Select(x => x.Previous).AsEnumerable();
                while (tempList.Count() > 0)
                {
                    previousNodeList = previousNodeList.Concat(tempList).ToList();
                    //tempList = tempList.SelectMany(t => t.Previous);
                }
                previousNodeList = previousNodeList.Distinct().ToList();
                var lines = new List<LineOM>();
                foreach (var node in previousNodeList)
                {
                    //if (node.Previous.Count > 0)
                    //{
                    //    lines.AddRange(node.Previous.Select(n => new LineOM()
                    //    {
                    //        Id = "line_" + n.Id + "_" + node.Id,
                    //        From = n.Id,
                    //        To = node.Id
                    //    }));
                    //}
                    if (node.PreviousLines.Count > 0)
                    {
                        lines.AddRange(node.PreviousLines.Select(n => new LineOM()
                        {
                            Id = "line_" + n.PreviousId + "_" + node.Id,
                            From = n.PreviousId,
                            To = node.Id
                        }));
                    }
                }
                return new NodeComplexModel()
                {
                    Nodes = previousNodeList.Select(n => new NodeOM()
                    {
                        Id = n.Id,
                        Name = n.Name,
                        Type = n.Type,
                        Left = n.Left,
                        Top = n.Top,
                        Width = n.Width,
                        Height = n.Height,
                        DataId = n.DataId,
                        State = n.State
                    }).ToList(),
                    Lines = lines
                };
            }
            return new NodeComplexModel();
        }

        public void UpdateRefusedNodes(List<long> nodeIds)
        {
            var targetNodes = _nodeRepository.Table.Where(n => nodeIds.Contains(n.Id));
            foreach (var node in targetNodes)
            {
                node.State = NodeState.处理中;
                node.IsRedo = true;
            }
            _unitWork.SaveChange();
        }

        public List<NodeOM> GetPreviousNodesByCurrentNodeId(long currentId)
        {
            var currentNode = _nodeRepository.GetById(currentId);
            if (currentNode != null)
            {
                //return currentNode.Previous.Where(n => n.Type != "start").Select(a => new NodeOM()
                //{
                //    Id = a.Id,
                //    Handler = a.Handler,
                //    //NodeTypeID = a.NodeType.Id,
                //    //OperatorID = a.OperatorID,
                //    //OperatorType = a.OperatorType,
                //    //Order = a.Order,
                //    Remark = a.Remark,
                //    State = a.State,
                //    Name = a.Name,
                //    HandlerDate = a.HandleDate
                //}).ToList();
                return currentNode.PreviousLines.Where(n => n.Previous.Type != "start").Select(a => new NodeOM()
                {
                    Id = a.Previous.Id,
                    Handler = a.Previous.HandlerId,
                    //NodeTypeID = a.NodeType.Id,
                    //OperatorID = a.OperatorID,
                    //OperatorType = a.OperatorType,
                    //Order = a.Order,
                    Remark = a.Previous.Remark,
                    State = a.Previous.State,
                    Name = a.Previous.Name,
                    HandlerDate = a.Previous.HandleDate
                }).ToList();
            }
            return new List<NodeOM>();
        }

        public IQueryable<WF_Node> GetPreviousNodesByCurrentNodeId(List<long> currentIds)
        {
            //var dd = _nodeRepository.Table.Where(o => currentIds.Any(c => c == o.Id)).ToList();
            //var Nodes = _nodeRepository.Table.Where(o => currentIds.Any(c => c == o.Id) && o.Previous.Any(n => n.Type != "start")).SelectMany(o => o.Previous);//.GetById(currentId);
            var Nodes = _nodeRepository.Table.Where(o => currentIds.Any(c => c == o.Id) && o.PreviousLines.Any(n => n.Previous.Type != "start")).SelectMany(o => o.PreviousLines).Select(y => y.Previous);
            return Nodes;
        }

        public bool IsFirstNode(long nodeId)
        {
            var node = _nodeRepository.GetById(nodeId);
            if (node != null)
            {
                //return node.Previous.Count == 1 && node.Previous.FirstOrDefault().Type == "start";
                return node.PreviousLines.Count == 1 && node.PreviousLines.FirstOrDefault().Previous.Type == "start";
            }
            return false;
        }

        public List<NodeOM> GetLastNodesByWorkflowId(long workflowId)
        {
            var lastNode = _nodeRepository.Table.FirstOrDefault(n => n.Workflow.Id == workflowId && n.Type == "end");
            if (lastNode != null)
            {
                //return lastNode.Previous.Where(n => n.Type != "start").Select(a => new NodeOM()
                //{
                //    Id = a.Id,
                //    Handler = a.Handler,
                //    //NodeTypeID = a.NodeType.Id,
                //    //OperatorID = a.OperatorID,
                //    //OperatorType = a.OperatorType,
                //    //Order = a.Order,
                //    Remark = a.Remark,
                //    State = a.State,
                //    Name = a.Name,
                //    HandlerDate = a.HandleDate
                //}).ToList();
                return lastNode.PreviousLines.Where(n => n.Previous.Type != "start").Select(a => new NodeOM()
                {
                    Id = a.Previous.Id,
                    Handler = a.Previous.HandlerId,
                    //NodeTypeID = a.NodeType.Id,
                    //OperatorID = a.OperatorID,
                    //OperatorType = a.OperatorType,
                    //Order = a.Order,
                    Remark = a.Previous.Remark,
                    State = a.Previous.State,
                    Name = a.Previous.Name,
                    HandlerDate = a.Previous.HandleDate
                }).ToList();
            }
            return new List<NodeOM>();
        }

        public IEnumerable<NodeModel> GetCurrentNodesByNodeIds(IEnumerable<long> nodeIds)
        {
            var nodeIdsList = nodeIds.ToList();
            var currentNodes = _nodeRepository.Table.AsNoTracking()
                .Where(o => nodeIdsList.Contains(o.Id) && o.State == NodeState.处理中);

            //A second operation started on this context before a previous operation completed
            return currentNodes.Select(n => new NodeModel
            {
                NodeID = n.Id,
                DataId = n.DataId,
                Name = n.Name,
                State = n.State,
                IsRedo = n.IsRedo
                //NodeTypeName = currentNodeType.Name,
                //Area = currentNodeType.Area,
                //Controller = currentNodeType.Controller,
                //Action = currentNodeType.Action
            }).ToList();
        }

        public NodeModel GetNodeByDataId(long workflowId, long dataId)
        {
            var entity = _nodeRepository.Table.FirstOrDefault(n => n.Workflow.Id == workflowId && n.DataId == dataId);
            if (entity != null)
            {
                return new NodeModel()
                {
                    NodeID = entity.Id,
                    DataId = entity.DataId,
                    Name = entity.Name,
                    State = entity.State,
                    IsRedo = entity.IsRedo
                };
            }
            return null;
        }

        public bool IsLastNode(long nodeId)
        {
            var node = _nodeRepository.GetById(nodeId);
            if (node != null)
            {
                //return node.Next.Count == 1 && node.Next.FirstOrDefault().Type == "end";
                return node.NextLines.Count == 1 && node.NextLines.FirstOrDefault().Next.Type == "end";
            }
            return false;
        }

        public void DeleteOperator(long logId)
        {
            var entity = _operatorRepository.GetById(logId);
            if (entity != null)
            {
                _operatorRepository.Delete(entity);
                _unitWork.SaveChange();
            }
        }

        public IEnumerable<NodeModel> GetAllEndNodesByDataIds(List<long> dataIds)
        {
            var nodes = _nodeRepository.Table
                            .Where(n => n.Type == "end" 
                                && dataIds.Contains(n.Workflow.DataId));
            return nodes.Select(n => new NodeModel
            {
                NodeID = n.Id,
                DataId = n.DataId,
                Name = n.Name,
                State = n.State,
                IsRedo = n.IsRedo,
                Workflow = new WorkflowModel()
                {
                      WorkflowId = n.Workflow.Id,
                      DataId=n.Workflow.DataId
                }
                //NodeTypeName = currentNodeType.Name,
                //Area = currentNodeType.Area,
                //Controller = currentNodeType.Controller,
                //Action = currentNodeType.Action
            }).ToList();
        }

        public long GetStartNodeId(long workflowId)
        {
            var node = _nodeRepository.Table.Where(n => n.Workflow.Id == workflowId && n.Type == "start").FirstOrDefault();
            if (node != null)
            {
                return node.Id;
            }
            return 0;
        }

        public bool IsStartNode(long nodeId)
        {
            var node = _nodeRepository.GetById(nodeId);
            if (node != null)
            {
                return node.Type == "start";
            }
            return false;
        }

        public bool IsFristNode(long nodeId)
        {
            var node = _nodeRepository.GetById(nodeId);
            if (node != null)
            {
                if (node.PreviousLines.Any(o => o.Previous.Type == "start"))
                {
                    return true;
                }
            }
            return false;
        }

        public List<WF_Node> GetAllNextByCurrentNodeId(long nodeId)
        {
            var node = _nodeRepository.GetById(nodeId);
            var result = new List<WF_Node>();
            this.getNextsNode(result, node);
            return result;
        }

        private void getNextsNode(List<WF_Node> result, WF_Node node)
        {

            foreach (var item in node.NextLines)
            {
                if (item.Next.NextLines.Any())
                {
                    this.getPreviousNode(result, item.Next);
                }

                var obj = result.FirstOrDefault(o => o.Id == item.Next.Id);
                if (obj == null)
                    result.Add(item.Next);
            }
        }


        public List<WF_Node> GetAllPreviousByCurrentNodeId(long nodeId)
        {
            var node = _nodeRepository.GetById(nodeId);
            var result = new List<WF_Node>();
            this.getPreviousNode(result, node);
            return result;
        }

        private void getPreviousNode(List<WF_Node> result, WF_Node node)
        {

            foreach (var item in node.PreviousLines)
            {
                if (item.Previous.PreviousLines.Any())
                {
                    this.getPreviousNode(result, item.Previous);
                }

                var obj = result.FirstOrDefault(o => o.Id == item.Previous.Id);
                if (obj == null)
                    result.Add(item.Previous);
            }
        }

        /// <summary>
        /// 查询 操作记录
        /// </summary>
        /// <returns></returns>
        public IQueryable<WF_Operation> GetOperator()
        {
            return _operatorRepository.Table;
        }


        /// <summary>
        /// 查询 操作记录
        /// </summary>
        /// <returns></returns>
        public IQueryable<WF_Operation> GetOperatorByWorkFlowId(long workFlowId)
        {
            return _operatorRepository.Table.Where(o => o.Node.Workflow.Id == workFlowId);
        }

        public IQueryable<WF_Operation> GetOperatorByWorkFlowIds(IEnumerable<long> workFlowId)
        {
            return _operatorRepository.Table.Where(o => workFlowId.Contains(o.Node.Workflow.Id));
        }

        public bool HasOtherNodes(long workflowId)
        {
            return _nodeRepository.Table.Any(n => n.Workflow.Id == workflowId && n.Type != "start" && n.Type != "end");
        }

        public IQueryable<WF_Node> GetAllWorkNodesByWorkflowId(long workflowID)
        {
            return _nodeRepository.Table.Where(n => n.Workflow.Id == workflowID && n.Type != "start" && n.Type != "end");
        }

        public IQueryable<WF_Node> Api_GetAllWorkNodesByWorkflowId(long workflowID)
        {
            return _nodeRepository.Table.Where(n => n.Workflow.Id == workflowID && n.Type != "start" && n.Type != "end");
        }

        public IQueryable<WF_Node> GetPreviousNodesByCurrentNodeIds(IEnumerable<long> currentIds)
        {
            var currentNodes = _nodeRepository.Table.Where(n => currentIds.Contains(n.Id));
            //return currentNodes.SelectMany(c => c.Previous);
            return currentNodes.SelectMany(c => c.PreviousLines).Select(x => x.Previous);
        }
        public IQueryable<WF_Node> GetPreviousAnyStartNodesByCurrentNodeId(long currentId)
        {
            var currentNodes = _nodeRepository.Table.Where(n => currentId == n.Id);
            //return currentNodes.SelectMany(c => c.Previous);
            return currentNodes.SelectMany(c => c.PreviousLines).Select(x => x.Previous);
        }

        public IQueryable<WF_Node> GetAllByNodeIds(IEnumerable<long> nodeIds)
        {
            var currentNodes = _nodeRepository.Table.Where(n => nodeIds.Contains(n.Id));
            return currentNodes;
        }
    }

    
}
