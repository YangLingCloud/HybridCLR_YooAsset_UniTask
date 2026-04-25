using System;
using System.Collections;
using System.Collections.Generic;

namespace UniFramework.Machine
{
	/// <summary>
	/// 轻量状态机实现，支持节点注册、状态切换和黑板数据共享。
	/// </summary>
	public class StateMachine
	{
		private readonly Dictionary<string, System.Object> _blackboard = new Dictionary<string, object>(100);
		private readonly Dictionary<string, IStateNode> _nodes = new Dictionary<string, IStateNode>(100);
		private IStateNode _curNode;
		private IStateNode _preNode;

		/// <summary>
		/// 状态机持有者
		/// </summary>
		public System.Object Owner { private set; get; }

		/// <summary>
		/// 当前运行的节点名称
		/// </summary>
		public string CurrentNode
		{
			get { return _curNode != null ? _curNode.GetType().FullName : string.Empty; }
		}

		/// <summary>
		/// 之前运行的节点名称
		/// </summary>
		public string PreviousNode
		{
			get { return _preNode != null ? _preNode.GetType().FullName : string.Empty; }
		}


		/// <summary>
		/// 禁止无持有者创建状态机。
		/// </summary>
		private StateMachine() { }

		/// <summary>
		/// 创建状态机并记录持有者对象。
		/// </summary>
		public StateMachine(System.Object owner)
		{
			Owner = owner;
		}

		/// <summary>
		/// 更新状态机
		/// </summary>
		public void Update()
		{
			if (_curNode != null)
				_curNode.OnUpdate();
		}

		/// <summary>
		/// 启动状态机
		/// </summary>
		public void Run<TNode>() where TNode : IStateNode
		{
			var nodeType = typeof(TNode);
			var nodeName = nodeType.FullName;
			Run(nodeName);
		}

		/// <summary>
		/// 以类型作为入口节点启动状态机。
		/// </summary>
		public void Run(Type entryNode)
		{
			var nodeName = entryNode.FullName;
			Run(nodeName);
		}

		/// <summary>
		/// 以节点完整类型名作为入口节点启动状态机。
		/// </summary>
		public void Run(string entryNode)
		{
			_curNode = TryGetNode(entryNode);
			_preNode = _curNode;

			if (_curNode == null)
				throw new Exception($"Not found entry node: {entryNode }");

			_curNode.OnEnter();
		}

		/// <summary>
		/// 加入一个节点
		/// </summary>
		public void AddNode<TNode>() where TNode : IStateNode
		{
			var nodeType = typeof(TNode);
			// 使用无参构造创建节点实例，节点上下文通过 OnCreate 注入。
			var stateNode = Activator.CreateInstance(nodeType) as IStateNode;
			AddNode(stateNode);
		}

		/// <summary>
		/// 向状态机添加已经创建好的节点实例。
		/// </summary>
		public void AddNode(IStateNode stateNode)
		{
			if (stateNode == null)
				throw new ArgumentNullException();

			var nodeType = stateNode.GetType();
			var nodeName = nodeType.FullName;

			if (_nodes.ContainsKey(nodeName) == false)
			{
				// 节点首次注册时调用 OnCreate，让节点保存状态机引用。
				stateNode.OnCreate(this);
				_nodes.Add(nodeName, stateNode);
			}
			else
			{
				UniLogger.Error($"State node already existed : {nodeName}");
			}
		}

		/// <summary>
		/// 转换状态节点
		/// </summary>
		public void ChangeState<TNode>() where TNode : IStateNode
		{
			var nodeType = typeof(TNode);
			var nodeName = nodeType.FullName;
			ChangeState(nodeName);
		}

		/// <summary>
		/// 按节点类型切换当前状态。
		/// </summary>
		public void ChangeState(Type nodeType)
		{
			var nodeName = nodeType.FullName;
			ChangeState(nodeName);
		}

		/// <summary>
		/// 按节点完整类型名切换当前状态。
		/// </summary>
		public void ChangeState(string nodeName)
		{
			if (string.IsNullOrEmpty(nodeName))
				throw new ArgumentNullException();

			IStateNode node = TryGetNode(nodeName);
			if (node == null)
			{
				UniLogger.Error($"Can not found state node : {nodeName}");
				return;
			}

			UniLogger.Log($"{_curNode.GetType().FullName} --> {node.GetType().FullName}");
			_preNode = _curNode;
			// 状态切换顺序固定为旧节点退出、新节点成为当前节点、新节点进入。
			_curNode.OnExit();
			_curNode = node;
			_curNode.OnEnter();
		}

		/// <summary>
		/// 设置黑板数据
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetBlackboardValue(string key, System.Object value)
		{
			if (_blackboard.ContainsKey(key) == false)
			{
				// 新 key 直接加入黑板。
				_blackboard.Add(key, value);
			}
			else
			{
				// 已存在 key 时覆盖旧值，便于状态节点之间传递最新上下文。
				_blackboard[key] = value;
			}
		}

		/// <summary>
		/// 获取黑板数据
		/// </summary>
		public System.Object GetBlackboardValue(string key)
		{
			if (_blackboard.TryGetValue(key, out System.Object value))
			{
				return value;
			}
			else
			{
				UniLogger.Warning($"Not found blackboard value : {key}");
				return null;
			}
		}

		/// <summary>
		/// 按完整类型名查找已注册的状态节点。
		/// </summary>
		private IStateNode TryGetNode(string nodeName)
		{
			_nodes.TryGetValue(nodeName, out IStateNode result);
			return result;
		}
	}
}
