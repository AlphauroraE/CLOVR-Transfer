//MIT License

//Copyright (c) 2017 Ben Otter

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
using System.Collections;
using UnityEngine;
using Valve.VR;

public partial class OVR_Pose_Handler 
{
    public Vector3 GetEyeTransform(EVREye eye) 
    {
        if(!SysExists)
            return Vector3.zero;

        return (new OVR_Utils.RigidTransform(VRSystem.GetEyeToHeadTransform(eye))).pos;
    }

    public Vector3 GetRightEyeTransform()
    {
        return GetEyeTransform(EVREye.Eye_Right);
    }

    public Vector3 GetLeftEyeTransform()
    {
        return GetEyeTransform(EVREye.Eye_Left);
    }
}