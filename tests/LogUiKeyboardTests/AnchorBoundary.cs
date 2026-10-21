// Explicit geometry/native focus fixtures; production InformationAnchors runs
// against them. This does not render Unity or execute a physical controller.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public static int Destroyed; public static void Destroy(Object value) { Destroyed++; } }
    public struct Vector3 { public float x, y, z; public Vector3(float a, float b, float c) { x = a; y = b; z = c; } }
    public class Transform
    {
        public Vector3 position; public Transform Parent; internal GameObject Owner;
        public bool IsChildOf(Transform target) { for (Transform at = this; at != null; at = at.Parent) if (ReferenceEquals(at, target)) return true; return false; }
    }
    public class RectTransform : Transform
    {
        public float Width = 120, Height = 32;
        public void GetWorldCorners(Vector3[] corners)
        { corners[0] = position; corners[1] = new Vector3(position.x, position.y + Height, position.z); corners[2] = new Vector3(position.x + Width, position.y + Height, position.z); corners[3] = new Vector3(position.x + Width, position.y, position.z); }
    }
    public class GameObject
    {
        public bool activeInHierarchy = true; public Transform transform;
        public readonly List<Component> Components = new List<Component>();
        public GameObject() { transform = new Transform { Owner = this }; }
    }
    public class Component : Object
    {
        public GameObject gameObject = new GameObject();
        public Transform transform => gameObject.transform;
        public Component() { gameObject.Components.Add(this); }
        public T GetComponent<T>() where T : class { foreach (var c in gameObject.Components) if (c is T value) return value; return null; }
        public T GetComponentInParent<T>() where T : class
        { for (var at = transform; at != null; at = at.Parent) if (at.Owner != null) foreach (var c in at.Owner.Components) if (c is T value) return value; return null; }
    }
    public class Behaviour : Component { public bool isActiveAndEnabled = true; }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Canvas : Behaviour { public Canvas rootCanvas => Root ?? this; public Canvas Root; public RenderMode renderMode; public Camera worldCamera; }
    public class Camera : Behaviour { public static Camera main = new Camera(); public Func<Vector3, Vector3> Project = p => p; public Vector3 WorldToScreenPoint(Vector3 p) => Project(p); }
    public static class RectTransformUtility
    { public static Vector2 WorldToScreenPoint(Camera camera, Vector3 p) { if (camera != null) p = camera.WorldToScreenPoint(p); return new Vector2(p.x, p.y); } }
}
namespace UnityEngine.UI { public class Selectable : UnityEngine.Behaviour { public bool Interactable = true; public bool IsInteractable() => Interactable; } }
namespace UnityEngine.EventSystems { public class EventSystem { public static EventSystem current = new EventSystem(); public UnityEngine.GameObject currentSelectedGameObject; } }
namespace Mortal.Free
{
    public class FreeManager { public UnityEngine.Camera _mainCamera; }
    public class PositionController : UnityEngine.Behaviour { public UnityEngine.Component[] _colliders; public FreeManager _freeManager = new FreeManager(); }
}
