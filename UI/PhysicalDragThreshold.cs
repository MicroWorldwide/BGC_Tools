﻿using UnityEngine;
using UnityEngine.EventSystems;

namespace BGC.UI
{
    /// <summary>
    /// Sets the drag threshold for an EventSystem as a physical size based on DPI.
    /// Taken from:
    /// https://forum.unity.com/threads/buttons-within-scroll-rect-are-difficult-to-press-on-mobile.265682/#post-1993502
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public class PhysicalDragThreshold : MonoBehaviour
    {
        [SerializeField]
        private float dragThresholdInches = 0.125f;

        void Start()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                    GetComponent<EventSystem>().pixelDragThreshold = (int)(dragThresholdInches * Screen.dpi);
                    break;

                default:
                    GetComponent<EventSystem>().pixelDragThreshold = 5;
                    break;
            }
        }
    }
}
