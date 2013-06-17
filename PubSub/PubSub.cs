using System;
using System.Collections.Generic;
using System.Text;

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
		TreeNode root = new TreeNode();

		public static void Subscribe<T>(string[] topic, Action<T> callback)
		{
			
		}

		public static void Broadcast<T>(string[] topic, T payload)
		{

		}


	}
}