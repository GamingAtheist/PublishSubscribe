using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Linq;

namespace PubSub
{

	internal class TreeNode
	{
		Dictionary<string, TreeNode> children = new Dictionary<string, TreeNode>();
		List<Delegate> payloads = new List<Delegate>();
		List<Delegate> wildcardListeners = new List<Delegate>();

		public TreeNode GetChild(string key)
		{
			if (!children.ContainsKey(key))
				children.Add(key, new TreeNode());
			return children[key];
		}

		public List<Delegate> GetPayloads()
		{
			return new List<Delegate>(payloads);
		}

		public List<Delegate> GetWildcards()
		{
			return new List<Delegate>(wildcardListeners);
		}

		public void AddPayload<T>(Action<T> callback)
		{
			payloads.Add(callback);
		}

		public void AddWildCard<T>(Action<T> callback)
		{
			wildcardListeners.Add(callback);
		}

	}



	public static class Transmitter
	{
		static TreeNode root = new TreeNode();

		public static void Subscribe<T>(Action<T> callback, params string[] topic)
		{
			foreach (var s in topic)
			{
				if (string.IsNullOrEmpty(s)) throw new ArgumentNullException("topic");
			}

			var curNode = root;

			foreach (var subt in topic)
			{
				if (subt == "*")
				{
					curNode.AddWildCard(callback);
					return;
				}
				else
				{
					curNode = curNode.GetChild(subt);
				}
			}

			curNode.AddPayload(callback);

		}

		public static void Broadcast<T>(T payload, params string[] topic)
		{
			var curNode = root;

			var invokes = new List<Delegate>();

			foreach (var comp in topic)
			{
				foreach (var delegateObj in curNode.GetWildcards())
				{
					var parameters = delegateObj.Method.GetParameters();
					if (parameters.Length > 1) throw new InvalidOperationException("Found non-unary delegate");

					var arg = parameters[0];
					var argType = arg.ParameterType;
					if (typeof(T).IsSubclassOf(argType))
					{
						invokes.Add(delegateObj);
					}
				}
				curNode = curNode.GetChild(comp);
			}

			foreach (var delegateObj in curNode.GetPayloads())
			{
				var parameters = delegateObj.Method.GetParameters();
				if (parameters.Length > 1) throw new InvalidOperationException("Found non-unary delegate");

				var arg = parameters[0];
				var argType = arg.ParameterType;
				if (typeof(T).Equals(argType) ||typeof(T).IsSubclassOf(argType))
				{
					invokes.Add(delegateObj);
				}
			}

			
			invokes.AsParallel().ForAll(delegate(Delegate D) { D.DynamicInvoke(payload); });

		}


	}
}