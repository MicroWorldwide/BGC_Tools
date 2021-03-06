﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace BGC.UI.Panels
{
    /// <summary>
    /// Clones the appearance of a ModelPanel to show it sliding offscreen.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CopyPanel : ModePanel
    {
        [SerializeField]
        private Canvas mainCanvas = null;
        [SerializeField]
        private Camera mainCamera = null;

#pragma warning disable UNT0007 // Null coalescing on Unity objects

        private Image _image = null;
        public Image Image => _image ?? (_image = GetComponent<Image>());

#pragma warning restore UNT0007 // Null coalescing on Unity objects

        [NonSerialized] //Added to fix Unity Serialization issue
        private RenderTexture rt;
        private Texture2D tex;

        private const float scalefactor = 4f;

        public void TakeSnapshot(RectTransform copyRect)
        {
            int rtWidth = (int)(mainCanvas.pixelRect.width / scalefactor);
            int rtHeight = (int)(mainCanvas.pixelRect.height / scalefactor);
            if (rt == null || rt.width != rtWidth || rt.height != rtHeight)
            {
                if (rt != null)
                {
                    Destroy(rt);
                }
                rt = new RenderTexture(rtWidth, rtHeight, 16);
            }

            int texWidth = (int)(mainCamera.pixelWidth / scalefactor);
            int texHeight = (int)(mainCamera.pixelHeight / scalefactor);
            if (tex == null || tex.width != texWidth || tex.height != texHeight)
            {
                if (tex != null)
                {
                    Destroy(tex);
                }
                tex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false);
            }

            Vector3[] corners = new Vector3[4];
            copyRect.GetWorldCorners(corners);

            Vector2 position = corners[0] / scalefactor;

            position.x = (float)Math.Floor(position.x);
            position.y = (float)Math.Floor(position.y);

            Vector2 size = (corners[2] - corners[0]) / scalefactor;

            size.x = (float)Math.Floor(size.x);
            size.y = (float)Math.Floor(size.y);

            Rect worldRect = new Rect(Vector2.zero, size);

            RenderMode cachedRenderMode = mainCanvas.renderMode;
            mainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            mainCanvas.worldCamera = mainCamera;

            mainCamera.targetTexture = rt;

            mainCamera.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0f, 0f, tex.width, tex.height), 0, 0);
            tex.Apply();
            mainCamera.targetTexture = null;
            RenderTexture.active = null;

            mainCanvas.renderMode = cachedRenderMode;

            Image.overrideSprite = Sprite.Create(
                texture: tex,
                rect: worldRect,
                pivot: Vector2.zero);

        }

        public override void FocusAcquired()
        {
            //Do Nothing
        }

        public override void FocusLost()
        {
            //Do Nothing
        }
    }
}

