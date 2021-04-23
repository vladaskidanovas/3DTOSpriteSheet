using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;

public class SpriteSheetGenerator : MonoBehaviour {

    [SerializeField]
    [Tooltip ("Assign Game Object to proceed.")]
    private GameObject _gameObject = null;

    [SerializeField]
    private List<CaptureAngles> _captureAnglesList = new List<CaptureAngles> ();

    [SerializeField]
    [Tooltip ("Assign a camera to proceed")]
    public Camera _camera = null;

    [SerializeField]
    public List<ObjectAnimation> _objectAnimationList = new List<ObjectAnimation> ();

    [SerializeField]
    [Tooltip ("Number of the columns in sprite sheet")]
    private Int32 _columns = 5;

    [SerializeField]
    private Boolean _useFramesPerSecond = false;

    [SerializeField]
    [Tooltip ("Animation length (seconds)")]
    private float _animationLength = 1;

    [SerializeField]
    [Tooltip ("Frame Per Second. NOTE: If you use FPS property, cell count will be different in every sprite sheet if animation lengths are different.")]
    private Int32 _framesPerSecond = 30;

    [SerializeField]
    private Boolean _useCellCount = true;

    [SerializeField]
    [Tooltip ("Cell Count. NOTE: If you use cell count property, animation FPS will be different in every sprite sheet only if animations lengths are different.")]
    private Int32 _cellCount = 20;

    [SerializeField]
    [Tooltip ("Size of the cell in the sprite sheet.")]
    private Vector2Int _cellSize = new Vector2Int (128, 128);

    [SerializeField]
    [Tooltip ("Bake diffuse map.")]
    private Boolean _bakeDiffuseMap = true;

    [SerializeField]
    [Tooltip ("Bake normal map.")]
    private Boolean _bakeNormalMap = true;

    [SerializeField]
    private Int32 _currentFrame = 0;

    [SerializeField]
    private String _savePath = "";

    /// <summary>
    /// Capture animations and save to Texture2D
    /// </summary>
    /// <param name="DisplayProgress"></param>
    /// <returns></returns>
    public IEnumerator GenerateAnimationSpriteSheet (Action<float, float> DisplayProgress) {

        if (_gameObject == null || _camera == null) {
            Debug.LogWarning ("Game object or Capture Camera is not set");
            yield break;
        }

        if (!_bakeDiffuseMap && !_bakeNormalMap) {
            Debug.LogWarning ("Baking map not selected");
            yield break;
        }

        Shader worldSpaceNormalShader = Shader.Find ("Unlit/WorldSpaceNormal");
        Regex illegalInFileName = new Regex(@"[\\/:*?""<>|]");

        float totalProgress = CountToggledAngles (_captureAnglesList) * CountToggledAnimations (_objectAnimationList);
        float progress = 1;

        foreach (var angle in _captureAnglesList) {

            if (!angle.toggle) continue;

            String prefix = angle.prefix;

            if (prefix == "") {
                prefix = Convert.ToString (Math.Abs (angle.value.x)) + "_" + Convert.ToString (Math.Abs (angle.value.y)) + "_" + Convert.ToString (Math.Abs (angle.value.z));
            }

            _camera.transform.rotation = Quaternion.Euler (angle.value);
            Color defaultBackgroundColor = _camera.backgroundColor;
            
            int animationCount = _objectAnimationList.Count;
            if (_objectAnimationList.Count == 0) animationCount = 1;

            for (var i = 0; i < animationCount; i++) {

                AnimationClip animationClip = null;                    
                String spriteSheetName = illegalInFileName.Replace(_gameObject.name + "_" + prefix, "");

                if (_objectAnimationList.Count != 0)
                {

                    if (!_objectAnimationList[i].toggle) continue;

                    animationClip = _objectAnimationList[i].clip;
                    _animationLength = animationClip.length;
                    spriteSheetName = illegalInFileName.Replace(_gameObject.name + "_" + _objectAnimationList[i].clip.name + "_" + prefix, "");

                }

                if (_useFramesPerSecond) {
                    _cellCount = (int) (_animationLength * _framesPerSecond);
                }

                if (_useCellCount)
                {
                    //Nothing to see here, keep scrolling [' _ ']
                }
                
                Int32 rows = (int) (Math.Ceiling ((double) _cellCount / _columns));
                Vector2Int spriteSheetSize = new Vector2Int (_cellSize.x * _columns, _cellSize.y * rows);
                Vector2Int SpriteSheetPosition = new Vector2Int (0, spriteSheetSize.y - _cellSize.y);

                if (spriteSheetSize.x > 4096 || spriteSheetSize.y > 4096) {
                    Debug.LogErrorFormat ("Error attempting to capture an animation with a length and resolution that would produce a texture of size: {0}", spriteSheetSize);
                }

                Texture2D diffuseMap = new Texture2D (spriteSheetSize.x, spriteSheetSize.y, TextureFormat.ARGB32, false, false) {
                    filterMode = FilterMode.Point
                };

                ClearColor (diffuseMap, Color.clear);

                Texture2D normalMap = new Texture2D (spriteSheetSize.x, spriteSheetSize.y, TextureFormat.ARGB32, false, false) {
                    filterMode = FilterMode.Point
                };

                ClearColor (normalMap, new Color (0.5f, 0.5f, 1.0f, 0.0f));

                RenderTexture rtFrame = new RenderTexture (_cellSize.x, _cellSize.y, 24, RenderTextureFormat.ARGBFloat) {
                    filterMode = FilterMode.Point,
                    anisoLevel = 0,
                    antiAliasing = 1,
                    hideFlags = HideFlags.HideAndDontSave,
                };

                _camera.targetTexture = rtFrame;

                try {

                    for (_currentFrame = 0; _currentFrame < _cellCount; _currentFrame++) {
                        
                        if (animationClip != null) animationClip.SampleAnimation (_gameObject, (_currentFrame / (float) _cellCount) * animationClip.length);

                        yield return null;

                        if (_bakeDiffuseMap) {
                            _camera.backgroundColor = Color.clear;
                            _camera.Render ();
                            Graphics.SetRenderTarget (rtFrame);
                            diffuseMap.ReadPixels (new Rect (0, 0, rtFrame.width, rtFrame.height), SpriteSheetPosition.x, SpriteSheetPosition.y);
                            diffuseMap.Apply ();
                        } else {
                            diffuseMap = null;
                        }

                        if (_bakeNormalMap) {
                            _camera.backgroundColor = new Color (0.5f, 0.5f, 1.0f, 0.0f);
                            _camera.RenderWithShader (worldSpaceNormalShader, "");
                            Graphics.SetRenderTarget (rtFrame);
                            normalMap.ReadPixels (new Rect (0, 0, rtFrame.width, rtFrame.height), SpriteSheetPosition.x, SpriteSheetPosition.y);
                            normalMap.Apply ();
                        } else {
                            normalMap = null;
                        }

                        SpriteSheetPosition.x += _cellSize.x;

                        if ((_currentFrame + 1) % _columns == 0) {
                            SpriteSheetPosition.x = 0;
                            SpriteSheetPosition.y -= _cellSize.y;
                        }

                    }

                    SaveSpriteSheet (diffuseMap, normalMap, spriteSheetName);

                } finally {
                    Graphics.SetRenderTarget (null);
                    _camera.targetTexture = null;
                    _camera.backgroundColor = defaultBackgroundColor;
                    DestroyImmediate (rtFrame);
                }

                DisplayProgress.Invoke (progress, totalProgress);
                progress++;


            }

        }

    }

