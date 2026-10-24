using System.Collections.Generic;
using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// The info area of a screen's top bar. Each page that draws its header here gets its own child, shown only while that page is
    /// active and dropped when the page is rebuilt or destroyed (see <see cref="UiFactory.TabHeader"/>).
    /// </summary>
    public sealed class HeaderSlot : MonoBehaviour
    {
        private readonly List<KeyValuePair<GameObject, GameObject>> _hosts = new List<KeyValuePair<GameObject, GameObject>>();   // header host, its page

        public void Register(GameObject host, GameObject page)
        {
            _hosts.Add(new KeyValuePair<GameObject, GameObject>(host, page));
            host.SetActive(page.activeInHierarchy);
        }

        private void LateUpdate()
        {
            for (int i = _hosts.Count - 1; i >= 0; i--)
            {
                var host = _hosts[i].Key;
                var page = _hosts[i].Value;
                if (host == null || page == null)
                {
                    if (host != null) Destroy(host);
                    _hosts.RemoveAt(i);
                    continue;
                }
                bool on = page.activeInHierarchy;
                if (host.activeSelf != on) host.SetActive(on);
            }
        }
    }
}
