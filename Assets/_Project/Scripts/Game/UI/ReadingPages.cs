using System;
using System.Collections.Generic;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Lossless story pagination. The caller supplies its actual font measurement.</summary>
    public static class ReadingPages
    {
        public static List<string> Split(string value, Func<string, float> measure, float height)
        {
            var pages = new List<string>();
            if (string.IsNullOrEmpty(value)) { pages.Add(""); return pages; }
            int offset = 0;
            while (offset < value.Length)
            {
                int lo = 1, hi = value.Length - offset, take = 1;
                while (lo <= hi)
                {
                    int mid = lo + (hi - lo) / 2;
                    if (measure(value.Substring(offset, mid)) <= height) { take = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                if (offset + take < value.Length)
                {
                    if (char.IsHighSurrogate(value[offset + take - 1])) take = take > 1 ? take - 1 : 2;
                    int end = take;
                    for (int i = take - 1; i >= take * 2 / 3; i--)
                        if (char.IsWhiteSpace(value[offset + i]) || ".!?。！？".IndexOf(value[offset + i]) >= 0) { end = i + 1; break; }
                    take = end;
                }
                pages.Add(value.Substring(offset, take)); offset += take;
            }
            return pages;
        }
    }
}
