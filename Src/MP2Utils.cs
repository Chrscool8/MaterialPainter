using MaterialPainter2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
    }
}