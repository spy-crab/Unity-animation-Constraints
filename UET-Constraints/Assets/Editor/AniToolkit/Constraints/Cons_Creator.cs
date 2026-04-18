using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting; //cant convert string to char -- TODO: code rewrite to remove unecessary library. //ToCharArray()
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Cons_Object; //the scriptable object


/*
 * Creates constraints when given two paths. these constraints have relationships with each other based on waht the user inputs
 */

public class Cons_Creator
{
    /////INPUTS///////////////////////
    public static AnimationClip animClip; //the animation clip we are editing
    private static GameObject root; //game object at the top of the heirarchy.
    private static string currentSourcePath; //path of hte object that will be used as a reference
    private static string currentTargetPath; //path of the object that will be edited
    public static Transform targetTransform; //transform of the Target, will be assigned later

    ///////////////////////////
    public class SharedProperty //the list of bindings and animation data within the animClip.
    {
        //Where the invalid animation clips are stored
        public string name; //this lets us show a simplified list
        public AnimationClip animClip;//the animation clip that the part derives from.
        public string path; //path of the anim
        public int count;//property#
    }
    public static HashSet<SharedProperty> pathToSharedProperty = new();

    private static Cons_Object constraintReference; //scriptable object

    /////////////////////////////////GETTERS AND SETTERS///


    /*
    * Assigns local variables
    * string source: the path of the source object to reference
    * string target: the path of the target object to edit
    */
    public static void setSourceandTargetBindings(string source, string target)
    {
        currentSourcePath = source;
        currentTargetPath = target;

    }

    /*Sets the root variable within the script using the given root.
     * GameObject GivenRoot: this should be the root game object that holds the animation.
     */
    public static void setRoot(GameObject GivenRoot)
    {
        root = GivenRoot;
    }

    /*Grabs the constraint reference transform. constraintReference refers to the currently active scriptable object that is currently being edited.
     * 
     */
    public static Transform getConstraintReferenceTransform()
    {
        if (constraintReference.getTransform() == null) 
        {
            Debug.Log("Failed to return transform.");
            return null;
        }
        else
        {
            Debug.Log(getConstraintReferenceTransform().position.x);
            return constraintReference.getTransform();
        }
    }


