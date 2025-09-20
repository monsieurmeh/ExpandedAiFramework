using UnityEngine;
using System;

namespace ExpandedAiFramework.DebugMenu
{
    public interface IDebugMenuTabContentProvider
    {
        void Initialize(GameObject parentContentArea);
        void Show();
        void Hide();
        void Refresh();
        void Cleanup();
    }
}
