using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace EditorHelper.Icons
{
    public class IconStore
    {
        private readonly int _width, _height;
        private readonly Color[] _transparent;
        private readonly Texture2D _transparentTexture;

        private readonly Camera _camera;

        private class ObjectIconInfo
        {
            public ObjectAsset ObjectAsset;

            public int Handle;
            public ItemIconReady Callback;

            public void AddCallback(ItemIconReady callback)
            {
                Callback = (ItemIconReady)Delegate.Combine(Callback, callback);
            }
        }

        private Stack<ObjectIconInfo> QueuedObjectIcons = []; // Stack to make newer requests higher priority
        private Dictionary<int, Guid> QueuedItemIcons = [];
        private Dictionary<Guid, Texture2D> CachedIcons = [];

        private int ObjectIconHandle = 1000;

        private ObjectIconInfo? PendingIconInfo;
        private Transform? PendingObject;

        public IconStore(int width, int height)
        {
            _width = width;
            _height = height;

            _transparent = new Color[_width * _height];
            for (int p = 0; p < _transparent.Length; p++)
                _transparent[p] = Color.clear;

            _transparentTexture = new Texture2D(_width, _height, TextureFormat.ARGB32, mipChain: false)
            {
                name = "Transparent Texture"
            };
            _transparentTexture.SetPixels(_transparent);
            _transparentTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            GameObject cameraObject = new GameObject("IconStore:Camera");
            _camera = cameraObject.AddComponent<Camera>();
            _camera.cullingMask = RayMasks.SMALL | RayMasks.MEDIUM | RayMasks.LARGE | RayMasks.ENEMY;
            _camera.clearFlags = CameraClearFlags.Nothing;
            _camera.orthographic = true;
            _camera.enabled = false; // Disable Rendering and use Camera.Render() instead
        }

        public int RequestIcon(ItemAsset itemAsset, ItemIconReady callback)
        {
            if (CachedIcons.TryGetValue(itemAsset.GUID, out Texture2D? icon))
            {
                callback(-1, icon);
                return -1;
            }

            callback = (ItemIconReady)Delegate.Combine(callback, new ItemIconReady(CacheItemIcon));
            int handle = ItemTool.getIcon(itemAsset.id, 100, itemAsset.getState(), itemAsset, _width, _height, callback);

            if (handle != -1)
            {
                ObjectIconHandle++;
                QueuedItemIcons.Add(handle, itemAsset.GUID);
            }
            return handle;
        }

        private void CacheItemIcon(int handle, Texture2D icon)
        {
            if (!QueuedItemIcons.TryGetValue(handle, out Guid itemGuid)) return;
            if (CachedIcons.ContainsKey(itemGuid)) return;

            CachedIcons.Add(itemGuid, icon);
            QueuedItemIcons.Remove(handle);
        }

        public int RequestIcon(ObjectAsset objectAsset, ItemIconReady callback)
        {
            if (CachedIcons.TryGetValue(objectAsset.GUID, out Texture2D? icon))
            {
                callback(-1, icon);
                return -1;
            }
            foreach (ObjectIconInfo queuedIcon in QueuedObjectIcons)
            {
                if (queuedIcon.ObjectAsset == objectAsset)
                {
                    queuedIcon.AddCallback(callback);
                    return queuedIcon.Handle;
                }
            }
            if (PendingIconInfo != null && PendingIconInfo.ObjectAsset == objectAsset)
            {
                PendingIconInfo.AddCallback(callback);
                return PendingIconInfo.Handle;
            }

            ObjectIconInfo iconInfo = new()
            {
                ObjectAsset = objectAsset,

                Handle = ObjectIconHandle,
                Callback = callback
            };
            QueuedObjectIcons.Push(iconInfo);

            ObjectIconHandle++;
            return iconInfo.Handle;
        }

        private List<Renderer> _renderers = new(4);
        private Bounds GetBounds(Transform transform)
        {
            _renderers.Clear();
            transform.GetComponentsInChildren(_renderers);

            Bounds bounds = default;
            bool boundsSet = false;
            foreach (Renderer renderer in _renderers)
            {
                if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;

                if (!boundsSet)
                {
                    bounds = renderer.bounds;
                    boundsSet = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            if (!boundsSet) bounds = new Bounds(transform.position, Vector3.one);
            return bounds;
        }

        // Taken from ItemTool
        private float CalculateOrthographicSize(Bounds bounds)
        {
            Vector3 extents = bounds.extents;
            if (extents.ContainsInfinity() || extents.ContainsNaN() || extents.IsNearlyZero()) return 1f;

            Transform cameraTransform = _camera.transform;

            Bounds bounds2 = new Bounds(cameraTransform.InverseTransformVector(extents), Vector3.zero);
            bounds2.Encapsulate(cameraTransform.InverseTransformVector(-extents));
            bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, extents.y, extents.z)));
            bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, 0f - extents.y, extents.z)));
            bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, extents.y, 0f - extents.z)));
            bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, 0f - extents.y, extents.z)));
            bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(0f - extents.x, extents.y, 0f - extents.z)));
            bounds2.Encapsulate(cameraTransform.InverseTransformVector(new Vector3(extents.x, 0f - extents.y, 0f - extents.z)));
            Vector3 extents2 = bounds2.extents;
            if (extents2.ContainsInfinity() || extents2.ContainsNaN() || extents2.IsNearlyZero()) return 1f;

            float num = Mathf.Abs(extents2.x);
            float num2 = Mathf.Abs(extents2.y);
            float num3 = Mathf.Abs(extents2.z);
            float nearClipPlane = _camera.nearClipPlane;
            cameraTransform.position = bounds.center - cameraTransform.forward * (num3 + 0.02f + nearClipPlane);

            num *= (float)(_width + 16) / (float)_width;
            num2 *= (float)(_height + 16) / (float)_height;
            float num4 = (float)_width / (float)_width;
            float num5 = num / num2;
            float num6 = ((num5 > num4) ? (num5 / num4) : 1f);
            return num2 * num6;
        }

        private Texture2D CaptureObjectIcon(ObjectAsset objectAsset, Transform objectTransform)
        {
            Bounds bounds = GetBounds(objectTransform);
            
            Vector3 direction;
            if (objectAsset.FriendlyName.ToLower().Contains("billboard") || objectAsset.interactability == EObjectInteractability.NPC)
            {
                direction = (objectTransform.right - objectTransform.up).normalized;
            }
            else
            {
                direction = (objectTransform.right + objectTransform.up).normalized;
            }
            float distance = Mathf.Max(bounds.size.x, bounds.size.z);
            float height = (bounds.size.y * 0.85f);
            if (bounds.size.y * 2 > Math.Abs(bounds.size.x - bounds.size.z))
            {
                height = bounds.size.y * 0.45f;
            }
            
            bool isFlat = bounds.size.y < 9f && bounds.size.y < Mathf.Min(bounds.size.x, bounds.size.z) * 0.25f;
            if (isFlat)
            {
                _camera.transform.position = bounds.center + direction * distance + Vector3.up * Mathf.Max(bounds.size.x, bounds.size.z);
            }
            else
            {
                _camera.transform.position = bounds.center + direction * distance + Vector3.up * height;
            }
            
            _camera.transform.rotation = Quaternion.LookRotation((bounds.center - _camera.transform.position).normalized);
            
            
            int antiAliasing = SDG.Unturned.GraphicsSettings.IsItemIconAntiAliasingEnabled ? 4 : 1;
            RenderTexture temporary = RenderTexture.GetTemporary(_width, _height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, antiAliasing);
            temporary.name = "Render_" + objectTransform.name;
            RenderTexture.active = temporary;
            _camera.targetTexture = temporary;
            _camera.orthographicSize = objectAsset.interactability == EObjectInteractability.NPC ? 1.4f : CalculateOrthographicSize(bounds);
            _camera.farClipPlane = (objectTransform.position - _camera.transform.position).magnitude * 2f;

            bool fog = RenderSettings.fog;
            AmbientMode ambientMode = RenderSettings.ambientMode;
            Color ambientSkyColor = RenderSettings.ambientSkyColor;
            Color ambientEquatorColor = RenderSettings.ambientEquatorColor;
            Color ambientGroundColor = RenderSettings.ambientGroundColor;
            Texture customReflection = RenderSettings.customReflection;

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.white;
            RenderSettings.ambientEquatorColor = Color.white;
            RenderSettings.ambientGroundColor = Color.white;
            RenderSettings.customReflection = null;
            if (Provider.isConnected)
                LevelLighting.setEnabled(isEnabled: false);

            GL.Clear(clearDepth: true, clearColor: true, ColorEx.BlackZeroAlpha);
            _camera.Render();

            if (Provider.isConnected)
                LevelLighting.setEnabled(isEnabled: true);
            RenderSettings.fog = fog;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
            RenderSettings.customReflection = customReflection;

            objectTransform.position = new Vector3(0f, -256f, 256f);

            Texture2D objectIcon = new(_width, _height, TextureFormat.ARGB32, mipChain: false)
            {
                name = "Icon_" + objectTransform.name,
                filterMode = FilterMode.Point
            };
            objectIcon.ReadPixels(new Rect(0f, 0f, _width, _height), 0, 0);
            objectIcon.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(temporary);
            GameObject.Destroy(objectTransform.gameObject);

            return objectIcon;
        }

        private Texture2D ResizeTexture(Texture2D texture, int newWidth, int newHeight)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(newWidth, newHeight);
            Graphics.Blit(texture, temporary);

            RenderTexture.active = temporary;

            Texture2D newTexture = new(_width, _height, TextureFormat.ARGB32, mipChain: false)
            {
                name = texture.name,
                filterMode = FilterMode.Point
            };
            newTexture.SetPixels(_transparent);
            newTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            newTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), newTexture.width / 2 - newWidth / 2, newTexture.height / 2 - newHeight / 2);
            newTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(temporary);

            return newTexture;
        }

        public void CustomUpdate()
        {
            if (PendingIconInfo == null)
            {
                if (QueuedObjectIcons.Count == 0) return;

                PendingIconInfo = QueuedObjectIcons.Pop();
                if (PendingIconInfo.ObjectAsset.type == EObjectType.DECAL) return;

                GameObject original = PendingIconInfo.ObjectAsset.GetOrLoadModel(Level.isEditor);
                if (original == null)
                {
                    PendingIconInfo.Callback(PendingIconInfo.Handle, _transparentTexture);
                    CachedIcons.Add(PendingIconInfo.ObjectAsset.GUID, _transparentTexture);
                    PendingIconInfo = null;
                    return;
                }
                PendingObject = GameObject.Instantiate(original).transform;

                if (PendingIconInfo.ObjectAsset.rubble != EObjectRubble.NONE)
                {
                    InteractableObjectRubble interactableRubble = PendingObject.gameObject.AddComponent<InteractableObjectRubble>();
                    interactableRubble.updateState(PendingIconInfo.ObjectAsset, PendingIconInfo.ObjectAsset.getState());
                    Transform? editor = PendingObject.Find("Editor");
                    if (editor != null) editor.gameObject.SetActive(PendingIconInfo.ObjectAsset.rubbleEditor == EObjectRubbleEditor.DEAD && Level.isEditor);
                }

                if (PendingIconInfo.ObjectAsset.interactability == EObjectInteractability.NPC)
                {
                    InteractableObjectNPC interactableNPC = PendingObject.gameObject.AddComponent<InteractableObjectNPC>();
                    interactableNPC.updateState(PendingIconInfo.ObjectAsset, PendingIconInfo.ObjectAsset.getState());
                    interactableNPC.enabled = false;
                    Animation animationNPC = PendingObject.Find("Root").GetComponent<Animation>();
                    animationNPC.Play("Idle_Stand");
                    animationNPC["Idle_Stand"].normalizedTime = 1f;
                }

                PendingObject.position = new Vector3(256f, -256f, 0f);
                PendingObject.rotation = Quaternion.Euler(-90f, 0f, 0f);
                return;
            }

            Texture2D objectIcon;
            if (PendingIconInfo.ObjectAsset.type == EObjectType.DECAL)
            {
                GameObject original = PendingIconInfo.ObjectAsset.GetOrLoadModel(Level.isEditor);
                Transform decalTransform = original.transform.Find("Decal");
                objectIcon = (Texture2D)decalTransform.GetComponent<Decal>().material.GetTexture("_MainTex");
                if (objectIcon == null)
                {
                    objectIcon = _transparentTexture;
                    CommandWindow.LogWarning($"{PendingIconInfo.ObjectAsset.AssetErrorPrefix}: Missing \"Decal\" Texture!");
                }

                int width = _width;
                int height = _height;
                if (decalTransform.localScale.x > decalTransform.localScale.y)
                    height = Mathf.CeilToInt(height * decalTransform.localScale.y / decalTransform.localScale.x);
                else
                    width = Mathf.CeilToInt(width * decalTransform.localScale.x / decalTransform.localScale.y);

                objectIcon = ResizeTexture(objectIcon, width, height);
            }
            else
                objectIcon = CaptureObjectIcon(PendingIconInfo.ObjectAsset, PendingObject);

            PendingIconInfo.Callback(PendingIconInfo.Handle, objectIcon);
            CachedIcons.Add(PendingIconInfo.ObjectAsset.GUID, objectIcon);

            if (PendingObject != null) GameObject.Destroy(PendingObject.gameObject);
            PendingObject = null;
            PendingIconInfo = null;
        }
    }
}
