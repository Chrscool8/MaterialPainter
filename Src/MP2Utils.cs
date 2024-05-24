using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaterialPainter2
{
    public static class Utils
    {
        public static void PrintHierarchy(GameObject gameObject, int depth = 1, bool listComponents = false)
        {
            // Print the GameObject's name with indentation based on its depth
            string indentation = new string('-', depth * 2);

            MP2.MPDebug($"{indentation} {gameObject.name} {gameObject.tag}");

            // If listComponents is true, print the types of components attached to this GameObject
            if (listComponents)
            {
                Component[] components = gameObject.GetComponents<Component>();
                foreach (Component component in components)
                {
                    MP2.MPDebug($"{indentation}    {component.GetType()}");
                }
            }

            // Recursively print the children GameObjects
            foreach (Transform child in gameObject.transform)
            {
                PrintHierarchy(child.gameObject, depth + 1, listComponents);
            }
        }

        public static bool PointInRectangle(Vector2 point, Rect rectangle)
        {
            return point.x >= rectangle.x && point.x <= rectangle.x + rectangle.width &&
                   point.y >= rectangle.y && point.y <= rectangle.y + rectangle.height;
        }

        public static bool FloatCloseToEqual(float a, float b, float tolerance)
        {
            return System.Math.Abs(a - b) < tolerance;
        }

        public static bool ColorCloseToEqual(Color a, Color b, float tolerance = 0.0002f)
        {
            try
            {
                return (FloatCloseToEqual(a[0], b[0], tolerance) && FloatCloseToEqual(a[1], b[1], tolerance) && FloatCloseToEqual(a[2], b[2], tolerance));
            }
            catch
            {
                return false;
            }
        }

        public static List<GameObject> GetAllObjectsInScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            List<GameObject> allGameObjects = new List<GameObject>();

            foreach (GameObject rootObject in currentScene.GetRootGameObjects())
            {
                AddChildObjects(rootObject.transform, allGameObjects);
            }

            return allGameObjects;
        }

        public static void AddChildObjects(Transform parentTransform, List<GameObject> allGameObjects = null)
        {
            allGameObjects.Add(parentTransform.gameObject);

            foreach (Transform childTransform in parentTransform)
            {
                AddChildObjects(childTransform, allGameObjects);
            }
        }
    }
}