using System;
using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Runs a callback when its object is destroyed (a pop-up closed by any of its buttons).</summary>
    public sealed class OnDestroyHook : MonoBehaviour
    {
        public Action Done;
        private void OnDestroy() { if (Done != null) Done(); }
    }
}
