using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace GISTech.GISTerrainLoader
{
    public class VerticalBlock : IDisposable
    {
        private bool _started = false;

        public VerticalBlock(params GUILayoutOption[] options)
        {
            try
            {
                GUILayout.BeginVertical(options);
                _started = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("BeginVertical failed: " + ex.Message);
            }
        }

        public VerticalBlock(GUIStyle style, params GUILayoutOption[] options)
        {
            try
            {
                GUILayout.BeginVertical(style, options);
                _started = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("BeginVertical (styled) failed: " + ex.Message);
            }
        }

        public void Dispose()
        {
            if (_started)
            {
                GUILayout.EndVertical();
            }
        }
    }

    public class HorizontalBlock : IDisposable
    {
        private bool _started = false;

        public HorizontalBlock(params GUILayoutOption[] options)
        {
            try
            {
                GUILayout.BeginHorizontal(options);
                _started = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("BeginHorizontal failed: " + ex.Message);
            }
        }

        public HorizontalBlock(GUIStyle style, params GUILayoutOption[] options)
        {
            try
            {
                GUILayout.BeginHorizontal(style, options);
                _started = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("BeginHorizontal (styled) failed: " + ex.Message);
            }
        }

        public void Dispose()
        {
            if (_started)
            {
                GUILayout.EndHorizontal();
            }
        }
    }

    public class ScrollviewBlock : IDisposable
    {
        private bool _started = false;

        public ScrollviewBlock(ref Vector2 scrollPos, params GUILayoutOption[] options)
        {
            try
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos, options);
                _started = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("BeginScrollView failed: " + ex.Message);
            }
        }

        public void Dispose()
        {
            if (_started)
            {
                GUILayout.EndScrollView();
            }
        }
    }

    public class ColoredBlock : IDisposable
    {
        private readonly Color _original;

        public ColoredBlock(Color color)
        {
            _original = GUI.color;
            GUI.color = color;
        }

        public void Dispose()
        {
            GUI.color = _original;
        }
    }

    [Serializable]
    public class TabsBlock
    {
        private Dictionary<string, Action> methods;
        private Action currentGuiMethod;
        public int curMethodIndex = -1;

        public TabsBlock(Dictionary<string, Action> _methods)
        {
            methods = _methods;
            SetCurrentMethod(0);
        }

        public void Draw()
        {
            var keys = methods.Keys.ToArray();
            using (new VerticalBlock(EditorStyles.helpBox))
            {
                using (new HorizontalBlock())
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        var btnStyle = i == 0 ? EditorStyles.miniButtonLeft : i == (keys.Length - 1) ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
                        using (new ColoredBlock(currentGuiMethod == methods[keys[i]] ? Color.grey : Color.white))
                        {
                            if (GUILayout.Button(keys[i], btnStyle))
                                SetCurrentMethod(i);
                        }
                    }
                }

                GUILayout.Label(keys[curMethodIndex], EditorStyles.centeredGreyMiniLabel);

                if (currentGuiMethod != null)
                {
                    try
                    {
                        currentGuiMethod();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("TabsBlock method failed: " + ex.Message);
                    }
                }
            }
        }

        public void SetCurrentMethod(int index)
        {
            if (index >= 0 && index < methods.Count)
            {
                curMethodIndex = index;
                currentGuiMethod = methods[methods.Keys.ToArray()[index]];
            }
            else
            {
                Debug.LogWarning("Invalid tab index: " + index);
            }
        }
    }
}
