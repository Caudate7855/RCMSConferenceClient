using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using VIAVR.Scripts.UI.Utils;

namespace VIAVR.Scripts.UI.Editor
{
	[CanEditMultipleObjects, CustomEditor(typeof(NonDrawingGraphic), false)]
	public class NonDrawingGraphicEditor : GraphicEditor
	{
		public override void OnInspectorGUI ()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_Script, new GUILayoutOption[0]);
			// skipping AppearanceControlsGUI
			RaycastControlsGUI();
			serializedObject.ApplyModifiedProperties();
		}
	}
}
