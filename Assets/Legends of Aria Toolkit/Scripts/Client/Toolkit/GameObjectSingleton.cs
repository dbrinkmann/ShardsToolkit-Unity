using UnityEngine;
using System.Collections;

public class GameObjectSingleton<T> : MonoBehaviour where T:Component{

    private static T instance;

#if UNITY_EDITOR
    private static bool applicationQuitting = false;
#endif

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(T)) as T;

                if (instance == null)
                {
#if UNITY_EDITOR                    
                    // make sure we dont end up with two instances if we are shutting down in the editor
                    if (applicationQuitting)
                    {
                        return null;
                    }
#endif                    
                    GameObject newObj = new GameObject(typeof(T).ToString());
                    instance = newObj.AddComponent<T>();
                }                
            }

            return instance;
        }
    }

    public static bool HasInstance()
    {
        return instance != null;
    }

    void OnEnable()
    {
        if (instance == null)
            instance = this.GetComponent<T>();
    }

#if UNITY_EDITOR
    void OnApplicationQuit()
    {
        applicationQuitting = true;
    }
#endif
}

