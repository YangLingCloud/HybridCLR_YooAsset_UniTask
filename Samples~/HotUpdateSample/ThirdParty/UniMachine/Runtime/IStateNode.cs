
using Cysharp.Threading.Tasks;

namespace UniFramework.Machine
{
	public interface IStateNode
	{
		UniTaskVoid OnCreate(StateMachine machine);
		
		UniTaskVoid OnEnter();
		UniTaskVoid OnUpdate();
		UniTaskVoid OnExit();
	}
}