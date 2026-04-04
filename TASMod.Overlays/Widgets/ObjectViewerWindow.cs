using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ImGuiVector2 = System.Numerics.Vector2;

namespace TASMod.Overlays.Widgets
{
    public class ObjectViewerWindow
    {
        public static object selectedObject;
        private static readonly HashSet<string> pinnedFields = new HashSet<string>();
        private static bool isPinnedFieldsSectionOpen = true;
        private static readonly Dictionary<Type, TypeFieldCache> typeFieldCache = new Dictionary<Type, TypeFieldCache>();

        private sealed class TypeFieldCache
        {
            public List<FieldInfo> PublicFields;
            public List<FieldInfo> PrivateFields;
            public List<FieldInfo> StaticFields;
            public Dictionary<string, FieldInfo> FieldsByName;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        public static void Set(object obj)
        {
            selectedObject = obj;
        }

        public static void Draw()
        {
            ImGui.SetNextWindowPos(new ImGuiVector2(10, 10), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new ImGuiVector2(200, 60), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.9f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 5.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3.0f);
            if (!ImGui.Begin("Object Viewer"))
            {
                ImGui.End();
                ImGui.PopStyleVar(2);
                return;
            }

            if (selectedObject != null)
            {
                DrawPinnedFields(selectedObject);
                if (pinnedFields.Count > 0)
                {
                    ImGui.Separator();
                }
                var recursionPath = new HashSet<object>(ReferenceEqualityComparer.Instance);
                DrawMember("selectedObject", selectedObject, "root", recursionPath, string.Empty);
            }
            else
            {
                ImGui.Text("No object selected.");
            }
            ImGui.End();
            ImGui.PopStyleVar(2);
        }

        private static void DrawPinnedFields(object rootObject)
        {
            if (pinnedFields.Count == 0)
            {
                return;
            }

            if (ImGui.SmallButton("Clear All Pins"))
            {
                pinnedFields.Clear();
                return;
            }

            ImGui.SetNextItemOpen(isPinnedFieldsSectionOpen, ImGuiCond.Always);
            bool isOpen = ImGui.TreeNode($"Pinned Fields ({pinnedFields.Count})##pinned-fields");
            isPinnedFieldsSectionOpen = isOpen;
            if (isOpen)
            {
                foreach (string path in pinnedFields.OrderBy(path => path).ToList())
                {
                    ImGui.PushID($"pinned-{path}");
                    if (ImGui.SmallButton("Unpin"))
                    {
                        pinnedFields.Remove(path);
                        ImGui.PopID();
                        continue;
                    }
                    ImGui.SameLine();

                    if (TryResolvePath(rootObject, path, out object resolvedValue))
                    {
                        var recursionPath = new HashSet<object>(ReferenceEqualityComparer.Instance);
                        DrawMember(path, resolvedValue, $"pinned/{path}", recursionPath, path);
                    }
                    else
                    {
                        ImGui.Text($"{path}: <unresolved>");
                    }

                    ImGui.PopID();
                }
                ImGui.TreePop();
            }
        }

        private static void DrawMember(string name, object value, string nodeId, HashSet<object> recursionPath, string memberPath)
        {
            if (value == null)
            {
                ImGui.Text($"{name}: null");
                return;
            }

            Type type = value.GetType();
            if (IsSimple(type))
            {
                ImGui.Text($"{name} ({type.Name}): {FormatSimple(value)}");
                return;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                int? count = TryGetEnumerableCount(value);
                string listLabel = count.HasValue
                    ? $"{name} [{type.Name}] Count={count.Value}##{nodeId}"
                    : $"{name} [{type.Name}]##{nodeId}";
                if (ImGui.TreeNode(listLabel))
                {
                    int index = 0;
                    foreach (object item in enumerable)
                    {
                        string itemPath = string.IsNullOrEmpty(memberPath)
                            ? $"[{index}]"
                            : $"{memberPath}[{index}]";
                        DrawMember($"[{index}]", item, $"{nodeId}/{index}", recursionPath, itemPath);
                        index++;
                    }

                    if (index == 0)
                    {
                        ImGui.Text("(empty)");
                    }

                    ImGui.TreePop();
                }
                return;
            }

            if (recursionPath.Contains(value))
            {
                ImGui.Text($"{name} ({type.Name}): <circular reference>");
                return;
            }

            string objectLabel = $"{name} [{type.Name}]##{nodeId}";
            if (!ImGui.TreeNode(objectLabel))
            {
                return;
            }

            recursionPath.Add(value);
            DrawGroupedFields(value, type, nodeId, recursionPath, memberPath);

            recursionPath.Remove(value);
            ImGui.TreePop();
        }

        private static void DrawGroupedFields(object owner, Type type, string nodeId, HashSet<object> recursionPath, string memberPath)
        {
            TypeFieldCache cache = GetTypeFieldCache(type);

            DrawFieldGroup("Public Fields", cache.PublicFields, owner, nodeId, recursionPath, memberPath);
            DrawFieldGroup("Private Fields", cache.PrivateFields, owner, nodeId, recursionPath, memberPath);
            DrawFieldGroup("Static Fields", cache.StaticFields, owner, nodeId, recursionPath, memberPath);
        }

