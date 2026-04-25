
using Cysharp.Threading.Tasks;

namespace UniFramework.Machine
{
	/// <summary>
	/// 状态机节点接口，定义节点创建、进入、更新和退出生命周期。
	/// </summary>
	public interface IStateNode
	{
		/// <summary>
		/// 节点被注册到状态机时调用。
		/// </summary>
		UniTaskVoid OnCreate(StateMachine machine);
		
		/// <summary>
		/// 节点成为当前状态时调用。
		/// </summary>
		UniTaskVoid OnEnter();

		/// <summary>
		/// 状态机每帧更新当前节点时调用。
		/// </summary>
		UniTaskVoid OnUpdate();

		/// <summary>
		/// 节点被切换出去时调用。
		/// </summary>
		UniTaskVoid OnExit();
	}
}