    /*Scans the animation clipp for paths within the clip.
     *  AnimationClip clip: animation clip to scan paths from.
     */
    public static void ScanPaths(AnimationClip clip)
    {
        pathToSharedProperty.Clear();
        try
        {
            var floatCurves = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in floatCurves)
            {
                CheckBinding(binding, clip);
            }
                

            // Object reference curves
            var objectCurves = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in objectCurves)
            {
                CheckBinding(binding, clip);
            }
                
        }
        catch
        {
            Debug.LogError("Failed to scan paths... :(");
            //nothing happens otherwise
        }

    }



    /*
     *Sets the value for the Transform for the target. 
     *This will be used for weighting, offset calculations, and reset. 
     *AnimationClip clip: the animation clip to grab bindings from
     *GabeObject root: the game object at the root of the object heirarchy, not necessarily the root bone.
     */
    public static void setTransform(AnimationClip clip, GameObject root)
    {

        var floatBindings = AnimationUtility.GetCurveBindings(clip); //Animations have two types of bindings, float bindings, and object bindings which are mysterious
        var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip); //TODO
        foreach (var binding in floatBindings)
        {
            if (binding.path == currentTargetPath)
            {
                clip.SampleAnimation(root, 0);
                Transform bindingTransform = root.transform.Find(binding.path);
                if (bindingTransform != null)
                {
                    targetTransform = bindingTransform;
                }
                else
                {
                    Debug.LogError("Could not find target transform! Are you in the right scene?");
                }


            }

        }
        foreach (var binding in objectBindings) //just in case
        {
            if (binding.path == currentTargetPath)
            {
                clip.SampleAnimation(root, 0);
                Transform bindingTransform = root.transform.Find(binding.path);
                if (bindingTransform != null)
                {
                    targetTransform = bindingTransform;
                }
                else
                {
                    Debug.LogError("Could not find target transform! Are you in the right scene?");
                }


            }
        }
    }

    /*When opening a constraint that already exists, the animation clip needs to be reassigned.
     * Perhaps there is a better design for all this.. alas.. I lack knowledge.
     * AnimationClip clip: the animation clip we are editing.
     */
    public static void setCurrentAnimClip(AnimationClip clip)
    {
        animClip = clip;
    }


    /*  This method checks for an existing scriptable object
     *  string fileName: this is the name of the scriptable object. 
     *  return:object, if found, otherwise it is null: not found.
     */
    public static Cons_Object GetExistingScriptableObject()
    {
        string fileName = getBindingName(currentSourcePath) + "_ConstraintTo_" + getBindingName(getBindingName(currentTargetPath));
        return AssetDatabase.LoadAssetAtPath<Cons_Object>("Assets/Editor/AniToolkit/Constraints/" + fileName + ".asset"); //if it is null none was found.
    }
    /*
     * Sets constraintReference to the scriptable object that should exist.
     */
    public static void setExistingScriptableObject()
    {
        constraintReference = GetExistingScriptableObject();
    }
    /*
     * grabs the targetWorldTransformPos using the constraintReference.
     */
    public static Vector3 getTargetWorldTransformPos()
    {
        Transform parentTransform = getParentTransform(root, currentTargetPath);
        if (!constraintReference)
        {
            //Debug.Log("why no constraintReference assigned?");
            return Vector3.zero;
        }
        //convert from world space to local space using the parent.
        return parentTransform.InverseTransformPoint(constraintReference.targetWorldTransformPos); // maybe dont use constraintReference for this?
    }
    /*
   * grabs the targetWorldRot using the constraintReference.
   */
    public static Vector3 getTargetWorldRot()
    {
        Transform parentTransform = getParentTransform(root, currentTargetPath);
        if (!constraintReference)
        {
            //Debug.Log("why no constraintReference assigned?");
            return Vector3.zero;
        }
        //convert from world space to local space using the parent.
        return parentTransform.InverseTransformPoint(constraintReference.targetWorldRot);
    }

    /*
   * grabs the targetWorldScale using the constraintReference.
   */
    public static Vector3 getTargetWorldScale()
    {
        //Transform parentTransform = getParentTransform(root, currentTargetPath);
        if (!constraintReference)
        {
            //Debug.Log("why no constraintReference assigned?");
            return Vector3.zero;
        }
        //there is no 'world space scale' so no calculation as opposed to the other two..
        return constraintReference.targetWorldScale;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /*
     *CheckBinding is used to add bindings to pathToSharedProperty. which is used to create constraints, etc.
     *EditorCurveBinding binding: the binding from the animation clip.
     *ANimationClip clip: the animation clip that the bindings originate from..
     */
    private static void CheckBinding(EditorCurveBinding binding, AnimationClip clip)
    {
        // check the HashSet for the first entry where property.oldpath = binding.path and return it in that var.
        var sharedProperty = pathToSharedProperty.FirstOrDefault(property => property.path == binding.path);

        if (sharedProperty == null)
        {
            sharedProperty = new SharedProperty();
            sharedProperty.path = binding.path;
            //sharedProperty.newPath = binding.path;
            sharedProperty.name = getBindingName(binding);
            pathToSharedProperty.Add(sharedProperty);
        }

        if (!sharedProperty.animClip)
        {
            sharedProperty.animClip = clip;
        }
            

        sharedProperty.count++;
    }


    /* Creates a weighted transform/rot/scale constraint between the target and the source.
     * Weight:
     * transform x, y,z
     * rot x,y,z
     * scale x,y,z
     * 
     * AnimationClip clip: the animation clip we are editing
     * GameObject root: the game object that contains the animated objects. at the top of the heirarchy
     * float[] weightings: the amount different aspects of the Target transform will be affected by the Source.
     * float[] offset: the amount to offset the resulting Target transform.
     * bool isRelative: do we need to add the source Base transform?
     */
    public static void createWeightedConstraint(AnimationClip clip, GameObject root, float[] weightings, float[] offset, bool isRelative)
    {
        //if string is empty then return you failed.
        if (string.IsNullOrEmpty(currentSourcePath) || string.IsNullOrEmpty(currentTargetPath))
        {
            return;
        }

        //TODO fix these namings. they are haunted from the past.
        //RELATIVE TRANSFORM
        Transform sourceBaseTransform;
        Vector3 sourceBaseTransVec;

        //INFO TO COPY
        Vector3 sourceTransformPos;
        //Vector3 sourceWorldRot; 
        Vector3 sourceTransformScale;

        //BASE TO BLEND OFF OF
        Vector3 targetTransformPos;
        Vector3 targetTransformRot;
        Vector3 targetTransformScale;

        Undo.RecordObject(clip, "Create Constraint");
        //TODO: bool: ovwerite? //different text, and add warning

        //float curves. object bindings found below.
        var floatBindings = AnimationUtility.GetCurveBindings(clip);

        //you need to reference a game object, so this only happens once, otherwise it keeps getting overwritten
        if (!targetTransform)
        {
            setTransform(clip, root);
        }
        //create scriptable object on disk if one does not already exist for this relationship.
        createDataObject(weightings, offset);

        if (constraintReference) //one should exist, because we just made one. otherwise it likely needs to be reassigned: it exited the prior function safely, but left it unassigned..
        {
            EditorUtility.SetDirty(constraintReference);
            if (constraintReference.backupInfo.Count == 0)//no backup info? we need to add stuff!
            {
                foreach (var binding in floatBindings)
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                    if (curve != null) //check if dataObject exists.
                    {
                        if (binding.path == currentTargetPath)
                        {

                            if (constraintReference.GetBackupTargetCurve(binding.propertyName) == null) //if the curve does NOT exist already.
                            {
                                var backup = new CurveBackup();
                                backup.curve = new AnimationCurve(curve.keys);
                                backup.propertyName = binding.propertyName;
                                backup.path = binding.path;

                                constraintReference.backupInfo.Add(backup);
                                EditorUtility.SetDirty(constraintReference); //save it!
                            }
                        }
                    }
                }
            }

        }
        else //constraintReference
        {
            //one already exists, so lets assign it!
            constraintReference = GetExistingScriptableObject();
            EditorUtility.SetDirty(constraintReference);
        }


        if (isRelative) //if it is relative then we set the value for sourceBaseTransVec. this is added into the transforms
        {
            clip.SampleAnimation(root, 0);
            sourceBaseTransform = root.transform.Find(currentSourcePath);
            Transform parentTransform = root.transform.Find(getParentPath(currentTargetPath)); //might be wasteful
            Transform bindingTransform = root.transform.Find(currentSourcePath);
            sourceBaseTransVec = parentTransform.InverseTransformPoint(bindingTransform.position);
        }
        else //isRelative
        {
            sourceBaseTransVec = Vector3.zero;
        }

        foreach (var binding in floatBindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (binding.path == currentSourcePath)
            {
                //Debug.Log(binding.path + " compare to " + currentSourcePath);

                Keyframe[] ks = new Keyframe[curve.keys.Length]; //holds the sauce

                for (int i = 0; i < curve.keys.Length; i++)
                {
                    var key = curve.keys[i];

                    clip.SampleAnimation(root, key.time);
                    //binding transform is the SOURCE transform.
                    Transform bindingTransform = root.transform.Find(binding.path);
                    Transform parentTransform = root.transform.Find(getParentPath(currentTargetPath));
                    //Debug.Log("the source path is: "+ binding.path + " and the parent is: "+ getParentPath(currentTargetPath));

                    sourceTransformPos = parentTransform.InverseTransformPoint(bindingTransform.position); 
                    sourceTransformScale = (bindingTransform.localScale); //TODO FIX flickering

                    //Debug.Log(parentTransform.rotation.eulerAngles.z + "bind" + bindingTransform.rotation.eulerAngles.z + "sourc"+ sourceWorldRot.z);

                    targetTransformPos = parentTransform.InverseTransformPoint(constraintReference.targetWorldTransformPos); //TODO: investigate loss of data
                    targetTransformScale = constraintReference.targetWorldScale; 
                    targetTransformRot = parentTransform.InverseTransformPoint(constraintReference.targetWorldRot);

                    

                    float keyValue = 0; //lets us edit weighting in a more readable way.
                    #region assigning FLOAT Keyframe Values
                    switch (binding.propertyName) //this is trash.... 
                    {
                        ////TRANSFORMS -> we want this to be global position.
                        case "m_LocalPosition.x":
                            if (weightings[0] <= 1f && weightings[0] >= 0f) //between 0 and 1, blend with the original, otherwise go ALL IN!!!
                            {
                                //Debug.Log(targetWorldTransformPos.x); // TODO: this value keeps getting changed!
                                keyValue = Mathf.Lerp(targetTransformPos.x + offset[0], (weightings[0] * (sourceTransformPos.x) + offset[0] + sourceBaseTransVec.x), weightings[0]);

                                //  keyValue = Mathf.Lerp(targetWorldRot.x + offset[3], (weightings[3] * (sourceWorldRot.x) + offset[3]), weightings[3]);
                            }
                            else
                            {
                                keyValue = (weightings[0] * (sourceTransformPos.x) + offset[0] + sourceBaseTransVec.x);
                            }
                            //Debug.Log(keyValue + " = : " + weightings[0] + "*" + sourceTransformPos.x + " ) +" + offset[0] + " what " + bindingTransform.position.x);
                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[0], key.outTangent* weightings[0], key.inWeight, key.outWeight);
                            //Debug.Log(key+ " edited");

                            break;

                        case "m_LocalPosition.y":
                            if (weightings[1] <= 1f && weightings[1] >= 0f)
                            {
                                keyValue = Mathf.Lerp(targetTransformPos.y + offset[1], (weightings[1] * (sourceTransformPos.y) + offset[1] + sourceBaseTransVec.y), weightings[1]);
                            }
                            else
                            {
                                keyValue = (weightings[1] * (sourceTransformPos.y) + offset[1] + sourceBaseTransVec.y);
                            }
                            //Debug.Log(keyValue + " and weight: " + weightings[1] + "" + offset[1]);
                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[1], key.outTangent * weightings[1], key.inWeight, key.outWeight);

                            break;

                        case "m_LocalPosition.z":
                            if (weightings[2] <= 1 && weightings[2] >= 0)
                            {
                                keyValue = Mathf.Lerp(targetTransformPos.z + offset[2], (weightings[2] * (sourceTransformPos.z) + offset[2]+ sourceBaseTransVec.z), weightings[2]);
                            }
                            else
                            {
                                keyValue = (weightings[2] * (sourceTransformPos.z) + offset[2] + sourceBaseTransVec.z);
                            }

                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[2], key.outTangent * weightings[2], key.inWeight, key.outWeight);
                            

                            //Debug.Log(key + " edited");

                            break;

                        ////ROTATIONS
                        case "localEulerAnglesRaw.x":
                            if (weightings[3] <= 1 && weightings[3] >= 0)
                            {
                                keyValue = Mathf.Lerp(targetTransformRot.x + offset[3], (weightings[3] * (curve.keys[i].value) + offset[3]), weightings[3]);
                            }
                            else
                            {
                                keyValue = (weightings[3] * (curve.keys[i].value) + offset[3]);
                            }


                            //Debug.Log(weightings[3] + " mult " + sourceWorldRot.x);
                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[3], key.outTangent * weightings[3], key.inWeight, key.outWeight);

                            break;

                        case "localEulerAnglesRaw.y":
                            if (weightings[4] <= 1 && weightings[4] >= 0)
                            {
                                keyValue = Mathf.Lerp(targetTransformRot.y + offset[4], (weightings[4] * (curve.keys[i].value) + offset[4]), weightings[4]);
                            }
                            else
                            {
                                keyValue = (weightings[4] * (curve.keys[i].value) + offset[4]);
                            }

                            //Debug.Log(keyValue + "= " +weightings[4]+ " * " + sourceWorldRot.y +" | Tangent: "+  key.inTangent + " - " + key.outTangent);
                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[4], key.outTangent * weightings[4], key.inWeight, key.outWeight);

                            break;

                        case "localEulerAnglesRaw.z":
                            if (weightings[5] <= 1 && weightings[5] >= 0)
                            {
                                //Debug.Log(curve.keys[i].value);
                                keyValue = Mathf.Lerp(targetTransformRot.z + offset[5], (weightings[5] * (curve.keys[i].value) + offset[5]), weightings[5]);
                            }
                            else
                            {
                                keyValue = (weightings[5] * (curve.keys[i].value) + offset[5]);
                            }

                            //keyValue = (weightings[5] * (sourceWorldRot.z) + offset[5]);
                            //Debug.Log(weightings[5] + " mult " + sourceWorldRot.z);
                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[5], key.outTangent * weightings[5], key.inWeight, key.outWeight);

                            break;

                        //SCALE
                        case "m_LocalScale.x":
                            //keyValue = (weightings[6] * (sourceTransformScale.x) + offset[6]);
                            if (weightings[6] <= 1 && weightings[6] >= 0)
                            {
                                keyValue = Mathf.Lerp(targetTransformScale.x + offset[6], (weightings[6] * (sourceTransformScale.x) + (offset[6])), weightings[6]);
                            }
                            else
                            {
                                keyValue = (weightings[6] * (sourceTransformScale.x) + (offset[6]));
                            }


                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[6], key.outTangent * weightings[6], key.inWeight, key.outWeight);

                            break;

                        case "m_LocalScale.y":
                            if (weightings[7] <= 1 && weightings[7] >= 0)
                            {
                                keyValue = Mathf.Lerp(targetTransformScale.y + offset[7], (weightings[7] * (sourceTransformScale.y) + (offset[7])), weightings[7]);
                            }
                            else
                            {
                                keyValue = (weightings[7] * (sourceTransformScale.y) + (offset[7]));
                            }



                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[7], key.outTangent * weightings[7], key.inWeight, key.outWeight);

                            break;

                        case "m_LocalScale.z":
                            if (weightings[7] <= 1 && weightings[7] >= 0)
                            {
                                keyValue = Mathf.Lerp(targetTransformScale.z + offset[8], (weightings[8] * (sourceTransformScale.z) + (offset[8])), weightings[8]);
                            }
                            else
                            {
                                keyValue = (weightings[8] * (sourceTransformScale.z) + (offset[8]));
                            }

                            key = new Keyframe(key.time, keyValue, key.inTangent * weightings[8], key.outTangent * weightings[8], key.inWeight, key.outWeight);

                            break;

                        default://ok i have NO idea what all the other properties are but.... HERE... COPY THEM.... we must discover the evil ones...

                            keyValue = key.value;
                            key = new Keyframe(key.time, keyValue,key.inTangent, key.outTangent, key.inWeight, key.outWeight);
                            break;

                    }//after this key has been altered.
                    #endregion
                    
                    ks[i] = key;

                } //END LOOP -- keys have been altered.

                //now that we have the altered keys, we need to adjust the binding tangents further! unfortunately key.tangentMode cant be assigned  nor copied in the above
                AnimationCurve oldCurve = curve; //to compare
                Keyframe[] oldKeys = oldCurve.keys;
                curve.keys = ks; //curve is made out of keyframe array.
                for(int i = 0; i<ks.Length; i++)
                { //get rid of any wierd nonesense
                    AnimationUtility.SetKeyRightTangentMode(curve, i,AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); 
                    AnimationUtility.SetKeyBroken(curve, i, true);
                }
                EditorCurveBinding newBinding = binding;
                newBinding.path = currentTargetPath;

                AnimationUtility.SetEditorCurve(clip, newBinding, curve); //set the target curve to the new curve.
            }
        }//ENDLOOP FOREACH: BINDINGS


        var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var binding in objectBindings)
        {
            //Debug.Log(binding.propertyName.GetType());
            //Debug.Log(binding.propertyName+ " from "+ binding.path + "OBJECT");
            if (binding.path == currentSourcePath)
            {
                if (binding.path == currentSourcePath)
                {
                    ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(clip, binding); //returns keyframes
                    ObjectReferenceKeyframe[] ks = new ObjectReferenceKeyframe[curve.Length]; //holds the sauce

                    for (int i = 0; i < curve.Length; i++)
                    {
                        ObjectReferenceKeyframe key = curve[i];
                        clip.SampleAnimation(root, key.time);

                        //Debug.Log("--->"+ sourceTransformPos + "\n" + sourceTransformScale + "\n"+ sourceWorldRot);

                        UnityEngine.Object keyValue = key.value; 
                        switch (binding.propertyName) //this is trash.... 
                        {
                            //as far as i know all object keyframes are just objects being replaced or flipped on/off -- not suitable for constraints but might be useful for something like Set Driven Keys ?
                            //if someone needs this, i will add it.

                            //BOOLEAN OPERATORS
                            //case "m_isActive":
                            default:
                                //theoretically impossible....? can you have a curve boolean? lol?
                                //keyValue = 
                                key = new ObjectReferenceKeyframe { time = key.time, value = keyValue };

                                break;


                        }//after this key has been altered.

                        ks[i] = key; //set the edited key into the keyframe array from the animation curve.

                    } //END LOOP -- keys have been altered.

                    curve = ks; //curve is made out of keyframe array.
                    EditorCurveBinding newBinding = binding;
                    newBinding.path = currentTargetPath;

                    AnimationUtility.SetObjectReferenceCurve(clip, newBinding, curve); //set the target curve to the new curve.
                }
            }
        }




    }

    /*This function returns the name of the binding. Ex. spine1/spine2/spine3/head_1  -> head_1
     * This makes it soooo much easier to read and differentiate between objects.
     * EditorCurveBinding binding: the binding....
     */
    public static string getBindingName(EditorCurveBinding binding)
    {
        string path = binding.path;
        //HashSet<string> bindingPathWords = new HashSet<string>();
        //Debug.Log(path + "\n" + binding.path);
        string currentWord = "";
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '/')
            {
                currentWord = ""; //we do not care if the word is not the last word.
                i++;//skip past this buffoon!
            }

            if (i == path.Length - 1) //had to check if we are at the end of the string otherwise it wont add the entire last word.
            {
                currentWord += path[i];

            }
            else
            {
                currentWord += path[i]; //not at the end of the string, you may continue.
            }

        }
        return currentWord;
    }


    /*This function returns the name of the binding. Ex. spine1/spine2/spine3/head_1  -> head_1
     * This makes it soooo much easier to read and differentiate between objects.
     * string bindingPath : this is the path of the binding. Look at example above.
     */
    public static string getBindingName(string bindingPath)
    {
        string path = bindingPath;
        //Debug.Log(path + "\n" + binding.path);
        string currentWord = "";
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '/')
            {
                currentWord = ""; //we do not care if the word is not the last word.
                i++;//skip past this buffoon!
            }

            if (i == path.Length - 1) //had to check if we are at the end of the string otherwise it wont add the entire last word.
            {
                currentWord += path[i];

            }
            else
            {
                currentWord += path[i];
            }

        }
        return currentWord; //temp
    }

    /*This method returns the parent string, to assist with findParentTransform.
     *  string bindingPath : the bindingPath, should be assigned
     */
    public static string getParentPath(string bindingPath)
    {
        if (bindingPath == null)
        {
            Debug.LogError("getParentName: Failed, bindingPath does was not assigned.");
            return null; //failsafe. should not be possible.
        }
        string bindingName = getBindingName(bindingPath); //sure whatever
        string parentPath = bindingPath.TrimEnd("/" + bindingName); 

        if(parentPath == currentSourcePath || parentPath == currentTargetPath) //This happens if prefab is not configured correctly. I've done it before -- saves a headache getting a warning instead.
        {
            Debug.LogError("Please make sure that your objects are under a root, under the Animation controller-- their paths shouldnt be a single name -- Recieved: " + parentPath + "\n Which isn't correct.");
            //return it anyway... 
        }

        return parentPath;

    }

    /*This method gets the parent Transform, and returns it.
     * Used to convert from world transform to local transform.
     * perhaps change this to a setter, and create another method in Cons_Object as a getter?
     * GameObject root: root game object
     * string bindingPath: the parent binding path
     */
    public static Transform getParentTransform(GameObject root, string bindingPath)
    {
        string parentPath = getParentPath(bindingPath);
        try
        {
            Transform parentTransform = root.transform.Find(parentPath);
            return parentTransform;
        }
        catch
        {
            Debug.LogError("getParentTransform: Failed to grab parent Transform.");
            return null;
        }

    }


    /*This is the function that actually creates the Scriptable Object!
     * The scriptable object stores data that can be used to reset the keys, or edit the keyframes at a later point.
     * float[] weightings: the weighting from the editorWindow. this determines how much weight the constraint has over editing the original keyframes.
     * float[] offset: Alters the position,rotation,and scale from the original. a literal.. offset.
     */
    public static void createDataObject(float[] weightings, float[] offset)
    {
        string fileName = getBindingName(currentSourcePath) + "_ConstraintTo_" + getBindingName(getBindingName(currentTargetPath)); //unsure if this is a good default fileName but this is what it is currently.
        string filePath = "Assets/Editor/AniToolkit/Constraints/" + fileName + ".asset";  //filePath will just be in the editor scripts folder because I dont have access
        Cons_Object constrainedObject = GetExistingScriptableObject();

        if (constrainedObject)
        {
            //maybe set one here? but i like using this to ensure you dont rewrite data.. though there should be a better way... 
            return; //one already exists, so we do not need to make another.
            //todo: prompt to replace data? you should be using the Edit window to make edits.
        }
        else // it is null! we need to make one.
        {
            GameObject rootReference = root;

            constrainedObject = ScriptableObject.CreateInstance<Cons_Object>();
            //set values
            constrainedObject.name = fileName;
            constrainedObject.clip = animClip;
            constrainedObject.targetPath = currentTargetPath;
            constrainedObject.sourcePath = currentSourcePath;
            constrainedObject.root = rootReference; //TYPE MISMATCH

            ///constraint and weight data
            Array.Copy(weightings, constrainedObject.mixWeightings, weightings.Length);
            Array.Copy(offset, constrainedObject.offsetValues, offset.Length);

            //transform values, rest are in createWeighted Contraints.
            constrainedObject.setTransform(targetTransform);
            constrainedObject.setTransformValuesForWeights();

            //create the object at filepath specified earlier.
            AssetDatabase.CreateAsset(constrainedObject, filePath);
            constraintReference = constrainedObject; //we can reference this for editing

        }

    }

    /*This function resets the keys to the base position by referencing constraintReference for the original curve data.
     * AnimationClip clip: this is the animation clip we are editing.
     */
    public static void resetKeysToBase(AnimationClip clip)
    {

        if (string.IsNullOrEmpty(currentTargetPath))
        {
            Debug.Log("Current Target path is empty. Maybe reassign it?");
            return;
        }
        Undo.RecordObject(clip, "Remove constraint, reset keys to base");
        var currentBindings = AnimationUtility.GetCurveBindings(clip);
        Undo.RecordObject(clip, "Reset target keys"); // safety net
        foreach (var binding in currentBindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

            if (binding.path == currentTargetPath)
            {
                AnimationUtility.SetEditorCurve(clip, binding, constraintReference.GetBackupTargetCurve(binding.propertyName));

            }
        }

    }

}
