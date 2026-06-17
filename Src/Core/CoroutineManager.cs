using System;
using System.Collections;

using UnityEngine;

namespace MaterialPainter2
{
    public class CoroutineManager : MonoBehaviour
    {
        public static CoroutineManager instance;

        public static CoroutineManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("CoroutineManager");
                    instance = obj.AddComponent<CoroutineManager>();
                }
                return instance;
            }
        }

        public static void DelayAction(float delay, Action action)
        {
            Instance.StartCoroutine(DelayCoroutine(delay, action));
        }

        public static IEnumerator DelayCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}
