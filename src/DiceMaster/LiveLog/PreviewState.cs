using System;
using System.Collections;
using System.Reflection;

namespace DiceMaster.LiveLog
{
    public static class PreviewState
    {
        private static Type GameType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            throw new TypeLoadException(name);
        }

        // Decorative names have a literal identifier fallback. Missing name
        // metadata must never initialize a game singleton, mutate a save, or
        // turn an otherwise known effect into a failed state evaluation.
        private static object OptionalField(object target, string name)
        {
            if (target == null) return null;
            Type type = target as Type ?? target.GetType();
            for (Type at = type; at != null; at = at.BaseType)
            {
                FieldInfo field = at.GetField(name, BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target is Type ? null : target);
            }
            return null;
        }

        private static object ExistingSingleton(string name)
        {
            Type type;
            try { type = GameType(name); }
            catch (TypeLoadException) { return null; }
            object result = OptionalField(type, "_instance");
            return result != null && result.GetType().FullName == name ? result : null;
        }

        private static IList NameItems(object collection, string typeName)
        {
            if (collection == null || collection.GetType().FullName != typeName) return null;
            object list = OptionalField(collection, "_list");
            if (list == null || !list.GetType().IsGenericType ||
                list.GetType().GetGenericTypeDefinition() != typeof(System.Collections.Generic.List<>)) return null;
            return list as IList;
        }

        private static string NameById(string singleton, string field, string collectionType,
            string itemType, string id, string localePrefix)
        {
            // Locale keys are audited against the supplied game DLL. A name
            // lookup does not depend on an inventory/player singleton being ready.
            return DisplayNames.LookupName(localePrefix + id, id);
        }

        public static string TeamName(string id)
        {
            return NameById("Mortal.Core.PlayerStatManagerData", "_enemyTeam", "Mortal.Core.BattleTeamData",
                "Mortal.Core.BattleTeamStat", id, "EnemyTeam/");
        }

        public static string TalentName(string id)
        {
            return NameById("Mortal.Core.PlayerStatManagerData", "_talents", "Mortal.Core.PlayerTalentDataCollection",
                "Mortal.Core.PlayerTalentData", id, "PlayerTalent/Name/");
        }

        public static string ItemName(int kind, string id)
        {
            string field = kind == 1 ? "_books" : kind == 2 ? "_miscs" : "_special";
            string itemType = kind == 1 ? "Book" : kind == 2 ? "Misc" : "SpecialItem";
            string prefix = kind == 1 ? "Book/Name/" : kind == 2 ? "Misc/Name/" : "Special/Name/";
            return NameById("Mortal.Core.ItemDatabase", field, "Mortal.Core.ItemDataCollection",
                "Mortal.Core." + itemType, id, prefix);
        }

        public static bool LooksLikeFlagId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 2 || !char.IsLetter(value[0])) return false;
            bool digit = false;
            for (int i = 1; i < value.Length; i++)
                if (char.IsDigit(value[i])) digit = true;
                else if (value[i] != '_') return false;
            return digit;
        }

        public static string FlagDevNote(string id)
        {
            object manager = ExistingSingleton("Mortal.Core.MissionManagerData");
            Array collections = OptionalField(manager, "_flagsCollection") as Array;
            if (collections == null) return null;
            foreach (object collection in collections)
            {
                IList items = NameItems(collection, "Mortal.Core.FlagCollectionData");
                if (items == null) continue;
                foreach (object flag in items)
                {
                    if (flag == null || flag.GetType().FullName != "Mortal.Core.FlagData") continue;
                    // Unity Object.name is a read-only name boundary. Do not
                    // invoke GetFlag, its missing-item logger, or Instance.
                    string name;
                    try { name = LogBridge.ReadFieldStrict(flag, "name") as string; }
                    catch (Exception) { continue; }
                    if (string.Equals(name, id, StringComparison.Ordinal)) return OptionalField(flag, "_devNote") as string;
                }
            }
            return null;
        }

        // Read only the existing singleton field. The game's Instance getter
        // performs a Unity object search and writes the singleton on a miss.
        public static int ReadPlayerStat(string key)
        {
            Type managerType = GameType("Mortal.Core.PlayerStatManagerData");
            FieldInfo singleton = managerType.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            if (singleton == null) throw new MissingFieldException(managerType.FullName, "_instance");
            object manager = singleton.GetValue(null);
            if (manager == null) throw new DataNotReadyException();
            object stats = LogBridge.ReadFieldStrict(manager, "_stats");
            if (stats == null) throw new DataNotReadyException();
            if (stats.GetType().FullName != "Mortal.Core.PlayerStatData") throw new UnsupportedDisplayStatException();
            IEnumerable list = LogBridge.ReadFieldStrict(stats, "List") as IEnumerable;
            if (list == null) throw new DataNotReadyException();
            foreach (object stat in list)
            {
                if (stat == null) throw new InvalidOperationException("Invalid preview stat collection.");
                if (stat.GetType().FullName != "Mortal.Core.GameStat") throw new UnsupportedDisplayStatException();
                object kind = LogBridge.ReadFieldStrict(stat, "_type");
                if (kind == null || !kind.GetType().IsEnum) throw new InvalidOperationException("Invalid preview stat kind.");
                FieldInfo field = kind.GetType().GetField(kind.ToString());
                if (field == null) throw new InvalidOperationException("Invalid preview stat kind.");
                // CustomAttributeData inspects metadata without instantiating
                // an attribute or invoking an arbitrary conversion method.
                foreach (CustomAttributeData attribute in CustomAttributeData.GetCustomAttributes(field))
                {
                    if (attribute.AttributeType.FullName != "OBB.Framework.Attributes.StringValueAttribute" ||
                        attribute.ConstructorArguments.Count != 1) continue;
                    if (string.Equals(attribute.ConstructorArguments[0].Value as string, key, StringComparison.Ordinal))
                        return ReadFinalValue(stat);
                }
            }
            throw new InvalidOperationException("Preview stat was not found.");
        }

        public static int ReadFinalValue(object stat) { return SafeDisplayEvaluation.ReadStatFinalValue(stat); }
    }
}
