using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

//TODO: rewrite all this for UI Toolkit to gain access to other functions

/*
 * Cons_Window is the EditorWindow script that contains the GUI for the tool.
 * REQUIRES: Cons_Creator.cs, Cons_Object.cs
 */

public class Cons_Window : EditorWindow
{

    AnimationClip sourceAnim; //source animation controller to search bones from.

    private string source; //source bone to inherit transformations from
    private string target; //target bone to be affected by source.
    private GameObject rootObj; //the root, used to reference bone names, world transformation to local, etc.

    //GUI STUFF
    //may not be necessary but this is what i did.
    private bool showSourceObjects = true; //bone selection for source
    private bool showTargetObjects = true;//bone selection for target
    private Vector2 scrollPos; //scrolling for parts
    private bool hideOffset = true;
    private bool hideMix = true; 
    private float minValue = -1; //for the mix slider
    private float maxValue = 1; // for the mix slider
    private int flipFlop = 1; //used to flip flop the horizontal layouts between gray and dark gray


    //individual transforms will be calculated within
    //mix vars
    private float mixVal = 1; //range 0-100 -> this is the value that gets set for all the values. this does not need to be global.
    private bool linkedMix; //bool to enable changing all values at once
    private bool isRelative = false; //enables relative, which adds the source transform to the final calculation. meant to be used with Match Offset


    float[] mixWeightings = { 0, 0, 0 , //TRANS - 0,1,2   x,y,z
                        0 , 0 , 0, //ROT   - 3,4,5     x,y,z
                        0 , 0 , 0}; //SCALE  - 6,7,8   x,y,z

    float[] offsetValues = { 0, 0, 0 , //TRANS - 0,1,2   x,y,z
                        0 , 0 , 0, //ROT   - 3,4,5     x,y,z
                        0 , 0 , 0}; //SCALE  - 6,7,8   x,y,z


    [MenuItem("Animation Editor Toolkit/Animation Constraints")]
    static void Init() 
    {
        Cons_Window window = (Cons_Window)EditorWindow.GetWindow(typeof(Cons_Window), false, "Animation Constraints");
        window.Show();

    }


