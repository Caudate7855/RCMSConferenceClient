using System.Collections.Generic;
using UnityEngine;
using VIAVR.Scripts.Core.Singleton;

namespace VIAVR.Scripts.Core
{
	public sealed class ObjectPOOL : MonoBehaviour
	{
		public enum StartupPoolMode { Awake, Start, CallManually };

		[System.Serializable]
		public class StartupPool
		{
			public int size;
			public GameObject prefab;
		}

		static List<GameObject> tempList = new List<GameObject>();
	
		Dictionary<GameObject, List<GameObject>> pooledObjects = new Dictionary<GameObject, List<GameObject>>();
		Dictionary<GameObject, GameObject> spawnedObjects = new Dictionary<GameObject, GameObject>();
	
		public StartupPoolMode startupPoolMode;
		public StartupPool[] startupPools;

		bool startupPoolsCreated;
		private static bool shuttingDown;


		private void Awake()
		{
			if (startupPoolMode == StartupPoolMode.Awake)
				CreateStartupPools();
		}

		private void Start()
		{
			if (startupPoolMode == StartupPoolMode.Start)
				CreateStartupPools();
		}

		private void OnApplicationQuit()
		{
			shuttingDown = true;
		}

		public static void CreateStartupPools()
		{
			if ( instance == null )
				return;
        
			if (!instance.startupPoolsCreated)
			{
				instance.startupPoolsCreated = true;
				var pools = instance.startupPools;
				if (pools != null && pools.Length > 0)
					for (int i = 0; i < pools.Length; ++i)
						CreatePool(pools[i].prefab, pools[i].size);
			}
		}

		public static void CreatePool<T>(T prefab, int initialPoolSize) where T : Component
		{
			CreatePool(prefab.gameObject, initialPoolSize);
		}
		public static void CreatePool(GameObject prefab, int initialPoolSize)
		{
			if ( instance == null )
				return;
        
			if (prefab != null && !instance.pooledObjects.ContainsKey(prefab))
			{
				var list = new List<GameObject>(initialPoolSize);
				instance.pooledObjects.Add(prefab, list);

				if (initialPoolSize > 0)
				{
					bool active = prefab.activeSelf;
					prefab.SetActive(false);
					Transform parent = instance.transform;
					while (list.Count < initialPoolSize)
					{
						var obj = (GameObject)Instantiate(prefab);
						obj.transform.SetParent(parent, false);
						list.Add(obj);
					}
					prefab.SetActive(active);
				}
			}
		}


		public static void RelocateInstancesToGlobalTransformIfTheyInThePassedParent(Transform prnt, GameObject prefab, int usedPrefabsCount) {
			usedPrefabsCount = Mathf.Max(usedPrefabsCount, 2);
			List<GameObject> list;
			if (instance.pooledObjects.TryGetValue(prefab, out list)) {
				int currentPrefabInstancesWithPassedTransform = 0;
				for (int i = 0; i < list.Count; i++) {
					var elem = list[i];
					if (elem != null) {
						if (elem.transform.parent == prnt) {
							currentPrefabInstancesWithPassedTransform++;
						}
					}
				}
				//Debug.LogError("currentPrefabInstances: " + currentPrefabInstancesWithPassedTransform);

				int AmountOfInstancesToLeaveInLoaclTransform = currentPrefabInstancesWithPassedTransform;
				while (AmountOfInstancesToLeaveInLoaclTransform > 4 * usedPrefabsCount) {
					AmountOfInstancesToLeaveInLoaclTransform /= 2;
				}

				int AmountToRepositionToGlobalTransorm = currentPrefabInstancesWithPassedTransform - AmountOfInstancesToLeaveInLoaclTransform;
				//Debug.LogError("Reposition: " + AmountToRepositionToGlobalTransorm);
				if (AmountToRepositionToGlobalTransorm > 0) {
					for (int i = 0; i < list.Count; i++) {
						var elem = list[i];
						if (elem != null) {
							if (elem.transform.parent == prnt) {
								elem.transform.parent = instance.transform;
								AmountToRepositionToGlobalTransorm--;
							}
						}

						if (AmountToRepositionToGlobalTransorm <= 0) {
							break;
						}
					}
				}
			}
		}

