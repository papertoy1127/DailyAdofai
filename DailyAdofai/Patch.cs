using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ADOFAI;
using ByteSheep.Events;
using GDMiniJSON;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace DailyAdofai {
	[HarmonyPatch(typeof(scnCLS), "Awake")]
	internal static class ClsAwakePatch {
		private static void Postfix(scnCLS __instance) {
			__instance.StartCoroutine(GetLevels(__instance));
		}

		private static IEnumerator GetLevels(scnCLS __instance) {
			var www = UnityWebRequest.Get("https://daily.hyonsu.com/levels?version=0");
			yield return www.SendWebRequest();

			if (www.result is not UnityWebRequest.Result.ConnectionError and not UnityWebRequest.Result.ProtocolError) {
				var levels = www.downloadHandler.text.Split('\n');
				foreach (var level in levels) {
					var rootDict = Json.Deserialize(level) as Dictionary<string, object>;
					var settings = rootDict["settings"] as Dictionary<string, object>;
					var workshopId = settings["workshopId"] as string;
					var levelDataCLS = new LevelDataCLS();
					levelDataCLS.Setup();
					levelDataCLS.Decode(rootDict);
					Data.dailyLevels.Add(workshopId, levelDataCLS);
				}
			}
		}
	}

	[HarmonyPatch(typeof(scnCLS), "Start")]
	internal static class ClsStartPatch {
		public static Transform tileparent;
		public static scrFloor tileprefab;
		public static scrFloor tilecls;
		public static scrFloor tilefeatured;
		public static scrFloor tilequit;
		public static scrFloor tiledaily;

		public static GameObject portalcls;
		public static GameObject portalfeatured;
		public static GameObject portaldaily;

		public static void Postfix(scnCLS __instance) {
			Data.dailyMode = false;
			tileparent = GameObject.Find("initialPath").transform;
			var deletedFirst = false;
			foreach (Transform transform in tileparent) {
				if (transform.name != "tile") continue;
				if (tileprefab == null) {
					tileprefab = transform.GetComponent<scrFloor>();
				} else {
					if (!deletedFirst) {
						Object.Destroy(transform.gameObject);
						deletedFirst = true;
						continue;
					}
					var spriteRenderer = transform.GetComponent<SpriteRenderer>();
					spriteRenderer.color = new Color(0, 0, 0, 0);
				}
			}

			Trace.Assert(tileprefab != null);
			tilecls = tileparent.Find("WorkshopPortal").GetComponent<scrFloor>();
			tilefeatured = tileparent.Find("FeaturedPortal").GetComponent<scrFloor>();
			tilequit = tileparent.Find("QuitPortal").GetComponent<scrFloor>();

			portalcls = GameObject.Find("workshopPortal");
			portalfeatured = GameObject.Find("featuredPortal");

			var tile = tileprefab.transform;
			var workshop = tilecls.transform;
			var featured = tilefeatured.transform;

			tile.localPosition = new Vector3(0f, 0f, 0f);
			workshop.localPosition = new Vector3(0f, 1f, 0f);
			featured.localPosition = new Vector3(-4f, 1f, 0f);
			tilequit.transform.localPosition = new Vector3(0f, -1f, 0f);

			var pos1 = workshop.position;
			var pos2 = featured.position;

			portalcls.transform.position = pos1 with {y = pos1.y + 2.5f};
			portalfeatured.transform.position = pos2 with {y = pos2.y + 2.5f};
			portalcls.transform.localScale = Vector3.one * 0.65f;
			portalfeatured.transform.localScale = Vector3.one * 0.65f;

			portaldaily = Object.Instantiate(portalcls, tileparent);
			portaldaily.transform.position = pos1 with {x = 4f, y = pos1.y + 2.5f};
			var quad = portaldaily.GetComponentInChildren<PortalQuad>();
			quad.SetTexture(Assets.Thumbnail);
			var text = portaldaily.transform.GetComponentInChildren<Text>();
			text.text = "Daily";
			var text2 = portaldaily.transform.GetComponentInChildren<scrTextChanger>();
			text2.desktopText = "Daily";

			tiledaily = Object.Instantiate(tilefeatured.gameObject, tileparent).GetComponent<scrFloor>();
			tiledaily.transform.localPosition = new Vector3(4f, 1f, 0f);
			var func = tiledaily.GetComponent<ffxCallFunction>();
			func.ue = new QuickEvent();
			func.ue.AddListener(() => {
				Data.dailyMode = true;
				scnCLS.instance.WorkshopLevelsPortal();
			});
			
			func = tilequit.GetComponent<ffxCallFunction>();
			func.ue = new QuickEvent();
			func.ue.AddListener(() => {
				Process.Start("https://discord.gg/SWDpB5W678");
			});

			var quittext = GameObject.Find("QuitCanvas").transform.GetChild(0).GetComponent<Text>();
			quittext.text = "Daily ADOFAI Discord";

			var positions = new float[] {-4, -3, -2, -1, 1, 2, 3, 4};
			foreach (var position in positions) {
				var transform2 = Object.Instantiate(tile, tileparent);
				transform2.localPosition = new Vector3(position, 0f, 0f);
			}

			scrController.instance.chosenplanet.transform.position = new Vector3(0f, -2f, 0f);
			scrCamera.instance.positionState = PositionState.CLSIntro;
		}
	}

	[HarmonyPatch(typeof(scrController), "Update")]
	internal static class CLSUpdatePatch {
		public static void Prefix(scrController __instance) {
			var cls = scnCLS.instance;
			if (cls == null) return;
			if (cls.showingInitialMenu) {
				if (Input.GetKeyDown(KeyCode.Alpha1)) {
					scrController.instance.chosenplanet.transform.position = new Vector3(-4f, -2f, 0f);
				}

				if (Input.GetKeyDown(KeyCode.Alpha2)) {
					scrController.instance.chosenplanet.transform.position = new Vector3(0f, -2f, 0f);
				}

				if (Input.GetKeyDown(KeyCode.Alpha3)) {
					scrController.instance.chosenplanet.transform.position = new Vector3(4f, -2f, 0f);
				}
			}
		}
	}

	[HarmonyPatch(typeof(scrController), "CountValidKeysPressed")]
	internal static class CountValidKeysPressedPatch {
		private static bool Prefix(scrController __instance, ref int __result) {
			if (scnCLS.instance == null || !scnCLS.instance.showingInitialMenu || (!Input.GetKeyDown(KeyCode.Alpha1) && !Input.GetKeyDown(KeyCode.Alpha2) && !Input.GetKeyDown(KeyCode.Alpha3))) return true;
			__result = 0;
			return false;

		}
	}

	[HarmonyPatch(typeof(scrCamera), "Update")]
	internal static class CameraUpdatePatch {
		public static void Prefix(scrCamera __instance) {
			if (!ADOBase.isLevelEditor || !ADOBase.controller.paused || !__instance.followMode) {
				if (!GCS.d_freeroam && !Input.GetKey(KeyCode.Minus) && !Input.GetKey(KeyCode.Equals)) {
					if (__instance.positionState == (PositionState) 100) {
						__instance.topos.y = 0f;
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(scnCLS), "ScanLevels")]
	internal static class ScanLevelsPatch {
		private static bool Prefix(scnCLS __instance, CancellationToken cancelToken, bool workshop, bool local, out Task __result) {
			__result = ScanLevels(__instance, cancelToken, workshop, local);
			return false;
		}
		
		private static async Task ScanLevels(scnCLS cls, CancellationToken cancelToken, bool workshop = true, bool local = false) {
			var levelsDir = cls.get<string>("levelsDir");
			var featuredLevelsMode = cls.get<bool>("featuredLevelsMode");
			var isWorkshopLevel = cls.get<Dictionary<string, bool>>("isWorkshopLevel");
			var loadedLevelIsDeleted = cls.get<Dictionary<string, bool>>("loadedLevelIsDeleted");
			var loadedLevels = cls.get<Dictionary<string, GenericDataCLS>>("loadedLevels");
			var extraLevels = cls.get<Dictionary<string, GenericDataCLS>>("extraLevels");
			var loadedLevelDirs = cls.get<Dictionary<string, string>>("loadedLevelDirs");
			
			if (local && !Directory.Exists(levelsDir)) {
				Debug.LogWarning("First time launching CLS, making directory");
				RDDirectory.CreateDirectory(levelsDir);
			} else {
				if (featuredLevelsMode) {
					foreach (var extraLevel in extraLevels) {
						string key = extraLevel.Key;
						if (!loadedLevels.ContainsKey(key)) {
							loadedLevels.Add(key, extraLevel.Value);
							loadedLevelDirs.Add(key, null);
							loadedLevelIsDeleted[key] = false;
							isWorkshopLevel[key] = true;
						}
					}
				} else if (Data.dailyMode) {
					foreach (var keyValuePair in Data.dailyLevels) {
						string key = keyValuePair.Key;
						if (!loadedLevels.ContainsKey(key)) {
							loadedLevels.Add(key, keyValuePair.Value);
							loadedLevelDirs.Add(key, null);
							loadedLevelIsDeleted[key] = false;
							isWorkshopLevel[key] = true;
						}
					}
				} else {
					string[] second = Array.Empty<string>();
					string[] first = Array.Empty<string>();
					if (workshop) {
						second = new string[SteamWorkshop.resultItems.Count];
						for (int index = 0; index < SteamWorkshop.resultItems.Count; ++index) {
							second[index] = SteamWorkshop.resultItems[index].path;
							isWorkshopLevel[Path.GetFileName(second[index])] = true;
						}
					}

					if (local)
						first = Directory.GetDirectories(levelsDir);
					string[] itemDirs = first.Concat(second)
						.ToArray();
					cancelToken.ThrowIfCancellationRequested();
					List<Task<Dictionary<string, object>>> taskList = new List<Task<Dictionary<string, object>>>();
					foreach (string str in itemDirs) {
						string levelPath = Path.Combine(str, "main.adofai");
						string fileName = Path.GetFileName(str);
						bool flag = false;
						if (loadedLevelIsDeleted.ContainsKey(fileName))
							flag = loadedLevelIsDeleted[fileName];
						if (RDFile.Exists(levelPath) && !flag)
							taskList.Add(Task.Run(
								() =>
									Json.Deserialize(RDFile.ReadAllText(levelPath)) as Dictionary<string, object>,
								cancelToken));
						else if (!flag) {
							Debug.LogWarning("No level file at " + str + "!");
							taskList.Add(
								Task.FromResult((Dictionary<string, object>) null));
						}
					}

					cancelToken.ThrowIfCancellationRequested();
					Dictionary<string, object>[] dictionaryArray =
						await Task.WhenAll(
							taskList);
					cancelToken.ThrowIfCancellationRequested();
					for (int index = 0; index < itemDirs.Length; ++index) {
						string path = itemDirs[index];
						string fileName = Path.GetFileName(path);
						Dictionary<string, object> rootDict = dictionaryArray[index];
						if (rootDict != null) {
							LevelDataCLS levelDataCls = new LevelDataCLS();
							levelDataCls.Setup();
							if (levelDataCls.Decode(rootDict)) {
								loadedLevels.Add(fileName, levelDataCls);
								loadedLevelDirs.Add(fileName, path);
								loadedLevelIsDeleted[fileName] = false;
							}
						}
					}
				}

				cls.levelCount = loadedLevels.Count;
			}
		}
	}
}