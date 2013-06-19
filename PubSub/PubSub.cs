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

		/// <summary>
		/// Subscribes a callback to a topic, which will then be called by any Transmit() which specifies
		/// 1) the same topic, 
		/// 2) an object that is of the same class, or derived from the callback's parameter type. 
		/// 
		/// If the subscribed topic name ends in a "*", the callback will also be subscribed to all sub-topics.
		/// Empty or null topic components are forbidden. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="callback"></param>
		/// <param name="topic"></param>
		public static void Subscribe<T>(Action<T> callback, params string[] topic)
		{
			if (callback == null) throw new ArgumentNullException("callback");
			if (topic == null || topic.Length == 0) throw new ArgumentException("topic");

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

		/// <summary>
		/// Broadcasts the given object to all of the callbacks subscribed to the named topic. 
		/// The callbacks will be invoked if they can accept the object without a casting error.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="payload"></param>
		/// <param name="topic"></param>
		public static void Broadcast<T>(T payload, params string[] topic)
		{
			//if (callback == null) throw new ArgumentNullException("callback");
			if (topic == null) throw new ArgumentException("topic");
			foreach (var s in topic)
			{
				if (s == null) throw new ArgumentNullException("topic");
			}

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