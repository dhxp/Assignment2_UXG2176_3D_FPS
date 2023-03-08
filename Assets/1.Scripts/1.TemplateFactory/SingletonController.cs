using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Helper parent class: inherit to make script a singleton
// access class instance like "MyCoolClass.I.MyCoolFunction"
public class SingletonController<T> : MonoBehaviour where T : Component
{
    private static T instance;
    public static T I
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    public virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            Debug.Log(gameObject.name + " Started!");
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Found more than one " + gameObject.name + " in the scene.");
        }
    }

}
