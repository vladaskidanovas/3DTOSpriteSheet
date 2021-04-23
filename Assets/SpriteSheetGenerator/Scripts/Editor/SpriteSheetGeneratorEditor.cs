using System;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor (typeof (SpriteSheetGenerator))]
public class SpriteSheetGeneratorEditor : Editor {
    private const String ASSIGN_REFS_INFO = "Assign the Game Object to proceed.";
    private const String ASSIGN_CAMERA_INFO = "Assign a camera to proceed.";
    private const String ADD_ANIMATION_INFO = "Game Object doesn’t have any animations.";
    private const String ADD_ANGLE_INFO = "Add an capturing angle.";
    private const String SELECT_ANIMATION = "Select at least one animation from the list.";
    private const String SELECT_ANGLE = "Select at least one capturing angle from the list.";
    private const String LEGACY_ANIM_INFO = "{0} object doesn't have an animation component, capturing will be in non-animated mode.";
    private const String SELECT_ONE_BAKE_WARN = "Select at least one baking map.";
private IEnumerator routine;
    private ReorderableList animationReorderableList;
    private ReorderableList angleReorderableList;
    private AnimationClip selectedAnimation;
    
    private SerializedProperty gameObject;
    private SerializedProperty objectAnimationListProp;
    private SerializedProperty captureAnglesListProp;
    private SerializedProperty captureCameraProp;
    private SerializedProperty columnsProp;
    private SerializedProperty animationLength;
    private SerializedProperty useFramesPerSecondProp;
    private SerializedProperty framesPerSecondProp;
    private SerializedProperty useCellCountProp;
    private SerializedProperty cellCountProp;
    private SerializedProperty cellSizeProp;
    private SerializedProperty diffuseMapProp;
    private SerializedProperty normalMapProp;
    private SerializedProperty savePath;

