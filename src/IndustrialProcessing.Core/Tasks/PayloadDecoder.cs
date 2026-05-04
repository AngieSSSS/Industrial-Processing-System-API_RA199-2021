using System;
using System.Collections.Generic;

namespace IndustrialProcessing.Tasks
{
    // payload format: kljuc:vrijednost,kljuc:vrijednost
    internal static class PayloadDecoder
    {
        public static Dictionary<string, string> ToMap(string payload)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var parts = payload.Split(',');
            foreach (var p in parts)
            {
                var part = p.Trim();
                if (part.Length == 0) continue;

                int colon = part.IndexOf(':');
                if (colon <= 0 || colon == part.Length - 1)
                    throw new FormatException("Neispravan payload: " + part);

                string k = part.Substring(0, colon).Trim();
                string v = part.Substring(colon + 1).Trim();
                if (k.Length == 0 || v.Length == 0)
                    throw new FormatException("Prazan kljuc ili vrijednost: " + part);

                map[k] = v;
            }

            return map;
        }

        public static int ReadInteger(IReadOnlyDictionary<string, string> map, string field)
        {
            if (!map.TryGetValue(field, out var raw))
                throw new FormatException("Polje " + field + " ne postoji u payload-u.");

            // dozvoljavamo razdvajace tipa 10_000
            var s = raw.Replace("_", "").Replace(" ", "");
            return int.Parse(s);
        }
    }
}
