﻿/*
 * Advaced Gesture Recognition - Unity Plug-In
 *
 * Copyright (c) 2019 MARUI-PlugIn (inc.)
 * This software is free to use for non-commercial purposes.
 * You may use this software in part or in full for any project
 * that does not pursue financial gain, including free software
 * and projectes completed for evaluation or educational purposes only.
 * Any use for commercial purposes is prohibited.
 * You may not sell or rent any software that includes
 * this software in part or in full, either in it's original form
 * or in altered form.
 * If you wish to use this software in a commercial application,
 * please contact us at support@marui-plugin.com to obtain
 * a commercial license.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
 * OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;


public class GestureController : MonoBehaviour
{
    //public SteamVR_Action_Single triggerValue = SteamVR_Actions.default_Squeeze;
    //public SteamVR_Input_Sources handType = SteamVR_Input_Sources.RightHand;
    public VRInputModule vRInputModule;
    public TrailController trailController;
    public PhotonView photonView;
    // The file from which to load gestures on startup.
    // For example: "Assets/GestureRecognition/sample_gestures.dat"
    [SerializeField] public string LoadGesturesFile;

    [SerializeField] private GameObject wandEnd;
    [SerializeField] private GameObject head;

    // File where to save recorded gestures.
    // For example: "Assets/GestureRecognition/my_custom_gestures.dat"
    [SerializeField] public string SaveGesturesFile;

    [SerializeField] public SpellManager spellManager;

    // The gesture recognition object:
    // You can have as many of these as you want simultaneously.
    private GestureRecognition gr = new GestureRecognition();

    // The text field to display instructions.
    public Text HUDText;

    // The game object associated with the currently active controller (if any):
    private GameObject active_controller = null;

    // ID of the gesture currently being recorded,
    // or: -1 if not currently recording a new gesture,
    // or: -2 if the AI is currently trying to learn to identify gestures
    // or: -3 if the AI has recently finished learning to identify gestures
    private int recording_gesture = -1;

    // Last reported recognition performance (during training).
    // 0 = 0% correctly recognized, 1 = 100% correctly recognized.
    private double last_performance_report = 0;

    // Temporary storage for objects to display the gesture stroke.
    private List<string> stroke = new List<string>();

    // Temporary counter variable when creating objects for the stroke display:
    private int stroke_index = 0;

    // List of Objects created with gestures:
    private List<GameObject> created_objects = new List<GameObject>();

    // Handle to this object/script instance, so that callbacks from the plug-in arrive at the correct instance.
    private GCHandle me;
    public ResourceManager rManager;
    // Initialization:
    private void Start()
    {
       
        
        
        // Load the default set of gestures.
        /*if (gr.loadFromFile(LoadGesturesFile) == false)
        {
            Debug.Log("Failed to load sample gesture database file");
        }*/
        Invoke("LoadTheFile", 0.3f);
        Invoke("LoadTheFile", 0.4f);

        //Debug.Log(Application.streamingAssetsPath.ToString()+ "Path");
        // Set the welcome message.
        //HUDText = GameObject.Find("HUDText").GetComponent<Text>();
        HUDText.text = "Welcome to MARUI Gesture Plug-in!\n"
                      + "Press the trigger to draw a gesture. Available gestures:\n"
                      + "1 - a circle/ring (creates a cylinder)\n"
                      + "2 - swipe left/right (rotate object)\n"
                      + "3 - shake (delete object)\n"
                      + "4 - draw a sword from your hip,\nhold it over your head (magic)\n"
                      + "or: press 'A'/'X'/Menu button\nto create new gesture.";

        me = GCHandle.Alloc(this);

        // Reset the skybox tint color
        RenderSettings.skybox.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 1.0f));
        
    }
    private void SetUpInputModule()
    {
        vRInputModule = GameObject.FindGameObjectWithTag("VRInputModule").GetComponent<VRInputModule>();
    }
    private void LoadTheOtherFile()
    {
        if (gr.loadFromFile("Assets/GestureRecognition/GestureSet2.dat"))
        {
            Debug.LogWarning("Successful load");
        }
        else
        {
            Debug.LogWarning("Unsuccessful load");
        }
    }

    private void LoadTheFile()
    {
#if UNITY_EDITOR
        gr.loadFromFile(LoadGesturesFile);
#else
                gr.loadFromFile(Application.streamingAssetsPath + "/GestureSet5Gestures180Samples.dat");
#endif

        /*if (gr.loadFromFile(LoadGesturesFile))
        {
            Debug.LogWarning("Successful load");
        }
        else
        {
            Debug.LogWarning("Unsuccessful load");
        }*/
    }

    private void StartTraining()
    {
        Debug.Log("Start training");
        gr.setTrainingUpdateCallback(trainingUpdateCallback);
        gr.setTrainingUpdateCallbackMetadata((IntPtr)me);
        gr.setTrainingFinishCallback(trainingFinishCallback);
        gr.setTrainingFinishCallbackMetadata((IntPtr)me);
        gr.startTraining();
    }

    private void FinishTraining()
    {
        Debug.Log("Stop training");
        gr.stopTraining();
        Invoke("SaveToFile", 2f);
    }

    private void SaveToFile()
    {
        Debug.Log("Save to file");
        gr.saveToFile(SaveGesturesFile);
    }

    // Update:
    private void Update()
    {

        if (Input.GetKeyDown("p"))
        {
            Debug.LogError("Pressed the p key");
            Debug.Log("Player took 15 damage");
            rManager.TakeDamage(15f);
        }
        if (Input.GetKeyDown("l"))
        {
            Debug.LogError("Pressed the L key");

            StartTraining();
        }
        if (vRInputModule!=null &&vRInputModule.rightController.GetHairTriggerUp())
        {
            trailController.TurnOffTrail();
        }
        if (spellManager.bufferedSpell!=SpellManager.Spells.NULL)
        {
            return;
        }
        //Debug.Log(last_performance_report * 100.0);
        float trigger_left = vRInputModule.leftController.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
        float trigger_right = vRInputModule.rightController.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
        Debug.Log(trigger_right);
        //float trigger_right = SteamVR_Actions.default_Squeeze.GetAxis(handType);

        // If the user is not yet dragging (pressing the trigger) on either controller, he hasn't started a gesture yet.
        if (active_controller == null)
        {
            if(vRInputModule.rightController.GetHairTriggerDown())
            {
                trailController.TurnOnTrail();
            }
            
            // If the user presses either controller's trigger, we start a new gesture.
            if (trigger_right > 0.3)
            {
                // Right controller trigger pressed.
                active_controller = wandEnd;
            }
            else if (trigger_left > 0.3)
            {
                // Left controller trigger pressed.
                active_controller = GameObject.Find("Left Hand");
            }
            else
            {
                // If we arrive here, the user is pressing neither controller's trigger:
                // nothing to do.
                return;
            }
            // If we arrive here: either trigger was pressed, so we start the gesture.
            GameObject hmd = head; // alternative: Camera.main.gameObject
            Vector3 hmd_p = hmd.transform.localPosition;
            Quaternion hmd_q = hmd.transform.localRotation;
            gr.startStroke(hmd_p, hmd_q, recording_gesture);
        }

        // If we arrive here, the user is currently dragging with one of the controllers.
        // Check if the user is still dragging or if he let go of the trigger button.
        if (trigger_left > 0.3 || trigger_right > 0.3 && spellManager.bufferedSpell == SpellManager.Spells.NULL)
        {
            
            // The user is still dragging with the controller: continue the gesture.
            Vector3 p = active_controller.transform.localPosition;
            p.z += 0.5f;
            Quaternion q = active_controller.transform.localRotation;
            gr.contdStroke(p, q);

            return;
        }
        if(spellManager.bufferedSpell == SpellManager.Spells.NULL)
        {
            // else: if we arrive here, the user let go of the trigger, ending a gesture.
            active_controller = null;

            Vector3 pos = Vector3.zero; // This will receive the position where the gesture was performed.
            double scale = 0; // This will receive the scale at which the gesture was performed.
            Vector3 dir0 = Vector3.zero; // This will receive the primary direction in which the gesture was performed (greatest expansion).
            Vector3 dir1 = Vector3.zero; // This will receive the secondary direction of the gesture.
            Vector3 dir2 = Vector3.zero; // This will receive the minor direction of the gesture (direction of smallest expansion).
            double similarity = 0;
            String gestureName = "";
            //int gesture_id = gr.endStroke(ref similarity, ref pos, ref scale, ref dir0, ref dir1, ref dir2);
            double[] grresult = gr.endStrokeAndGetAllProbabilities();
            List<double> listOfOver30Percent = new List<double>();
            int gesture_id = -1;
            for (int i = 0; i < grresult.GetLength(0); i++)
            {

                if (i == 0)
                {
                    gestureName = "Sandtimer";
                }
                if (i == 1)
                {
                    gestureName = "Circle";
                }
                if (i == 2)
                {
                    gestureName = "ThunderBolt";
                }
                if (i == 3)
                {
                    gestureName = "Fish";
                }
                if (i == 4)
                {
                    gestureName = "SwishAndFlick";
                }
                Debug.Log(gestureName + " " + grresult[i]);
                if (grresult[i] > 0.3)
                {
                    listOfOver30Percent.Add(grresult[i]);
                }
                //Debug.LogError(grresult[i].ToString() + " " + gestureName);
                if (grresult[i] > 0.92 && listOfOver30Percent.Count < 2)
                {
                    gesture_id = i;
                    //break;
                }
            }
            if (gesture_id < 0)
            {
                // Error trying to identify any gesture
                HUDText.text = "Failed to identify gesture.";
            }
            //if (similarity > 0.60)
            // {
            if (gesture_id == 0)
            {
                HUDText.text = "Sandtimer " + grresult[0];
                //+ (similarity * 100).ToString("#.0");
                if (spellManager.canCastSpells)
                {
                    spellManager.SetBufferedSpell(SpellManager.Spells.BLUELIGHTNING);
                }
            }
            else if (gesture_id == 1)
            {
                HUDText.text = "Circle " + grresult[1];
                if (spellManager.canCastSpells)
                {
                    spellManager.SetBufferedSpell(SpellManager.Spells.SHIELD);
                }
            }
            else if (gesture_id == 2)
            {
                HUDText.text = "ThunderBolt " + grresult[2];
            }
            else if (gesture_id == 3)
            {
                HUDText.text = "Fish " + grresult[3];
            }
            else if (gesture_id == 4)
            {
                HUDText.text = "Swish " + grresult[4];
              
            }
            
        }

    }

    // Callback function to be called by the gesture recognition plug-in during the learning process.
    public static void trainingUpdateCallback(double performance, IntPtr ptr)
    {
        // Get the script/scene object back from metadata.
        GCHandle obj = (GCHandle)ptr;
        GestureController me = (obj.Target as GestureController);
        // Update the performance indicator with the latest estimate.
        me.last_performance_report = performance;
        Debug.LogError(performance);
    }

    // Callback function to be called by the gesture recognition plug-in when the learning process was finished.
    public static void trainingFinishCallback(double performance, IntPtr ptr)
    {
        Debug.LogError("Gesture training finished");
        // Get the script/scene object back from metadata.
        GCHandle obj = (GCHandle)ptr;
        GestureController me = (obj.Target as GestureController);
        // Update the performance indicator with the latest estimate.
        me.last_performance_report = performance;
        // Signal that training was finished.
        me.recording_gesture = -3;
        // Save the data to file.
        me.gr.saveToFile(me.SaveGesturesFile);
        Debug.LogError("Training finished");
    }
}