		public static T Spawn<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
		{
			return Spawn(prefab.gameObject, parent, position, rotation).GetComponent<T>();
		}
		public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
		{
			return Spawn(prefab.gameObject, null, position, rotation).GetComponent<T>();
		}
		public static T Spawn<T>(T prefab, Transform parent, Vector3 position) where T : Component
		{
			return Spawn(prefab.gameObject, parent, position, Quaternion.identity).GetComponent<T>();
		}
		public static T Spawn<T>(T prefab, Vector3 position) where T : Component
		{
			return Spawn(prefab.gameObject, null, position, Quaternion.identity).GetComponent<T>();
		}
		public static T Spawn<T>(T prefab, Transform parent) where T : Component
		{
			return Spawn(prefab.gameObject, parent, Vector3.zero, Quaternion.identity).GetComponent<T>();
		}
		public static T Spawn<T>(T prefab) where T : Component
		{
			return Spawn(prefab.gameObject, null, Vector3.zero, Quaternion.identity).GetComponent<T>();
		}
		public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
		{
			if ( instance == null )
				return null;
        
			List<GameObject> list;
			Transform trans;
			GameObject obj;
			if (instance.pooledObjects.TryGetValue(prefab, out list))
			{
				obj = null;
				if (list.Count > 0)
				{
					while (obj == null && list.Count > 0)
					{
						obj = list[0];
						list.RemoveAt(0);
					}
					if (obj != null)
					{
						trans = obj.transform;
						trans.SetParent(parent, false);
						trans.localPosition = position;
						trans.localRotation = rotation;
						obj.SetActive(true);
						instance.spawnedObjects.Add(obj, prefab);
						return obj;
					}
				}
				obj = (GameObject)Instantiate(prefab);
				trans = obj.transform;
				trans.SetParent(parent, false);
				trans.localPosition = position;
				trans.localRotation = rotation;
				instance.spawnedObjects.Add(obj, prefab);
				return obj;
			}

			obj = (GameObject)Instantiate(prefab);
			trans = obj.GetComponent<Transform>();
			trans.SetParent(parent, false);
			trans.localPosition = position;
			trans.localRotation = rotation;
			return obj;
		}


		public static GameObject MySpawn(GameObject prefab, Transform preferredParent, out bool foundObjWithPrefferedParent) {
			foundObjWithPrefferedParent = false;
			if (instance == null)
				return null;

			List<GameObject> list;
			GameObject obj;
			if (instance.pooledObjects.TryGetValue(prefab, out list)) {
				while (list.Count > 0 && list[0] == null) {
					list.RemoveAt(0);
				}
				obj = null;
				if (list.Count > 0) {
					int objIndex = -1;
					if (list.Count > 0) {
						objIndex = 0;
					}
					for (int i = 0; i < list.Count; i++) {
						var preffredObj = list[i];
						if (preffredObj != null && preffredObj.transform.parent == preferredParent) {
							objIndex = i;
							//Debug.Log("prefferedObj " + objIndex);
							foundObjWithPrefferedParent = true;
							break;
						}
					}

					if (objIndex >= 0) {
						obj = list[objIndex];
						list.RemoveAt(objIndex);
					}

					//if (!foundObjWithPrefferedParent) {
					//    Debug.Log("prefferedObj not found");
					//}

					if (obj != null) {
						obj.SetActive(true);
						instance.spawnedObjects.Add(obj, prefab);
						return obj;
					}
				}
				obj = (GameObject)Instantiate(prefab);
				instance.spawnedObjects.Add(obj, prefab);
				return obj;
			}

			obj = (GameObject)Instantiate(prefab);
			return obj;
		}


