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
        public float ScaleX = 1, ScaleY = 1;
        public Vector3 TransformPoint(Vector3 p) => new Vector3(position.x + p.x * ScaleX, position.y + p.y * ScaleY, position.z + p.z);
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
    public struct UIVertex { public Vector3 position; }
    public class TextGenerator
    {
        public int Reads; public readonly List<UIVertex> Vertices = new List<UIVertex>();
        public IList<UIVertex> verts { get { Reads++; return Vertices; } }
    }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Canvas : Behaviour { public Canvas rootCanvas => Root ?? this; public Canvas Root; public RenderMode renderMode; public Camera worldCamera; }
    public class Camera : Behaviour { public static Camera main = new Camera(); public Func<Vector3, Vector3> Project = p => p; public Vector3 WorldToScreenPoint(Vector3 p) => Project(p); }
    public static class RectTransformUtility
    { public static Vector2 WorldToScreenPoint(Camera camera, Vector3 p) { if (camera != null) p = camera.WorldToScreenPoint(p); return new Vector2(p.x, p.y); } }
}
namespace UnityEngine.UI
{
    public class Selectable : UnityEngine.Behaviour { public bool Interactable = true; public bool IsInteractable() => Interactable; }
    public class Image : UnityEngine.Behaviour { }
    public class Text : UnityEngine.Behaviour
    {
        private UnityEngine.TextGenerator m_TextCache;
        public string text = ""; public float pixelsPerUnit = 1;
        public UnityEngine.Vector2 PixelAdjustPoint(UnityEngine.Vector2 p) => p;
        public void Render(UnityEngine.TextGenerator generator) { m_TextCache = generator; }
    }
}
namespace Mortal.Core
{
    public class PositionOptionButton : UnityEngine.Behaviour
    {
        public UnityEngine.UI.Text[] _titleText, _typeText, _descText;
        public UnityEngine.UI.Image[] _iconImage;
    }
    public class BreakOptionButton : UnityEngine.Behaviour
    {
        public UnityEngine.UI.Text _typeText;
        public UnityEngine.UI.Text[] _titleText, _actionText, _contributeText;
    }
}
namespace UnityEngine.EventSystems { public class EventSystem { public static EventSystem current = new EventSystem(); public UnityEngine.GameObject currentSelectedGameObject; } }
namespace Mortal.Free
{
    public class FreeManager : UnityEngine.Behaviour
    {
        public static FreeManager Instance { get; set; }
        public UnityEngine.Camera _mainCamera;
        public bool EnableHotKey { get; set; } = true;
        public bool IsProcessing { get; set; }
        public bool _menuOpen, _statOpen, _saveOpen;
    }
    public class PositionController : UnityEngine.Behaviour { public UnityEngine.Component[] _colliders; public FreeManager _freeManager = new FreeManager(); }
}
namespace Mortal.Story
{
    public class StoryManager : UnityEngine.Behaviour
    {
        public static StoryManager Instance { get; set; }
        public bool _panelOpen, _menuOpen, _statusOpen, _logOpen;
    }
}
