using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Blynxy.Helper;

namespace Blynxy.Tools {
	[ExecuteInEditMode]
	public class SpriteInfo : MonoBehaviour {
		// Start is called before the first frame update

		private List<SpriteRenderer> m_SRs = new List<SpriteRenderer>();
		public List<SpriteRenderer> errorSRs = new List<SpriteRenderer>();

		public bool showSortingOrder = true;
		public Gradient gradient = new Gradient();
		GradientColorKey[] colorKey;
		GradientAlphaKey[] alphaKey;
		[SerializeField] private float minZpos;
		[SerializeField] private float maxZpos;

		[SerializeField] private Color SortingOrderBackground = Color.black;
		private Texture2D t2d;

		void Start() {
			var SRs = gameObject.GetComponentsInChildren<SpriteRenderer>();
			foreach (var sr in SRs) {
				m_SRs.Add(sr);
			}

			colorKey = new GradientColorKey[2];
			colorKey[0].color = Color.red;
			colorKey[0].time = 0.0f;
			colorKey[1].color = Color.green;
			colorKey[1].time = 1.0f;

			// Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
			alphaKey = new GradientAlphaKey[2];
			alphaKey[0].alpha = 1.0f;
			alphaKey[0].time = 0.0f;
			alphaKey[1].alpha = 1f;
			alphaKey[1].time = 1.0f;

			gradient.SetKeys(colorKey, alphaKey);

			Debug.Log(m_SRs.Count);
		}


		private void OnDrawGizmos() {
			foreach (SpriteRenderer spriteRenderer in errorSRs) {
				Gizmos.color = new Color(1, 0, 0, .5f);
				Gizmos.DrawCube(spriteRenderer.bounds.center,
					spriteRenderer.size * spriteRenderer.transform.lossyScale);
			}

			if (showSortingOrder) {
				foreach (var sr in m_SRs) {
					// Handles.Label(sr.transform.position, sr.sortingOrder.ToString(),style );
					var color = gradient.Evaluate(NormalizeMeDaddy(sr.transform.position.z));
					GUI.skin.label.normal.background = t2d;
					GizmosUtils.DrawText(GUI.skin, sr.sortingOrder.ToString(), sr.transform.position, color, 26, 10);
				}
			}
		}

		public float NormalizeMeDaddy(float x) {
			var v = (x - minZpos) / (maxZpos - minZpos);
			return v;
		}


		private void Update() {
			List<float> ZPositions = new List<float>();
			Dictionary<float, List<SpriteRenderer>> SRbyZPosition = new Dictionary<float, List<SpriteRenderer>>();

			List<SpriteRenderer> SRs = new List<SpriteRenderer>();

			float min_z = float.MinValue;
			float max_z = float.MaxValue;

			foreach (var sr in m_SRs) {
				float currentZ = sr.transform.position.z;

				if (!SRbyZPosition.ContainsKey(currentZ)) {
					// new z position
					SRbyZPosition.Add(currentZ, new List<SpriteRenderer>());
					SRbyZPosition[currentZ].Add(sr);
					ZPositions.Add(currentZ);
				}
				else {
					// add to existing
					SRbyZPosition[currentZ].Add(sr);
				}

				if (min_z == float.MinValue || currentZ < min_z) {
					min_z = currentZ;
				}

				if (max_z == float.MaxValue || currentZ > max_z) {
					max_z = currentZ;
				}
			}

			maxZpos = max_z;
			minZpos = min_z;

			ZPositions.Sort((a, b) => a.CompareTo(b));


			List<int> pSO = new List<int>();

			foreach (var zPosition in ZPositions) {
				int currentHighestSortingOrder = int.MinValue;

				foreach (SpriteRenderer sr in SRbyZPosition[zPosition]) {
					if (sr.sortingOrder > currentHighestSortingOrder)
						currentHighestSortingOrder = sr.sortingOrder;
					if (pSO.Count > 0) {
						foreach (int previousSortingOrder in pSO) {
							if (sr.sortingOrder > previousSortingOrder) {
								// Debug.Log(sr.name);
								if (!SRs.Contains(sr))
									SRs.Add(sr);
							}
						}
					}
				}

				pSO.Add(currentHighestSortingOrder);
			}

			errorSRs = SRs;
			t2d = new Texture2D(1, 1);
			t2d.SetPixel(0, 0, SortingOrderBackground);
			t2d.Apply();
		}
	}
}