		public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position)
		{
			return Spawn(prefab, parent, position, Quaternion.identity);
		}
		public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return Spawn(prefab, null, position, rotation);
		}
		public static GameObject Spawn(GameObject prefab, Transform parent)
		{
			return Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
		}
		public static GameObject Spawn(GameObject prefab, Vector3 position)
		{
			return Spawn(prefab, null, position, Quaternion.identity);
		}
		public static GameObject Spawn(GameObject prefab)
		{
			return Spawn(prefab, null, Vector3.zero, Quaternion.identity);
		}

		public static void Recycle<T>(T obj, bool changeParent) where T : Component
		{
			Recycle(obj.gameObject, changeParent);
		}
		public static void Recycle(GameObject obj, bool changeParent)
		{
			if ( instance == null )
				return;
        
			GameObject prefab;
			if (instance.spawnedObjects.TryGetValue(obj, out prefab))
				Recycle(obj, prefab, changeParent);
			else
				Destroy(obj);
		}
		static void Recycle(GameObject obj, GameObject prefab, bool changeParent)
		{
			if ( instance == null )
				return;
        
			instance.pooledObjects[prefab].Add(obj);
			instance.spawnedObjects.Remove(obj);
			if (changeParent) {
				obj.transform.SetParent(instance.transform, false);
			}
			obj.SetActive(false);
		}

		public static void RecycleAll<T>(T prefab, bool changeParent) where T : Component
		{
			RecycleAll(prefab.gameObject, changeParent);
		}
		public static void RecycleAll(GameObject prefab, bool changeParent)
		{
			if ( instance == null )
				return;
        
			foreach (var item in instance.spawnedObjects)
				if (item.Value == prefab)
					tempList.Add(item.Key);
			for (int i = 0; i < tempList.Count; ++i)
				Recycle(tempList[i], changeParent);
			tempList.Clear();
		}

	
		public static bool IsSpawned(GameObject obj)
		{
			return instance.spawnedObjects.ContainsKey(obj);
		}

		public static int CountPooled<T>(T prefab) where T : Component
		{
			return CountPooled(prefab.gameObject);
		}
		public static int CountPooled(GameObject prefab)
		{
			if ( instance == null )
				return 0;
        
			List<GameObject> list;
			if (instance.pooledObjects.TryGetValue(prefab, out list))
				return list.Count;
			return 0;
		}

		public static int CountSpawned<T>(T prefab) where T : Component
		{
			return CountSpawned(prefab.gameObject);
		}
		public static int CountSpawned(GameObject prefab)
		{
			int count = 0 ;
			foreach (var instancePrefab in instance.spawnedObjects.Values)
				if (prefab == instancePrefab)
					++count;
			return count;
		}

		public static int CountAllPooled()
		{
			int count = 0;
			foreach (var list in instance.pooledObjects.Values)
				count += list.Count;
			return count;
		}

		public static List<GameObject> GetPooled(GameObject prefab, List<GameObject> list, bool appendList)
		{
			if ( instance == null )
				return new List<GameObject>();
        
			if (list == null)
				list = new List<GameObject>();
			if (!appendList)
				list.Clear();
			List<GameObject> pooled;
			if (instance.pooledObjects.TryGetValue(prefab, out pooled))
				list.AddRange(pooled);
			return list;
		}
		public static List<T> GetPooled<T>(T prefab, List<T> list, bool appendList) where T : Component
		{
			if ( instance == null )
				return new List<T>();

			if (list == null)
				list = new List<T>();
			if (!appendList)
				list.Clear();
			List<GameObject> pooled;
			if (instance.pooledObjects.TryGetValue(prefab.gameObject, out pooled))
				for (int i = 0; i < pooled.Count; ++i)
					list.Add(pooled[i].GetComponent<T>());
			return list;
		}

		public static List<GameObject> GetSpawned(GameObject prefab, List<GameObject> list, bool appendList)
		{
			if ( instance == null )
				return new List<GameObject>();
        
			if (list == null)
				list = new List<GameObject>();
			if (!appendList)
				list.Clear();
			foreach (var item in instance.spawnedObjects)
				if (item.Value == prefab)
					list.Add(item.Key);
			return list;
		}
		public static List<T> GetSpawned<T>(T prefab, List<T> list, bool appendList) where T : Component
		{
			if ( instance == null )
				return new List<T>();
        
			if (list == null)
				list = new List<T>();
			if (!appendList)
				list.Clear();
			var prefabObj = prefab.gameObject;
			foreach (var item in instance.spawnedObjects)
				if (item.Value == prefabObj)
					list.Add(item.Key.GetComponent<T>());
			return list;
		}

		public static ObjectPOOL instance
		{
			get
			{
				if ( shuttingDown ) return null;

				return Singleton<ObjectPOOL>.Instance;
			}
		}
	}

	public static class ObjectPoolExtensions
	{
		public static void CreatePool<T>(this T prefab) where T : Component
		{
			ObjectPOOL.CreatePool(prefab, 0);
		}
		public static void CreatePool<T>(this T prefab, int initialPoolSize) where T : Component
		{
			ObjectPOOL.CreatePool(prefab, initialPoolSize);
		}
		public static void CreatePool(this GameObject prefab)
		{
			ObjectPOOL.CreatePool(prefab, 0);
		}
		public static void CreatePool(this GameObject prefab, int initialPoolSize)
		{
			ObjectPOOL.CreatePool(prefab, initialPoolSize);
		}
	
		public static T Spawn<T>(this T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
		{
			return ObjectPOOL.Spawn(prefab, parent, position, rotation);
		}
		public static T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
		{
			return ObjectPOOL.Spawn(prefab, null, position, rotation);
		}
		public static T Spawn<T>(this T prefab, Transform parent, Vector3 position) where T : Component
		{
			return ObjectPOOL.Spawn(prefab, parent, position, Quaternion.identity);
		}
		public static T Spawn<T>(this T prefab, Vector3 position) where T : Component
		{
			return ObjectPOOL.Spawn(prefab, null, position, Quaternion.identity);
		}
		public static T Spawn<T>(this T prefab, Transform parent) where T : Component
		{
			return ObjectPOOL.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
		}
		public static T Spawn<T>(this T prefab) where T : Component
		{
			return ObjectPOOL.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
		}
		public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
		{
			return ObjectPOOL.Spawn(prefab, parent, position, rotation);
		}
		public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation)
		{
			return ObjectPOOL.Spawn(prefab, null, position, rotation);
		}
		public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position)
		{
			return ObjectPOOL.Spawn(prefab, parent, position, Quaternion.identity);
		}
		public static GameObject Spawn(this GameObject prefab, Vector3 position)
		{
			return ObjectPOOL.Spawn(prefab, null, position, Quaternion.identity);
		}
		public static GameObject Spawn(this GameObject prefab, Transform parent)
		{
			return ObjectPOOL.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
		}
		public static GameObject Spawn(this GameObject prefab)
		{
			return ObjectPOOL.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
		}
	
		public static void Recycle<T>(this T obj, bool changeParent = true) where T : Component
		{
			ObjectPOOL.Recycle(obj, changeParent);
		}
		public static void Recycle(this GameObject obj, bool changeParent = true)
		{
			ObjectPOOL.Recycle(obj, changeParent);
		}

		public static void RecycleAll<T>(this T prefab, bool changeParent = true) where T : Component
		{
			ObjectPOOL.RecycleAll(prefab, changeParent);
		}
		public static void RecycleAll(this GameObject prefab, bool changeParent = true)
		{
			ObjectPOOL.RecycleAll(prefab, changeParent);
		}

		public static int CountPooled<T>(this T prefab) where T : Component
		{
			return ObjectPOOL.CountPooled(prefab);
		}
		public static int CountPooled(this GameObject prefab)
		{
			return ObjectPOOL.CountPooled(prefab);
		}

		public static int CountSpawned<T>(this T prefab) where T : Component
		{
			return ObjectPOOL.CountSpawned(prefab);
		}
		public static int CountSpawned(this GameObject prefab)
		{
			return ObjectPOOL.CountSpawned(prefab);
		}

		public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list, bool appendList)
		{
			return ObjectPOOL.GetSpawned(prefab, list, appendList);
		}
		public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list)
		{
			return ObjectPOOL.GetSpawned(prefab, list, false);
		}
		public static List<GameObject> GetSpawned(this GameObject prefab)
		{
			return ObjectPOOL.GetSpawned(prefab, null, false);
		}
		public static List<T> GetSpawned<T>(this T prefab, List<T> list, bool appendList) where T : Component
		{
			return ObjectPOOL.GetSpawned(prefab, list, appendList);
		}
		public static List<T> GetSpawned<T>(this T prefab, List<T> list) where T : Component
		{
			return ObjectPOOL.GetSpawned(prefab, list, false);
		}
		public static List<T> GetSpawned<T>(this T prefab) where T : Component
		{
			return ObjectPOOL.GetSpawned(prefab, null, false);
		}

		public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list, bool appendList)
		{
			return ObjectPOOL.GetPooled(prefab, list, appendList);
		}
		public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list)
		{
			return ObjectPOOL.GetPooled(prefab, list, false);
		}
		public static List<GameObject> GetPooled(this GameObject prefab)
		{
			return ObjectPOOL.GetPooled(prefab, null, false);
		}
		public static List<T> GetPooled<T>(this T prefab, List<T> list, bool appendList) where T : Component
		{
			return ObjectPOOL.GetPooled(prefab, list, appendList);
		}
		public static List<T> GetPooled<T>(this T prefab, List<T> list) where T : Component
		{
			return ObjectPOOL.GetPooled(prefab, list, false);
		}
		public static List<T> GetPooled<T>(this T prefab) where T : Component
		{
			return ObjectPOOL.GetPooled(prefab, null, false);
		}
	}
}