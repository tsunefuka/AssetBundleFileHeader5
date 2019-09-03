using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;


namespace InstantBuild
{
    public class InstantBuildInfo
    {
        // キーバリューリスト
        Dictionary<string, float> datas = new Dictionary<string, float>();

        // 合計容量を返す
        public float SizeSum
        {
            get
            {
                return datas.Sum(x => x.Value);
            }
        }

        public string SizeSumKB
        {
            get
            {
                return SizeSum.ToString("0.0") + " kb";
            }
        }

        /// <summary>
        /// ログ文字列から情報を起こす
        /// </summary>
        /// <param name="log"></param>
        public InstantBuildInfo(string log)
        {
            // データ初期化
            datas = new Dictionary<string, float>();

            // ピックアップしたい要素
            var picks = new string[] { "Textures", "Meshes", "Animations", "Sounds", "Shaders", "Other Assets", "Levels", "Scripts", "Included DLLs", "File headers", "Complete size" };

            // 改行で分割して配列へ
            string[] lines = log.Split('\n');
            // 行数ぶん実行
            foreach (var line in lines)
            {
                // ピックアップ要素対象
                foreach (var pick in picks)
                {
                    // 前方一致で必要なキーワード検索
                    if (line.StartsWith(pick))
                    {
                        // スペースを含んだピックアップ要素があるので消しておく
                        var tmp = line.Replace(pick, "");
                        // 空白で分解
                        var splits = tmp.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        // 有効な配列が取れたら
                        if (splits.Length >= 1)
                        {
                            var key = pick;
                            var val = float.Parse(splits[0]);
                            if (splits[1].Contains("mb")) val *= 1000f; // MB単位のデータがあった場合は値を1000倍
                            datas.Add(key, val);
                        }
                    }
                }
            }

            Debug.Log("合計容量 " + SizeSum);
        }

        public void DrawOnGUI()
        {
            /*
            ProgressBar("TOTAL", SizeSum, 2000f);

            var keys = new string[] { "Textures", "Meshes", "Animations" };
            // 必要な情報をグラフ化して表示
            foreach (var k in keys)
            {
                ProgressBar(k, datas[k] + 10f, 1000f, 0.5f);
            }
            */

            GraphAll(20f, 2000f);

        }

        /*
        // Custom GUILayout progress bar.
        void ProgressBar(string label, float value, float max, float scale = 1.0f)
        {
            // Get a rect for the progress bar using the same margins as a textfield:
            Rect rect = GUILayoutUtility.GetRect(18, 16, "TextField");
            rect.width *= scale;
            EditorGUI.ProgressBar(rect, value / max, label + " " + value + " KB");
        }
        */


        Gradient prColor;
        Gradient PressureColor
        {
            get
            {
                if (prColor == null)
                {
                    var subCol = new Color(0.3f, 0.2f, 0.2f);

                    var cols = new List<GradientColorKey>();
                    cols.Add(new GradientColorKey(Color.blue + subCol, 0f));
                    cols.Add(new GradientColorKey(Color.green + subCol, 0.5f));
                    cols.Add(new GradientColorKey(Color.yellow + subCol, 0.75f));
                    cols.Add(new GradientColorKey(Color.red + subCol, 1f));

                    prColor = new Gradient();
                    prColor.colorKeys = cols.ToArray();
                }

                return prColor;
            }
        }

        // 上限サイズ初期値
        int limitSizeKb = 1000;
        // 上限サイズ(MB)
        string LimitSizeMb
        {
            get
            {
                return (limitSizeKb / 1000f).ToString("0.0") + " MB";
            }
        }

        /// <summary>
        /// アセット容量グラフを表示
        /// </summary>
        /// <param name="limitKB"></param>
        void GraphAll(float startY, float limitKB = 2000f)
        {
            // 

            // 上限表示
            GUI.Label(new Rect(EditorGUIUtility.currentViewWidth - 120, 0, 200, 16), "サイズ上限 " + LimitSizeMb);
            // 上限設定
            limitSizeKb = (int)GUI.HorizontalSlider(new Rect(EditorGUIUtility.currentViewWidth - 120, 16, 100, 16), limitSizeKb / 100, 1, 20) * 100;

            // バーオフセット
            var offsetY = 40f;
            var stepY = 16f;
            var barY = 14f;
            var barStartX = 100;

            // １帯グラフ
            foreach (var d in datas)
            {

                //    // カラーとる
                //     var tmpCol = ViewColors[d.Key];


                // バーの長さを求める
                int barW = (int)(d.Value / limitSizeKb * (EditorGUIUtility.currentViewWidth - 150));
                // リミットの長さを求める
                int limitW = (int)((limitSizeKb) / limitSizeKb * (EditorGUIUtility.currentViewWidth - 150));
                // 割合の算出
                float rate = (float)barW / (float)limitW;
                // 色調整
                var barColor = PressureColor.Evaluate(rate);



                var backRect = new Rect(barStartX, offsetY, limitW, barY);
                var labelRect = new Rect(0, offsetY, 100, barY);
                var sizeRect = new Rect(barStartX, offsetY, 100, barY);
                var barRect = new Rect(barStartX, offsetY, barW, barY);
                var limitRect = new Rect(barStartX + limitW, offsetY, 55, barY);

                // 黒下地を描画
                Handles.DrawSolidRectangleWithOutline(backRect, new Color(0.30f, 0.30f, 0.30f), Color.clear);

                // ラベル
                GUI.Label(labelRect, d.Key);

                // バーを描画
                Handles.DrawSolidRectangleWithOutline(barRect, barColor, Color.clear);

                GUI.color = Color.black;
                // 容量表示
                GUI.Label(sizeRect, d.Value.ToString("0.0") + " kb");
                GUI.color = Color.white;

                // リミット表示
                GUI.Label(limitRect, LimitSizeMb);

                // オフセットずらす
                offsetY += stepY;

            }
        }


    }
}


