using ICities;
using Harmony;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using ColossalFramework;
/* Notes

 */
 
namespace HotReload.Patches {
    using System;
    using UnityEngine;
    using Utils;
    using static TranspilerUtils;
    public static class CalculateMaterialCommons {
        public static bool ShouldHideCrossing(ushort nodeID, ushort segmentID) {
            return false;
        }


        public static Material CalculateMaterial(Material material, ushort nodeID, ushort segmentID) {
            return material;
        }

        static Type[] args = new [] {
                typeof(Mesh),
                typeof(Vector3),
                typeof(Quaternion),
                typeof(Material),
                typeof(int),
                typeof(Camera),
                typeof(int),
                typeof(MaterialPropertyBlock) };
        static MethodInfo mDrawMesh => typeof(Graphics).GetMethod("DrawMesh", args);
        static FieldInfo fNodeMaterial => typeof(NetInfo.Node).GetField("m_nodeMaterial");
        static MethodInfo mCalculateMaterial => typeof(CalculateMaterialCommons).GetMethod("CalculateMaterial");
        static MethodInfo mCheckRenderDistance => typeof(RenderManager.CameraInfo).GetMethod("CheckRenderDistance");
        static MethodInfo mShouldHideCrossing => typeof(CalculateMaterialCommons).GetMethod("ShouldHideCrossing");
        static MethodInfo mGetSegment => typeof(NetNode).GetMethod("GetSegment");

        // returns the position of First DrawMesh after index.
        public static void PatchCheckFlags(List<CodeInstruction> codes, int occurance, MethodInfo method) {
            Extensions.Assert(mDrawMesh != null, "mDrawMesh!=null failed");
            Extensions.Assert(fNodeMaterial != null, "fNodeMaterial!=null failed"); 
            Extensions.Assert(mCalculateMaterial != null, "mCalculateMaterial!=null failed"); 
            Extensions.Assert(mCheckRenderDistance != null, "mCheckRenderDistance!=null failed"); 
            Extensions.Assert(mShouldHideCrossing != null, "mShouldHideCrossing!=null failed");

            int index = 0;
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Call, mDrawMesh), index, counter: occurance);
            Extensions.Assert(index != 0, "index!=0");


            // find ldfld node.m_material
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, fNodeMaterial), index, dir: -1);
            int insertIndex2 = index + 1;

            // find: if (cameraInfo.CheckRenderDistance(data.m_position, node.m_lodRenderDistance))
            /* IL_0627: callvirt instance bool RenderManager CameraInfo::CheckRenderDistance(Vector3, float32)
             * IL_062c brfalse      IL_07e2 */
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, mCheckRenderDistance), index, dir: -1);
            int insertIndex1 = index + 1; // at this point boloean is in stack


            CodeInstruction LDArg_NodeID = GetLDArg(method, "nodeID"); // push nodeID into stack
            CodeInstruction LDLoc_segmentID = BuildSegnentLDLocFromPrevSTLoc(codes, index, counter: 1); // push segmentID into stack

            { // Insert material = CalculateMaterial(material, nodeID, segmentID)
                var newInstructions = new[] {
                    LDArg_NodeID,
                    LDLoc_segmentID,
                    new CodeInstruction(OpCodes.Call, mCalculateMaterial), // call Material CalculateMaterial(material, nodeID, segmentID).
                };
                InsertInstructions(codes, newInstructions, insertIndex2);
            }

            { // Insert ShouldHideCrossing(nodeID, segmentID)
                var newInstructions = new[]{
                    LDArg_NodeID, 
                    LDLoc_segmentID, 
                    new CodeInstruction(OpCodes.Call, mShouldHideCrossing), // call Material mShouldHideCrossing(nodeID, segmentID).
                    new CodeInstruction(OpCodes.Or) };

                InsertInstructions(codes, newInstructions, insertIndex1);
            } // end block
        } // end method

        public static CodeInstruction BuildSegnentLDLocFromPrevSTLoc(List<CodeInstruction> codes, int index, int counter=1) {
            Extensions.Assert(mGetSegment != null, "mGetSegment!=null");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Call, mGetSegment), index, counter: counter, dir: -1);

            var code = codes[index + 1];
            Extensions.Assert(IsStLoc(code), $"IsStLoc(code) | code={code}");

            return BuildLdLocFromStLoc(code);
        }



    }
}
