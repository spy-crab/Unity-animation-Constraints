using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * This class is used to store constraint information for the constraint editor to access and edit afterwards.
 * 
 */

public class Cons_Object : ScriptableObject
{
    [NonSerialized]
    public string sourcePath;
    [NonSerialized]
    public string targetPath;
    [NonSerialized]
    public AnimationClip clip;
    //TODO: make private so that it keeps info correctly... actually redundant because this info will never change?
    [NonSerialized]
    public GameObject root;

    [NonSerialized]
    public Transform originalTransform; //this is to calculate weighting math. -- it cannot be saved
    public Vector3 targetWorldTransformPos; //can be manually edited if needed.. you probably shouldnt though.
    public Vector3 targetWorldRot;
    public Vector3 targetWorldScale;


    public float[] mixWeightings = new float[9]; //this is silly.. maybe
                                                 //TRANS - 0,1,2   x,y,z
                                                 //ROT   - 3,4,5     x,y,z
                                                 //SCALE  - 6,7,8   x,y,z
                                                 //keep this in the container lol

    public float[] offsetValues = { 0, 0, 0 , //TRANS - 0,1,2   x,y,z
                        0 , 0 , 0, //ROT   - 3,4,5     x,y,z
                        0 , 0 , 0}; //SCALE  - 6,7,8   x,y,z


    [Serializable]  //required information to reference. Also allows the user to edit the base values manually
    public class CurveBackup
    {
        public string path;
        public string propertyName;
        public AnimationCurve curve;

    }

    public List<CurveBackup> backupInfo = new List<CurveBackup>();

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    //GETTERS AND SETTERS//////////////////////////////////////////////////////
    public Transform getTransform()
    {
        return originalTransform;
    }

    public void setTransform(Transform transform)
    {
        originalTransform = transform;
    }

    //returns weight values
    public float getWeightData(int index)
    {
        return mixWeightings[index];
    }

    //returns offset values
    public float getOffsetData(int index)
    {
        return offsetValues[index];
    }

    //////////////////////////////////METHODS/////////////////////////////////////////////////////////////////////////
    public void setTransformValuesForWeights() //og position
    {
        targetWorldTransformPos = originalTransform.localPosition; 
        targetWorldScale = originalTransform.localScale; //jittery?
        targetWorldRot = originalTransform.localRotation.eulerAngles; //quaternion converted to euler.
    }


    public void setWeightings(float[] weightVal)
    {

        for(int i = 0; i < 9; i++) //if the user adds more to try and break it, not gonna look at it. 
        {
            //Debug.Log(weightVal[i] + "weight");
            mixWeightings[i] = weightVal[i];
        }
    }

    public void setOffset(float[] offsetVal)
    {
        for(int i = 0; i<9; i++) //if the user adds more to try and break it, not gonna look at it.
        {
            //Debug.Log(offsetVal[i] + "offset");
            offsetValues[i] = offsetVal[i];
        }
    }


    //method to help us get the backup info to reset back to default
    public AnimationCurve GetBackupTargetCurve(string property)
    {
        //loop through backup list. return them 
        foreach (CurveBackup backup in backupInfo)
        {
            if (backup.propertyName == property)
            {
                return backup.curve;
            }
        }
        return null; //found nothing
    }


}