    int toolbarInt = 0; //for the toolbar Switch.
    string[] toolbarStrings = { "Create" }; //for future updates, where I plan to add constraint editing, etc.
    private void OnGUI()
    {
        
        EditorGUILayout.BeginHorizontal(); //toolBar, Help
        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        GUILayout.Space(200);
        GUIContent contentHelp = new GUIContent("Help", "https://github.com/spy-crab/Unity-animation-Constraints"); //TODO change lol
        if (GUILayout.Button(contentHelp, GUILayout.Width(50)))
        {
            Application.OpenURL("https://github.com/spy-crab/Unity-animation-Constraints"); //temporary
        }
        EditorGUILayout.EndHorizontal(); //toolBar, Help



        switch (toolbarInt) //for future updates
        {
            case 0: //Create

                EditorGUIUtility.labelWidth = 150;
                GUILayout.Label("Select source animation object to find bones from", EditorStyles.boldLabel);

                GUIContent contentSourceAnim = new GUIContent("Source Animation", "The animation file to reference keyframe data from");
                sourceAnim = EditorGUILayout.ObjectField(contentSourceAnim, sourceAnim, typeof(AnimationClip), false) as AnimationClip;

                if (sourceAnim == null)
                {
                    EditorGUILayout.HelpBox("Select a source animation clip", MessageType.Warning);
                }

                GUIContent contentRootObj = new GUIContent("Root Scene Object", "The game object at the top of the heirarchy that holds the skeleton.");
                rootObj = (GameObject)EditorGUILayout.ObjectField(contentRootObj, rootObj, typeof(GameObject), true);

                if (rootObj == null)
                {
                    EditorGUILayout.HelpBox("Select a root scene object", MessageType.Warning);
                }

                EditorGUI.BeginDisabledGroup(sourceAnim == null || rootObj == null || rootObj==null && sourceAnim==null); //sourceAnim == null || rootObj == null || rootObj==null && sourceAnim==null
                GUILayout.BeginHorizontal(); //Sourc, Target
                {
                    EditorGUIUtility.labelWidth = 50; //removes large gaps
                    GUIContent contentSource = new GUIContent("Source:", "The object to use as reference to guide the Target");
                    source = EditorGUILayout.TextField(contentSource, source);
                    if (GUILayout.Button("Find"))
                    {
                        showSourceObjects = false;
                        showTargetObjects = true; // you cannot have both selected at the same time
                        Cons_Creator.ScanPaths(sourceAnim);
                    }


                    EditorGUILayout.Space(); 
                    GUIContent contentTarget = new GUIContent("Target:", "The object that will be affected by the Source");
                    target = EditorGUILayout.TextField(contentTarget, target);
                    if (GUILayout.Button("Find"))
                    {
                        showTargetObjects = false;
                        showSourceObjects = true;
                        Cons_Creator.ScanPaths(sourceAnim);
                    }
                }
                GUILayout.EndHorizontal(); //Sourc, Target
                EditorGUI.EndDisabledGroup(); //sourceAnim == null || rootObj == null || rootObj==null && sourceAnim==null
                /////////////
                EditorGUI.BeginDisabledGroup(showSourceObjects && showTargetObjects); //(showSourceObjects && showTargetObjects)

                {
                    if (showSourceObjects == false || showTargetObjects == false)
                    {


                        { ////////////////////PARTS LISTTT

                            scrollPos = EditorGUILayout.BeginScrollView(scrollPos); //scrollPos

                            // go through all the properties found, sort it.
                            var sortedList = Cons_Creator.pathToSharedProperty.OrderBy(property => property.path).ToList();
                            //Debug.Log("what"+ sortedList.Count);
                            foreach (var sharedProperty in sortedList)

                            //foreach (var animationProperty in AniConstraintContainer.pathToSharedProperty)
                            {
                                //var sharedProperty = animationProperty.Value;

                                flipFlop = 1 - flipFlop;
                                if (flipFlop == 1)
                                {
                                    //dark gray box
                                    EditorGUILayout.BeginHorizontal(BackgroundStyle.Get(new Color(0.15f, 0.15f, 0.15f, 0.7f))); //pathBox
                                }
                                else
                                {
                                    EditorGUILayout.BeginHorizontal(); //pathBox
                                }
                                EditorGUI.indentLevel++;

                                //GUI.skin.hover.textColor = Color.green;
                                EditorGUILayout.LabelField(sharedProperty.name, EditorStyles.label);
                                GUI.skin = null;
                                if (GUILayout.Button("Select"))
                                {
                                    if (showSourceObjects == false)
                                    {
                                        source = sharedProperty.path;
                                    }
                                    else
                                    {
                                        target = sharedProperty.path;
                                    }

                                    showSourceObjects = true;
                                    showTargetObjects = true;
                                    //exit selection. write selection into source
                                }
                                EditorGUI.indentLevel--;
                                EditorGUILayout.EndHorizontal(); //pathBox


                            }
                            EditorGUILayout.EndScrollView(); //scrollPos
                        }

                        //////////////////////////
                        GUILayout.Space(5);


                        if (GUILayout.Button("Cancel")) //might not be the best implementation but it exists.
                        {
                            showSourceObjects = true;
                            showTargetObjects = true;
                            //exits out
                        }

                    }
                    EditorGUI.EndDisabledGroup(); //(showSourceObjects && showTargetObjects)


                    EditorGUILayout.Space(5);

                    EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(target) && string.IsNullOrEmpty(source)); //if source or target are empty.. disable!

                        //////////////////////////////////////////////////////////////////OFFSET 
                        EditorGUILayout.BeginHorizontal(); //Offset, relative
                    GUIContent contentOffset = new GUIContent("Offset", "Offsets the final transform of the target by the below values");
                    hideOffset = EditorGUILayout.Foldout(hideOffset, contentOffset);
                    GUILayout.Space(20);
                    GUIContent contentRelative = new GUIContent("Relative", "Adds the source transform to the final transform of the target. This is meant to be used with 'Match Offset'");
                    isRelative = EditorGUILayout.Toggle(contentRelative, isRelative); 
                    GUILayout.FlexibleSpace(); //eats up the right side so everything is leaning to the left.

                    EditorGUILayout.EndHorizontal(); //Offset, relative

                    if (hideOffset) //if we arent hiding the offset window.
                    {
                        EditorGUI.indentLevel++; //1

                        EditorGUILayout.Space();
                        EditorGUILayout.Space();

                        EditorGUILayout.BeginHorizontal(); //TRANS
                        GUILayout.Label("Position");
                        EditorGUIUtility.labelWidth = 30;
                        offsetValues[0] = EditorGUILayout.FloatField("X", offsetValues[0], GUILayout.Width(100));
                        offsetValues[1] = EditorGUILayout.FloatField("Y", offsetValues[1], GUILayout.Width(100));
                        offsetValues[2] = EditorGUILayout.FloatField("Z", offsetValues[2], GUILayout.Width(100));
                        EditorGUILayout.EndHorizontal(); //TRANS


                        EditorGUILayout.BeginHorizontal(); //ROT
                        GUILayout.Label("Rotation");
                        offsetValues[3] = EditorGUILayout.FloatField("X", offsetValues[3], GUILayout.Width(100));
                        offsetValues[4] = EditorGUILayout.FloatField("Y", offsetValues[4], GUILayout.Width(100));
                        offsetValues[5] = EditorGUILayout.FloatField("Z", offsetValues[5], GUILayout.Width(100));
                        EditorGUILayout.EndHorizontal(); //ROT


                        EditorGUILayout.BeginHorizontal(); //SCALE
                        GUILayout.Label("Scale");
                        offsetValues[6] = EditorGUILayout.FloatField("X", offsetValues[6], GUILayout.Width(100));
                        offsetValues[7] = EditorGUILayout.FloatField("Y", offsetValues[7], GUILayout.Width(100));
                        offsetValues[8] = EditorGUILayout.FloatField("Z", offsetValues[8], GUILayout.Width(100));
                        EditorGUILayout.EndHorizontal(); //SCALE
                        EditorGUI.indentLevel--; //0

                        EditorGUILayout.BeginHorizontal(); //Match offset
                        GUIContent contentMatchOffset = new GUIContent("Match Offset", "Sets the offset to be equal to the base transformation of the object.");
                        if (GUILayout.Button(contentMatchOffset)) 
                        {
                            Cons_Creator.setSourceandTargetBindings(source, target);
                            Cons_Creator.setRoot(rootObj); //type mismatch can be ignored
                            Cons_Creator.setCurrentAnimClip(sourceAnim);
                            Cons_Creator.setExistingScriptableObject();
                            //set offset values to be equal to constraintReference transform values.
                            offsetValues[0] = Cons_Creator.getTargetWorldTransformPos().x;
                            offsetValues[1] = Cons_Creator.getTargetWorldTransformPos().y;
                            offsetValues[2] = Cons_Creator.getTargetWorldTransformPos().z;
                            offsetValues[3] = Cons_Creator.getTargetWorldRot().x;
                            offsetValues[4] = Cons_Creator.getTargetWorldRot().y;
                            offsetValues[5] = Cons_Creator.getTargetWorldRot().z;
                            offsetValues[6] = Cons_Creator.getTargetWorldScale().x - 1;
                            offsetValues[7] = Cons_Creator.getTargetWorldScale().y -1;
                            offsetValues[8] = Cons_Creator.getTargetWorldScale().z -1;
                        }
                        EditorGUILayout.Space(50);
                        GUIContent contentResetOffset = new GUIContent("Reset Offset", "Sets all offset values to 0");
                        if (GUILayout.Button(contentResetOffset)) 
                        {
                            offsetValues[0] = 0;
                            offsetValues[1] = 0;
                            offsetValues[2] = 0;
                            offsetValues[3] = 0;
                            offsetValues[4] = 0;
                            offsetValues[5] = 0;
                            offsetValues[6] = 0;
                            offsetValues[7] = 0;
                            offsetValues[8] = 0;
                        }

                        EditorGUILayout.EndHorizontal();//Match offset
                    }

                    EditorGUILayout.Space();

                    /////////////////////////////////////////////////////////////////////MIX
                    EditorGUILayout.BeginHorizontal(); //Mix foldout
                    GUIContent contentMix = new GUIContent("Mix", "The amount the Target is affected by the Source.");
                    hideMix = EditorGUILayout.Foldout(hideMix, contentMix);
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal(); //Mix foldout
                    if (hideMix)
                    {
                        EditorGUILayout.BeginHorizontal(); //link values, min max slider values
                        EditorGUIUtility.labelWidth = 100;
                        GUIContent contentLinkValues = new GUIContent("Link values", "Change all Mix values together.");
                        linkedMix = EditorGUILayout.Toggle(contentLinkValues, linkedMix);
                        GUILayout.Space(20);
                        if (linkedMix) // make it editable when not linked. locked when linked.
                        {
                            minValue = EditorGUILayout.FloatField("Min Slider value", minValue);
                            maxValue = EditorGUILayout.FloatField("Max Slider value", maxValue);
                        }
                        GUILayout.FlexibleSpace(); //eats up right side
                        EditorGUILayout.EndHorizontal(); //link values, min max slider values

                        if (linkedMix) 
                        { //probably not the best.
                            mixVal = EditorGUILayout.Slider(mixVal, minValue, maxValue);
                            EditorGUI.BeginDisabledGroup(linkedMix); //linkedMix
                            EditorGUI.indentLevel++; //1
                            EditorGUIUtility.labelWidth = 30;

                            EditorGUILayout.BeginHorizontal(); //TRANS
                            GUILayout.Label("Position");
                            mixWeightings[0] = EditorGUILayout.FloatField("X", mixVal, GUILayout.Width(100));
                            mixWeightings[1] = EditorGUILayout.FloatField("Y", mixVal, GUILayout.Width(100));
                            mixWeightings[2] = EditorGUILayout.FloatField("Z", mixVal, GUILayout.Width(100));
                            EditorGUILayout.EndHorizontal(); //TRANS


                            EditorGUILayout.BeginHorizontal(); //ROT
                            GUILayout.Label("Rotation");
                            mixWeightings[3] = EditorGUILayout.FloatField("X", mixVal, GUILayout.Width(100));
                            mixWeightings[4] = EditorGUILayout.FloatField("Y", mixVal, GUILayout.Width(100));
                            mixWeightings[5] = EditorGUILayout.FloatField("Z", mixVal, GUILayout.Width(100));
                            EditorGUILayout.EndHorizontal(); //ROT


                            EditorGUILayout.BeginHorizontal(); //SCALE
                            GUILayout.Label("Scale");
                            mixWeightings[6] = EditorGUILayout.FloatField("X", mixVal, GUILayout.Width(100));
                            mixWeightings[7] = EditorGUILayout.FloatField("Y", mixVal, GUILayout.Width(100));
                            mixWeightings[8] = EditorGUILayout.FloatField("Z", mixVal, GUILayout.Width(100));
                            EditorGUILayout.EndHorizontal(); //SCALE

                            EditorGUI.EndDisabledGroup(); //linkedMix
                            EditorGUI.indentLevel--; //0
                        }
                        else
                        {
                            EditorGUI.BeginDisabledGroup(linkedMix); //linkedMix
                            EditorGUI.indentLevel++; //1
                            EditorGUIUtility.labelWidth = 30;

                            EditorGUILayout.BeginHorizontal(); //TRANS
                            GUILayout.Label("Position");
                            mixWeightings[0] = EditorGUILayout.FloatField("X", mixWeightings[0], GUILayout.Width(100));
                            mixWeightings[1] = EditorGUILayout.FloatField("Y", mixWeightings[1], GUILayout.Width(100));
                            mixWeightings[2] = EditorGUILayout.FloatField("Z", mixWeightings[2], GUILayout.Width(100));
                            EditorGUILayout.EndHorizontal(); //TRANS


                            EditorGUILayout.BeginHorizontal(); //ROT
                            GUILayout.Label("Rotation");
                            mixWeightings[3] = EditorGUILayout.FloatField("X", mixWeightings[3], GUILayout.Width(100));
                            mixWeightings[4] = EditorGUILayout.FloatField("Y", mixWeightings[4], GUILayout.Width(100));
                            mixWeightings[5] = EditorGUILayout.FloatField("Z", mixWeightings[5], GUILayout.Width(100));
                            EditorGUILayout.EndHorizontal(); //ROT


                            EditorGUILayout.BeginHorizontal(); //SCALE
                            GUILayout.Label("Scale");
                            mixWeightings[6] = EditorGUILayout.FloatField("X", mixWeightings[6], GUILayout.Width(100));
                            mixWeightings[7] = EditorGUILayout.FloatField("Y", mixWeightings[7], GUILayout.Width(100));
                            mixWeightings[8] = EditorGUILayout.FloatField("Z", mixWeightings[8], GUILayout.Width(100));
                            EditorGUILayout.EndHorizontal(); //SCALE
                            EditorGUI.indentLevel--; //0
                            EditorGUI.EndDisabledGroup(); //linkedMix
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.Space();

                    }

                    ///////////////////////////////////////////////////////////////////////

                    EditorGUILayout.BeginHorizontal(); //Key, clear keys
                    GUIContent contentKey = new GUIContent("Key", "Create a constraint object/ edit existing one with Scriptable Object data.");
                    if (GUILayout.Button(contentKey))
                    {
                        if (!rootObj) //you cant do that!
                        {
                            EditorUtility.DisplayDialog("Error", "Please select a root object.", "OK");
                        }
                        else //root object exists, we can create keys.
                        {
                            Cons_Creator.setSourceandTargetBindings(source, target);
                            Cons_Creator.setRoot(rootObj); //type mismatch can be ignored
                            Cons_Creator.setCurrentAnimClip(sourceAnim);
                            Cons_Creator.setExistingScriptableObject(); //if fails -- missing scriptable object!
                            Cons_Creator.createWeightedConstraint(sourceAnim, rootObj, mixWeightings, offsetValues, isRelative);
                        }
                    }

                    EditorGUILayout.Space();

                    //Removes animation data from the selected target
                    GUIContent contentClearKeys = new GUIContent("Clear Keys", "Clears keyframe data within the Target. Retains Scribtable Object");
                    if (GUILayout.Button(contentClearKeys))
                    {
                        Cons_Creator.setSourceandTargetBindings(source, target);
                        Cons_Creator.setRoot(rootObj); //type mismatch can be ignored
                        Cons_Creator.setCurrentAnimClip(sourceAnim);
                        Cons_Creator.setExistingScriptableObject(); //if fails -- missing scriptable object!

                        try
                        {
                            Cons_Creator.resetKeysToBase(sourceAnim);
                            //Reset all these settings too -- makes it easier. TODO: maybe just create a reset helper function?
                            mixWeightings[0] = 0;
                            mixWeightings[1] = 0;
                            mixWeightings[2] = 0;
                            mixWeightings[3] = 0;
                            mixWeightings[4] = 0;
                            mixWeightings[5] = 0;
                            mixWeightings[6] = 0;
                            mixWeightings[7] = 0;
                            mixWeightings[8] = 0;

                            offsetValues[0] = 0;
                            offsetValues[1] = 0;
                            offsetValues[2] = 0;
                            offsetValues[3] = 0;
                            offsetValues[4] = 0;
                            offsetValues[5] = 0;
                            offsetValues[6] = 0;
                            offsetValues[7] = 0;
                            offsetValues[8] = 0;
                            //AniConstraintContainer.resetKeysToBase(sourceAnim);
                        }
                        catch
                        {
                            Debug.Log("Failed, check that you have source and target assigned");
                        }

                        //EditorUtility.DisplayDialog("Warning", "Are you sure? This will delete all keys on the target", "I understand");

                        /*
                         *TODO: confirmation of destructive action. 
                         */

                    }

                    GUIContent contentLoadData = new GUIContent("Load data", "Fills in offset, and mix values, previously keyed onto this constraint");
                    if (GUILayout.Button(contentLoadData))
                    {
                        Cons_Creator.setSourceandTargetBindings(source, target);
                        Cons_Creator.setRoot(rootObj); //type mismatch can be ignored
                        Cons_Creator.setCurrentAnimClip(sourceAnim);
                        Cons_Creator.loadScriptableObjectData(offsetValues, mixWeightings);
                    }

                    EditorGUI.EndDisabledGroup();  //if source or target are empty.. disable!
                    EditorGUILayout.EndHorizontal(); //Key, clear keys
                }



                break; // END OF CREATE MODE



           
        } //toolBarInt


        //should this be collapsible?
        EditorGUILayout.HelpBox("Version 0.3 - Early Access | Send feedback on GitHub ", MessageType.Info);
    } //onGUI

    
    public static class BackgroundStyle //used to create the black boxes -- https://discussions.unity.com/t/changing-the-background-color-for-beginhorizontal/427449/15
    {
        private static GUIStyle style = new GUIStyle();
        private static Texture2D texture = new Texture2D(1, 1);


        public static GUIStyle Get(Color color)
        {
            texture.SetPixel(0, 0, color);
            texture.Apply();
            style.normal.background = texture;
            return style;
        }
    }

}//class

