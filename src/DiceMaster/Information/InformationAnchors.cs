using System;
using System.Collections;
using System.Reflection;
using Mortal.Free;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DiceMaster.Information
{
    // Geometry and native focus only. No scene scan, raycast, pointer capture,
    // gameplay evaluation, or selectable UI is introduced by passive hints.
    public static class InformationAnchors
    {
        private static readonly FieldInfo colliders = typeof(PositionController).GetField("_colliders", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo free = typeof(PositionController).GetField("_freeManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo freeCamera = typeof(FreeManager).GetField("_mainCamera", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly Vector3[] corners = new Vector3[4];
        private static object cameraOwner;
        private static Camera camera;
        private static float cameraTime = float.NaN;
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Live(Component value)
        { return value != null && value.gameObject.activeInHierarchy && (!(value is Behaviour) || ((Behaviour)value).isActiveAndEnabled); }
        private static Camera CameraFor(Component component)
        {
            object owner = component is PositionController && free != null ? free.GetValue(component) : null;
            if (!ReferenceEquals(owner, cameraOwner) || cameraTime != Time.unscaledTime)
            {
                cameraOwner = owner; cameraTime = Time.unscaledTime;
                camera = owner == null || freeCamera == null ? null : freeCamera.GetValue(owner) as Camera;
                if (camera == null) camera = Camera.main;
            }
            return camera;
        }
        private static bool ComponentRect(Component component, Camera fallback, out Rect rect)
        {
            rect = default(Rect);
            if (!Live(component)) return false;
            var rt = component.transform as RectTransform;
            if (rt == null)
            {
                if (fallback == null) return false;
                Vector3 point = fallback.WorldToScreenPoint(component.transform.position);
                if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z) || point.z <= 0) return false;
                rect = new Rect(point.x - 12, Screen.height - point.y - 12, 24, 24);
                return true;
            }
            Canvas canvas = component.GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            bool overlay = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay;
            Camera projection = overlay ? null : canvas != null && canvas.worldCamera != null ? canvas.worldCamera : fallback;
            if (!overlay && projection == null) return false;
            rt.GetWorldCorners(corners);
            float left = float.MaxValue, right = float.MinValue, bottom = float.MaxValue, top = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                if (!Finite(corner.x) || !Finite(corner.y) || !Finite(corner.z)) return false;
                if (projection != null && projection.WorldToScreenPoint(corner).z <= 0) return false;
                Vector2 point = RectTransformUtility.WorldToScreenPoint(projection, corner);
                if (!Finite(point.x) || !Finite(point.y)) return false;
                left = Math.Min(left, point.x); right = Math.Max(right, point.x); bottom = Math.Min(bottom, point.y); top = Math.Max(top, point.y);
            }
            rect = new Rect(left, Screen.height - top, right - left, top - bottom);
            return rect.width > 1 && rect.height > 1;
        }
        public static bool TryRect(object anchor, out Rect rect)
        {
            rect = default(Rect);
            try
            {
                var component = anchor as Component;
                if (!Live(component)) return false;
                Camera projection = CameraFor(component);
                var areas = component is PositionController && colliders != null ? colliders.GetValue(component) as IList : null;
                bool found = false;
                if (areas != null)
                    for (int at = 0; at < Math.Min(16, areas.Count); at++)
                    {
                        Rect next;
                        if (!ComponentRect(areas[at] as Component, projection, out next)) continue;
                        if (!found) { rect = next; found = true; }
                        else
                        {
                            float left = Math.Min(rect.x, next.x), top = Math.Min(rect.y, next.y);
                            rect = new Rect(left, top, Math.Max(rect.x + rect.width, next.x + next.width) - left, Math.Max(rect.y + rect.height, next.y + next.height) - top);
                        }
                    }
                if (!found && !ComponentRect(component, projection, out rect)) return false;
                return Finite(rect.x) && Finite(rect.y) && Finite(rect.width) && Finite(rect.height) && rect.width > 0 && rect.height > 0 &&
                    rect.x < Screen.width && rect.y < Screen.height && (double)rect.x + rect.width > 0 && (double)rect.y + rect.height > 0;
            }
            catch (Exception) { return false; }
        }
        public static bool IsVisible(PositionController controller) { Rect rect; return TryRect(controller, out rect); }
        public static bool IsInteractive(object anchor)
        {
            var component = anchor as Component;
            if (!Live(component)) return false;
            var selectable = component.GetComponent<UnityEngine.UI.Selectable>();
            return selectable == null || selectable.isActiveAndEnabled && selectable.IsInteractable();
        }
        public static bool IsFocused(object anchor)
        {
            var component = anchor as Component;
            var system = EventSystem.current;
            var selected = system == null ? null : system.currentSelectedGameObject;
            if (!Live(component) || selected == null) return false;
            if (selected == component.gameObject || selected.transform.IsChildOf(component.transform)) return true;
            var areas = component is PositionController && colliders != null ? colliders.GetValue(component) as IList : null;
            if (areas != null)
                for (int at = 0; at < Math.Min(16, areas.Count); at++)
                {
                    var area = areas[at] as Component;
                    if (Live(area) && (selected == area.gameObject || selected.transform.IsChildOf(area.transform))) return true;
                }
            return false;
        }
    }
}
