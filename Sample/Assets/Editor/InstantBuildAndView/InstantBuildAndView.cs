using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace InstantBuild
{

    public class InstantBuildAndView : EditorWindow
    {
        static InstantBuildAndView currentWindow;

        static void BuildAssetBundles(BuildTarget buildTarget)
        {

            // １個だけ選択中許可
            if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length == 1)
            {
                // GUIDからAssetPathを得る
                var assetPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                Debug.Log("インスタントビルドを開始します。 " + assetPath);
                // インスタントビルド開始
                StartInstantBuild(assetPath, buildTarget);
            }
            else
            {
                Debug.LogWarning("ビルドしたい対象を1つ選んで実行してください。");
            }
        }

        // Projectビューの右クリックメニューに登録
        [MenuItem("Assets/即席ビルド/Android")]
        static void AndroidBuildAssetBundles()
        {
            BuildAssetBundles(BuildTarget.Android);
        }

        // Projectビューの右クリックメニューに登録
        [MenuItem("Assets/即席ビルド/iOS")]
        static void iOSBuildAssetBundles()
        {
            BuildAssetBundles(BuildTarget.iOS);
        }

        /// <summary>
        /// インスタントビルドの開始
        /// </summary>
        /// <param name="targetAssetPath"></param>
        /// <param name="buildTarget"></param>
        private static void StartInstantBuild(string targetAssetPath, BuildTarget buildTarget)
        {
            var dirPath = @"Temp/_InstantBuild/Android";

            // 検索用時間文字列
            var time = DateTime.Now.ToString("yyyyMMddHHmmss");
            // 開始タグ
            var startTag = "SINGLE BUILD START " + time;
            // 終了タグ
            var finishTag = "SINGLE BUILD FINISH " + time;
            // 開始タグを出す
            Debug.Log(startTag);

            // AssetBundle一つを作成する場合
            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];

            buildMap[0].assetBundleName = "InstantAssetBundle.unity3d";            // ビルドするだけなので名前は固定
            buildMap[0].assetBundleVariant = "";                                // とりあえずバリアントいらない
            buildMap[0].assetNames = new string[1] { targetAssetPath };         // 選択中のtargetのパスを渡す

            // 存在しない場合はディレクトリ作る
            if (!Directory.Exists(dirPath))
            {
                var dirInfo = Directory.CreateDirectory(dirPath);
                if (dirInfo == null) return;
            }

            // 強制リビルド（ForceRebuildAssetBundle）
            var manifest = BuildPipeline.BuildAssetBundles(dirPath, buildMap, BuildAssetBundleOptions.ForceRebuildAssetBundle, buildTarget);
            if (manifest == null)
            {
                throw new Exception("アセットバンドルの生成に失敗しました");
            }

			// 終了タグを出す
            Debug.Log(finishTag);

			#if UNITY_EDITOR_WIN
            // C:\Users\username\AppData\Local\Unity\Editor\Editor.log
            // Windowsの場合、AppData/Localのパスを取得
            var appLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // UnityEditorログのパス
            var unityEditorLogPath = "\\Unity\\Editor\\Editor.log";
			#else
			// ~/Library/Logs/Unity/Editor.log
			var appLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			// UnityEditorログのパス
			var unityEditorLogPath = "/Library/Logs/Unity/Editor.log";
			#endif

            // リードオンリーでストリームを開く
            FileStream fs = new FileStream(appLocalPath + unityEditorLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader sr = new StreamReader(fs);
            //
            var tmpBuff = new StringBuilder();
            //
            bool isTargetRange = false;
            int tagCount = 0;

            // Editor.logから読むために泥臭いことして読み込む
            for (;;)
            {
                // 1行ずつ読み込み
                var line = sr.ReadLine();
                // 終わったら終了
                if (line == null) break;
                // 開始行
                if (line.IndexOf(startTag) >= 0) isTargetRange = true;
                // 閉じ行
                if (line.IndexOf(finishTag) >= 0) isTargetRange = false;

                // 今回の出力範囲
                if (isTargetRange)
                {
                    // 出現回数をカウント
                    if (line.IndexOf("***Player size statistics***") >= 0) tagCount++;
                    // １回目～２回目の間をバッファに出力
                    if (tagCount == 1)
                    {
                        tmpBuff.AppendLine(line);
                    }
                }
            }

            // ウィンドウなかったら
            if (!currentWindow)
            {
                // ウィンドウをつくる
                currentWindow = GetWindow(typeof(InstantBuildAndView)) as InstantBuildAndView;
                Texture icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Editor/InstantBuildAndView/Icon/icon_ibv.png");
                currentWindow.titleContent = new GUIContent("BuildInfo", icon);
            }
            // ウィンドウ表示
            currentWindow.Show();
            // ウィンドウのバッファにログを与える
            currentWindow.SetViewLog(tmpBuff.ToString(), targetAssetPath);
            // 


        }

        /// <summary>
        /// ログ情報渡し
        /// </summary>
        /// <param name="log"></param>
        /// <param name="targetAssetPath"></param>
        public void SetViewLog(string log, string targetAssetPath)
        {
            // アセット名
            assetPathView = targetAssetPath;

            windowViewLog = log;
            // 詳細情報の構成
            info = new InstantBuildInfo(log);
        }

        bool isGraph = false;
        InstantBuildInfo info;
        string assetPathView = "";
        string windowViewLog = "";
        Vector2 scrollPosition;
        void OnGUI()
        {
            // 情報が無い場合は描かない
            if (info == null) return;

            // ヘッダの色帯
            Handles.DrawSolidRectangleWithOutline(new Rect(0, 0, EditorGUIUtility.currentViewWidth, 34), new Color(0.3f, 0.33f, 0.33f), Color.clear);

            GUIStyle guiStyle = new GUIStyle();
            guiStyle.fontSize = 12;
            guiStyle.normal.textColor = Color.white;
            // ファイル名とサイズの表示
            GUILayout.Label(" " + assetPathView, guiStyle);
            guiStyle.fontSize = 18;
            GUILayout.Label(" total size : " + info.SizeSumKB, guiStyle);
            //
            var startY = 40f;

            // グラフ表示指定の場合
            if (isGraph)
            {
                // 詳細情報GUI
                info.DrawOnGUI();
                startY = 220f;
            }

            // スクロールエリアを作る
            GUILayout.BeginArea(new Rect(0, startY, Screen.width, Screen.height));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height - startY));

            // グラフ表示のON/OFF
            isGraph = GUILayout.Toggle(isGraph, "詳細グラフの表示");

            // クリップボード転送ボタン
            if (GUILayout.Button("クリップボードに転送")) { EditorGUIUtility.systemCopyBuffer = windowViewLog; }
            // テキスト表示
            EditorGUILayout.TextArea(windowViewLog);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