    /// <summary>
    /// Save Generated Texture2D to png
    /// </summary>
    /// <param name="diffuseMap"></param>
    /// <param name="normalMap"></param>
    /// <param name="fileName"></param>
    private void SaveSpriteSheet (Texture2D diffuseMap, Texture2D normalMap, string fileName) {
        if (String.IsNullOrEmpty (_savePath)) return;

        String normalPath = string.Format ("{0}/{1}{2}.{3}", _savePath, fileName, "_NormalMap", "png");
        String diffusePath = string.Format ("{0}/{1}{2}.{3}", _savePath, fileName, "_DiffuseMap", "png");

        if (diffuseMap != null) File.WriteAllBytes (diffusePath, diffuseMap.EncodeToPNG ());
        if (normalMap != null) File.WriteAllBytes (normalPath, normalMap.EncodeToPNG ());
    }

    /// <summary>
    /// Clear Color
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="color"></param>
    private void ClearColor (Texture2D texture, Color color) {
        Color[] pixels = new Color[texture.width * texture.height];
        for (Int32 i = 0; i < pixels.Length; i++) {
            pixels[i] = color;
        }
        texture.SetPixels (pixels);
        texture.Apply ();
    }

    /// <summary>
    /// Check Toggled Cpature Angles in List
    /// </summary>
    /// <param name="list"></param>
    /// <returns>Int32</returns>
    private Int32 CountToggledAngles (List<CaptureAngles> list) {
        Int32 toggled = 0;
        foreach (var item in list) {
            if (!item.toggle) continue;

            toggled++;
        }
        return toggled;
    }

    /// <summary>
    /// Check Toggled Animations in list
    /// </summary>
    /// <param name="list"></param>
    /// <returns>Int32</returns>
    private Int32 CountToggledAnimations (List<ObjectAnimation> list) {
        Int32 toggled = 0;
        foreach (var item in list) {
            if (!item.toggle) continue;

            toggled++;
        }
        return toggled;
    }

}