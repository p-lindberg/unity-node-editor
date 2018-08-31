using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DataDesigner
{
	public class ZoomableEditorWindow : EditorWindow
	{
		const float kZoomMin = 0.1f;
		const float kZoomMax = 10.0f;
		protected Rect zoomAreaRect = new Rect();
		protected float zoom = 1.0f;
		protected Vector2 zoomAreaOrigin = Vector2.zero;

		protected virtual Texture2D Background { get { return null; } }

		public static T Init<T>(string windowName) where T : ZoomableEditorWindow
		{
			T window = EditorWindow.GetWindow<T>(windowName);
			window.Show();
			return window;
		}

		public Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
		{
			return (screenCoords - zoomAreaRect.TopLeft()) / zoom - zoomAreaOrigin;
		}

		protected virtual void DrawZoomAreaContents(Vector2 origin)
		{
			for (int i = 0; i < 20; i++)
			{
				for (int j = 0; j < 20; j++)
				{
					GUI.Box(new Rect(origin.x + i * 210.0f, origin.y + j * 210.0f, 200.0f, 200.0f), "Box");
				}
			}
		}

		protected virtual void DrawUtilityBarContents()
		{
			if (GUILayout.Button("Home", EditorStyles.toolbarButton))
			{
				zoomAreaOrigin = Vector2.zero;
				zoom = 1;
			}
		}

		void DrawZoomArea()
		{
			zoomAreaRect.Set(0, 17, position.width, position.height - 17);

			if (Background != null)
			{
				var uvOffset = new Vector2(-zoomAreaOrigin.x / Background.width,
					              (zoomAreaOrigin.y - zoomAreaRect.height / zoom) / Background.height);

				var uvSize = new Vector2(zoomAreaRect.width / (Background.width * zoom),
					            zoomAreaRect.height / (Background.height * zoom));

				GUI.DrawTextureWithTexCoords(zoomAreaRect, Background, new Rect(uvOffset, uvSize));
			}

			EditorZoomArea.Begin(zoom, zoomAreaRect);
			DrawZoomAreaContents(zoomAreaOrigin);
			EditorZoomArea.End();
		}

		void DrawUtilityBar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			DrawUtilityBarContents();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		void HandleEvents()
		{
			if (Event.current.type == EventType.ScrollWheel)
			{
				Vector2 screenCoordsMousePos = Event.current.mousePosition;
				Vector2 delta = Event.current.delta;
				Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(screenCoordsMousePos);
				float zoomDelta = -delta.y / 150.0f;
				float oldZoom = zoom;
				zoom += zoomDelta;
				zoom = Mathf.Clamp(zoom, kZoomMin, kZoomMax);
				zoomAreaOrigin -= (zoomCoordsMousePos + zoomAreaOrigin) - (oldZoom / zoom) * (zoomCoordsMousePos + zoomAreaOrigin);

				Event.current.Use();
			}
			else if (Event.current.type == EventType.MouseDrag &&
			        ((Event.current.button == 0 && Event.current.modifiers == EventModifiers.Alt) || Event.current.button == 2))
			{
				Vector2 delta = Event.current.delta;
				delta /= zoom;
				zoomAreaOrigin += delta;
				Event.current.Use();
			}

			OnHandleEvents();
		}

		protected virtual void OnHandleEvents()
		{
			Debug.Log("ZoomableEditorWindow");
		}

		public void OnGUI()
		{
			DrawZoomArea();
			DrawUtilityBar();
			HandleEvents();
		}
	}
}