    private void OnEnable () {
        
        gameObject = serializedObject.FindProperty ("_gameObject");

        objectAnimationListProp = serializedObject.FindProperty("_objectAnimationList"); 
        captureAnglesListProp = serializedObject.FindProperty("_captureAnglesList"); 
        captureCameraProp = serializedObject.FindProperty("_camera"); 

        columnsProp = serializedObject.FindProperty ("_columns");

        animationLength = serializedObject.FindProperty ("_animationLength");

        useFramesPerSecondProp = serializedObject.FindProperty ("_useFramesPerSecond");
        framesPerSecondProp = serializedObject.FindProperty ("_framesPerSecond");
        useCellCountProp = serializedObject.FindProperty ("_useCellCount");
        cellCountProp = serializedObject.FindProperty ("_cellCount");

        cellSizeProp = serializedObject.FindProperty ("_cellSize");
        
        diffuseMapProp = serializedObject.FindProperty ("_bakeDiffuseMap");
        normalMapProp = serializedObject.FindProperty ("_bakeNormalMap");

        savePath = serializedObject.FindProperty ("_savePath");

        animationReorderableList = new ReorderableList (serializedObject, objectAnimationListProp, true, true, false, false);

        animationReorderableList.drawHeaderCallback = rect => {
                EditorGUI.LabelField (rect, "Animations");
        };
                
        animationReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
			var element = animationReorderableList.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;

			EditorGUI.PropertyField(new Rect(rect.x, rect.y, 20, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("toggle"), GUIContent.none);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(new Rect(rect.x + 20, rect.y, rect.width - 20, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("clip"), GUIContent.none);
            EditorGUI.EndDisabledGroup();
        
		};

        animationReorderableList.onSelectCallback = (ReorderableList l) => {
            selectedAnimation = (AnimationClip) animationReorderableList.serializedProperty.GetArrayElementAtIndex(l.index).FindPropertyRelative("clip").objectReferenceValue;
            
            if (useCellCountProp.boolValue){
                framesPerSecondProp.intValue = (int)(cellCountProp.intValue / selectedAnimation.length);
            }

            if (useFramesPerSecondProp.boolValue){
                cellCountProp.intValue = (int)(selectedAnimation.length * framesPerSecondProp.intValue);
            }

            animationLength.floatValue = (float)(selectedAnimation.length);
        };

        angleReorderableList = new ReorderableList (serializedObject, captureAnglesListProp, true, true, true, true);
        
        angleReorderableList.drawHeaderCallback = rect => {
                EditorGUI.LabelField (rect, "Angles");
                EditorGUI.LabelField (new Rect(rect.x + (rect.width + 40) / 2, rect.y, rect.width + 20, EditorGUIUtility.singleLineHeight) , "File Prefix");
        };

        angleReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
			var element = angleReorderableList.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;

			EditorGUI.PropertyField(new Rect(rect.x, rect.y, 20, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("toggle"), GUIContent.none);
            
            EditorGUI.BeginChangeCheck();

			EditorGUI.PropertyField(new Rect(rect.x + 20, rect.y, (rect.width - 20) / 2, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("value"), GUIContent.none);
            
            if (EditorGUI.EndChangeCheck())
            {
                var captureCamera = (Camera) captureCameraProp.objectReferenceValue;
                captureCamera.transform.rotation = Quaternion.Euler (element.FindPropertyRelative("value").vector3IntValue);
            }

			EditorGUI.PropertyField(new Rect(rect.x + 35 + (rect.width - 40) / 2, rect.y, (rect.width - 40) / 2, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("prefix"), GUIContent.none);
   
		};

        angleReorderableList.onSelectCallback = (ReorderableList l) => {
            var captureCamera = (Camera) captureCameraProp.objectReferenceValue;
            captureCamera.transform.rotation = Quaternion.Euler (l.serializedProperty.GetArrayElementAtIndex(l.index).FindPropertyRelative("value").vector3IntValue);
        };
        
    }

    public override void OnInspectorGUI () {

        serializedObject.Update();

        using (new EditorGUI.DisabledScope (this.routine != null)) {

            using (new EditorGUILayout.VerticalScope (EditorStyles.helpBox)) {

                EditorGUILayout.LabelField ("Capture Object", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField (gameObject);
                
                if (EditorGUI.EndChangeCheck())
                {
                    LoadAnimations(gameObject.objectReferenceValue as GameObject);
                }

            }
            
            var objectReference = (GameObject) gameObject.objectReferenceValue;

            if (objectReference != null) {

                if (objectReference.GetComponent<Animation>() != null)
                {
                    
                    using (new EditorGUILayout.VerticalScope (EditorStyles.helpBox)) {

                        if(animationReorderableList.serializedProperty.arraySize == 0){
                            EditorGUILayout.HelpBox (ADD_ANIMATION_INFO, MessageType.Info);
                            serializedObject.ApplyModifiedProperties ();
                            return;
                        }

                        GUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField ("Object Animation(s)", EditorStyles.boldLabel);
                        
                        var styleReimport = new GUIStyle(GUI.skin.button);
                        
                        if (GUILayout.Button ("Refresh", styleReimport))
                        {
                            LoadAnimations(objectReference);
                        }

                        GUILayout.EndHorizontal();

                        animationReorderableList.DoLayoutList();

                        if (CountToggledSerializedProperty(animationReorderableList.serializedProperty, "toggle") == 0)
                        {
                            EditorGUILayout.HelpBox (String.Format(SELECT_ANIMATION, objectReference.name), MessageType.Warning);
                            serializedObject.ApplyModifiedProperties ();
                            return;
                        }


                        if (animationReorderableList.index == -1)
                        {
                            animationReorderableList.index = 0;
                            selectedAnimation = (AnimationClip) animationReorderableList.serializedProperty.GetArrayElementAtIndex(animationReorderableList.index).FindPropertyRelative("clip").objectReferenceValue;
                            animationLength.floatValue = selectedAnimation.length;
                        }
                    }

                }else{
                    //TODO Something wrong here
                    // useFramesPerSecondProp.boolValue = false;
                    EditorGUILayout.HelpBox (String.Format(LEGACY_ANIM_INFO, objectReference.name), MessageType.Info);
                    serializedObject.ApplyModifiedProperties ();
                }

                
            }else{
                EditorGUILayout.HelpBox (ASSIGN_REFS_INFO, MessageType.Warning);
                serializedObject.ApplyModifiedProperties ();
                return;
            }                

            // Capture Options Section
            using (new EditorGUILayout.VerticalScope (EditorStyles.helpBox)) {

                EditorGUILayout.LabelField ("Capture Options", EditorStyles.boldLabel);

                EditorGUILayout.ObjectField (captureCameraProp, typeof (Camera));

                if (captureCameraProp.objectReferenceValue == null) {
                    EditorGUILayout.HelpBox (ASSIGN_CAMERA_INFO, MessageType.Info);
                    serializedObject.ApplyModifiedProperties ();
                    return;
                }

                angleReorderableList.DoLayoutList();

                if (CountToggledSerializedProperty(angleReorderableList.serializedProperty, "toggle") == 0)
                {
                    EditorGUILayout.HelpBox (String.Format(SELECT_ANGLE, objectReference.name), MessageType.Warning);
                    serializedObject.ApplyModifiedProperties ();
                    return;
                }

                if(angleReorderableList.serializedProperty.arraySize == 0){
                    EditorGUILayout.HelpBox (ADD_ANGLE_INFO, MessageType.Info);
                    serializedObject.ApplyModifiedProperties ();
                    return;
                }       
                
                EditorGUILayout.LabelField ("Sprite Sheet Options", EditorStyles.boldLabel);

                EditorGUIUtility.labelWidth = 169;          
                
                //TODO Comment this shit
                EditorGUI.BeginChangeCheck();
                    
                    EditorGUI.BeginDisabledGroup((objectReference.GetComponent<Animation>() != null));
                        EditorGUILayout.PropertyField(animationLength); 
                    EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                {
                    if (animationLength.floatValue > 0)
                    {
                        framesPerSecondProp.intValue = (int)(cellCountProp.intValue / animationLength.floatValue);
                        cellCountProp.intValue = (int)(animationLength.floatValue * framesPerSecondProp.intValue);     
                    }else{
                        framesPerSecondProp.intValue = 0;
                        cellCountProp.intValue = 0;
                    }
                   
                }


                EditorGUIUtility.labelWidth = 150;

                GUILayout.BeginHorizontal();
                    //TODO Comment this shit
                    EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(useFramesPerSecondProp, GUIContent.none, GUILayout.MaxWidth(15));

                        EditorGUI.BeginDisabledGroup(useCellCountProp.boolValue);
                            EditorGUILayout.PropertyField(framesPerSecondProp);
                        EditorGUI.EndDisabledGroup();

                    if (EditorGUI.EndChangeCheck())
                    {
                        
                        useCellCountProp.boolValue = false;
                        useFramesPerSecondProp.boolValue = true;

                        cellCountProp.intValue = (int)(animationLength.floatValue * framesPerSecondProp.intValue);
                    }

                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    //TODO Comment this shit
                    EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField (useCellCountProp, GUIContent.none, GUILayout.MaxWidth(15));

                        EditorGUI.BeginDisabledGroup(useFramesPerSecondProp.boolValue);
                            EditorGUILayout.PropertyField (cellCountProp);
                        EditorGUI.EndDisabledGroup();

                    if (EditorGUI.EndChangeCheck())
                    {

                        useFramesPerSecondProp.boolValue = false;
                        useCellCountProp.boolValue = true;

                        framesPerSecondProp.intValue = (int)(cellCountProp.intValue / animationLength.floatValue);
                    }
                    
                GUILayout.EndHorizontal();


                if (framesPerSecondProp.intValue < 1)
                {
                    EditorGUILayout.HelpBox ("FPS value can't be zero", MessageType.Info);
                    serializedObject.ApplyModifiedProperties ();
                    return;
                }

                if (cellCountProp.intValue < 1)
                {
                    EditorGUILayout.HelpBox ("Cell count can't be zero", MessageType.Info);
                    serializedObject.ApplyModifiedProperties ();
                    return;
                }


                EditorGUIUtility.labelWidth = 169;
                                
                EditorGUILayout.PropertyField (columnsProp);
                EditorGUILayout.PropertyField (cellSizeProp);
                EditorGUILayout.PropertyField (diffuseMapProp);
                EditorGUILayout.PropertyField (normalMapProp);
                
                //TODO MB - Separate into folder by animations
                //TODO MB - Separate sprites into separate files

                if (columnsProp.intValue < 1)
                {
                    EditorGUILayout.HelpBox ("Collumns count can't be zero", MessageType.Info);
                    serializedObject.ApplyModifiedProperties ();
                    return;
                }

                if (cellSizeProp.vector2IntValue.x < 1 || cellSizeProp.vector2IntValue.y < 1)
                {
                    EditorGUILayout.HelpBox ("Cell size can't be zero", MessageType.Info);
                    serializedObject.ApplyModifiedProperties ();
                    return;
                }

                if (!diffuseMapProp.boolValue && !normalMapProp.boolValue)
                {
                    EditorGUILayout.HelpBox (SELECT_ONE_BAKE_WARN, MessageType.Warning);
                    serializedObject.ApplyModifiedProperties ();
                    return;
                }

                var styleCapture = new GUIStyle(GUI.skin.button);
                styleCapture.normal.textColor = Color.white;

                GUI.backgroundColor = new Color(0.70f,0.117f,0.10f);
                
                if (GUILayout.Button ("Bake", styleCapture)) {
                    
                    savePath.stringValue = EditorUtility.OpenFolderPanel("Save Sprite Sheet", "", "");
                    if (string.IsNullOrEmpty (savePath.stringValue)) {
                        return;
                    }
                    
                    var helper = (SpriteSheetGenerator) target;
                    StartCoroutine (helper.GenerateAnimationSpriteSheet(DisplayProgressBar));

                    AssetDatabase.Refresh();

                }

            }

            serializedObject.ApplyModifiedProperties ();
        }
    }

    private Int32 CountToggledSerializedProperty(SerializedProperty list, String propertyName){
        Int32 total = 0;
        foreach (SerializedProperty item in list)
        {
            if (!item.FindPropertyRelative(propertyName).boolValue) continue;
            total++;
        }
        return total;
    }

    private void DisplayProgressBar(float progress, float totalProgress){
        if (progress < totalProgress){
            EditorUtility.DisplayProgressBar("Baking", progress.ToString() + " / " + totalProgress.ToString(), (float)(progress / totalProgress));
        }else{
            EditorUtility.ClearProgressBar();
        }
    }

    private void LoadAnimations(GameObject objectReference){
        animationReorderableList.serializedProperty.ClearArray();

        if (objectReference == null) return;

        foreach (AnimationClip item in AnimationUtility.GetAnimationClips(objectReference))
        {
            var index = animationReorderableList.serializedProperty.arraySize;
            
            animationReorderableList.serializedProperty.arraySize++;
            animationReorderableList.index = index;
            var element = animationReorderableList.serializedProperty.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("clip").objectReferenceValue = item;
            element.FindPropertyRelative("toggle").boolValue = true;
        }
    }

    private void StartCoroutine (IEnumerator routine) {
        this.routine = routine;
        EditorApplication.update += UpdateCoroutine;
    }
    
    private void UpdateCoroutine () {
        if (!this.routine.MoveNext ()) {
            this.routine = null;
            EditorApplication.update -= UpdateCoroutine;
        }
    }
}