        private static void DrawFieldGroup(string groupName, List<FieldInfo> fields, object owner, string nodeId, HashSet<object> recursionPath, string memberPath)
        {
            if (fields.Count == 0)
            {
                return;
            }

            if (!ImGui.TreeNode($"{groupName} ({fields.Count})##{nodeId}/{groupName}"))
            {
                return;
            }

            foreach (FieldInfo field in fields)
            {
                string fieldPath = string.IsNullOrEmpty(memberPath)
                    ? field.Name
                    : $"{memberPath}.{field.Name}";
                bool isPinned = pinnedFields.Contains(fieldPath);

                ImGui.PushID($"{nodeId}.{field.Name}#pin");
                if (ImGui.SmallButton(isPinned ? "Unpin" : "Pin"))
                {
                    if (isPinned)
                    {
                        pinnedFields.Remove(fieldPath);
                    }
                    else
                    {
                        pinnedFields.Add(fieldPath);
                    }
                }
                ImGui.SameLine();

                object fieldValue;
                try
                {
                    fieldValue = field.GetValue(field.IsStatic ? null : owner);
                }
                catch (Exception ex)
                {
                    ImGui.Text($"{field.Name}: <error: {ex.GetType().Name}>");
                    ImGui.PopID();
                    continue;
                }

                DrawMember(field.Name, fieldValue, $"{nodeId}.{field.Name}", recursionPath, fieldPath);
                ImGui.PopID();
            }

            ImGui.TreePop();
        }

        private static TypeFieldCache GetTypeFieldCache(Type type)
        {
            if (typeFieldCache.TryGetValue(type, out TypeFieldCache cached))
            {
                return cached;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            List<FieldInfo> fields = new List<FieldInfo>();
            Dictionary<string, FieldInfo> fieldsByName = new Dictionary<string, FieldInfo>();
            for (Type current = type; current != null; current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(flags))
                {
                    fields.Add(field);
                    if (!fieldsByName.ContainsKey(field.Name))
                    {
                        fieldsByName.Add(field.Name, field);
                    }
                }
            }

            cached = new TypeFieldCache
            {
                PublicFields = fields.Where(field => !field.IsStatic && field.IsPublic).OrderBy(field => field.Name).ToList(),
                PrivateFields = fields.Where(field => !field.IsStatic && !field.IsPublic).OrderBy(field => field.Name).ToList(),
                StaticFields = fields.Where(field => field.IsStatic).OrderBy(field => field.Name).ToList(),
                FieldsByName = fieldsByName
            };
            typeFieldCache[type] = cached;

            return cached;
        }

        private static int? TryGetEnumerableCount(object value)
        {
            if (value is ICollection collection)
            {
                return collection.Count;
            }

            Type type = value.GetType();
            Type iCollectionGeneric = type
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (iCollectionGeneric == null)
            {
                return null;
            }

            PropertyInfo countProperty = iCollectionGeneric.GetProperty("Count");
            if (countProperty == null)
            {
                return null;
            }

            object countValue = countProperty.GetValue(value);
            return countValue as int? ?? (countValue is int count ? count : null);
        }

        private static bool IsSimple(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid);
        }

        private static string FormatSimple(object value)
        {
            return value switch
            {
                string s => string.IsNullOrEmpty(s) ? "\"\"" : s,
                char c => c.ToString(),
                _ => value.ToString() ?? "<null>"
            };
        }

        private static bool TryResolvePath(object rootObject, string path, out object value)
        {
            value = rootObject;
            if (value == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(path))
            {
                return true;
            }

            string[] segments = path.Split('.');
            foreach (string rawSegment in segments)
            {
                string segment = rawSegment;
                if (string.IsNullOrEmpty(segment))
                {
                    return false;
                }

                int bracketStart = segment.IndexOf('[');
                string fieldName = bracketStart >= 0 ? segment.Substring(0, bracketStart) : segment;
                if (!string.IsNullOrEmpty(fieldName))
                {
                    FieldInfo field = FindField(value.GetType(), fieldName);
                    if (field == null)
                    {
                        return false;
                    }

                    value = field.GetValue(field.IsStatic ? null : value);
                    if (value == null && segment.Contains("["))
                    {
                        return false;
                    }
                }

                int indexCursor = bracketStart;
                while (indexCursor >= 0 && indexCursor < segment.Length)
                {
                    int closeBracket = segment.IndexOf(']', indexCursor + 1);
                    if (closeBracket < 0)
                    {
                        return false;
                    }

                    string indexText = segment.Substring(indexCursor + 1, closeBracket - indexCursor - 1);
                    if (!int.TryParse(indexText, out int index))
                    {
                        return false;
                    }
                    if (value is not IEnumerable enumerable || value is string)
                    {
                        return false;
                    }

                    int currentIndex = 0;
                    bool found = false;
                    foreach (object item in enumerable)
                    {
                        if (currentIndex == index)
                        {
                            value = item;
                            found = true;
                            break;
                        }
                        currentIndex++;
                    }
                    if (!found)
                    {
                        return false;
                    }

                    indexCursor = segment.IndexOf('[', closeBracket + 1);
                }

                if (value == null)
                {
                    break;
                }
            }

            return true;
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            TypeFieldCache cache = GetTypeFieldCache(type);
            if (cache.FieldsByName.TryGetValue(fieldName, out FieldInfo field))
            {
                return field;
            }

            return null;
        }
    }
}