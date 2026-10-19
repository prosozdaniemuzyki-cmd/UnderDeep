using System.Collections.Generic;
using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Core
{
    public static class Localization
    {
        private static Dictionary<string, string> _ru = new()
        {
            {"drill_mk2","Бур MK2"}, {"drill_mk3","Бур MK3"}, {"battery","Аккумулятор"},
            {"radar","Радар"}, {"magnet","Магнит"}, {"shield","Аварийный щит"},
            {"evac","ЭВАКУАЦИЯ (10 топлива)"}, {"depth","Глубина"}, {"fuel","Топливо"}, {"heat","Перегрев"}
        };
        private static Dictionary<string, string> _en = new()
        {
            {"drill_mk2","Drill MK2"}, {"drill_mk3","Drill MK3"}, {"battery","Battery"},
            {"radar","Radar"}, {"magnet","Magnet"}, {"shield","Emergency Shield"},
            {"evac","EVAC (10 fuel)"}, {"depth","Depth"}, {"fuel","Fuel"}, {"heat","Heat"}
        };
        public static string Get(string key)
        {
            var lang = "ru";
            if (ServiceLocator.TryGet<IPlatformProvider>(out var p)) lang = p.Language;
            var dict = lang.StartsWith("ru") ? _ru : _en;
            return dict.TryGetValue(key, out var v) ? v : key;
        }
    }
}
