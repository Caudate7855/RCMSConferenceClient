using UnityEngine;

namespace VIAVR.Scripts.Core.Singleton
{
    public class Singleton<T> where T : UnityEngine.Object
    {
        static GameObject _singletonGameObject;
        static GameObject singletonGo {
            get {
                if (_singletonGameObject == null)
                    _singletonGameObject = GameObject.FindGameObjectWithTag("Singleton");
                return _singletonGameObject;
            }
        }

        static T _instance;
        public static T Instance {
            get {
                if (_instance == null)
                {
                    if (singletonGo == null)
                    {
                        Debug.LogError($"singletonGo == null{(Application.isEditor ? " <b>(если при стопе приложения то всё ок)</b>" : "")}");
                        return null;
                    }

                    var alreadyContainingComponent = singletonGo.GetComponent<T>();

                    if (alreadyContainingComponent != null)
                    {
                        _instance = alreadyContainingComponent;
                    }
                    else
                    {
                        var tType = typeof(T);

                        if (tType.IsInterface)
                        {
                            var targetType = DependencyContainer.Resolve(tType);

                            singletonGo.AddComponent(targetType);
                        }
                        else
                        {
                            alreadyContainingComponent = GameObject.FindObjectOfType<T>();

                            if (alreadyContainingComponent != null)
                            {
                                _instance = alreadyContainingComponent;
                            }
                            else
                            {
                                //Debug.Log($"<color=orange>Регистрируй <b>{tType}</b> в объекте Singletone в компоненте DependencyContainer.</color>");

                                singletonGo.AddComponent(typeof(T));

                                alreadyContainingComponent = singletonGo.GetComponent<T>();
                            }
                        }

                        _instance = alreadyContainingComponent;
                    }
                }

                return _instance;
            }
        }
    }
}