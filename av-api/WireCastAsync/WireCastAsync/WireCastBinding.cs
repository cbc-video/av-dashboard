using System;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json;
using NLog;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;

namespace WireCastAsync
{
    /// <summary>
    /// Proxy Object for the Late binding
    /// Provide more logical API for bindings
    /// </summary>
    public class WirecastBinding
    {
        private object _Wirecast;
        private Logger log = LogManager.GetCurrentClassLogger();

        public Wirecast Get()
        {
            return new Wirecast
            {
                isRecording = IsRecording(),
                isBroadcasting = IsBroadcasting(),
                previewShotId = GetPreviewShotID(),
                programShotId = GetActiveShotID()
            };
        }

        public Wirecast Set(dynamic input)
        {
            if (input == null) return null;
            log.Info("setting with input: " + input.ToString());

            Wirecast inputWirecast = Parse(input);
            if (inputWirecast == null) return null;
            if(inputWirecast.isRecording != IsRecording())
            {
                if (inputWirecast.isRecording) StartRecording();
                else StopRecording();
            }
            if(inputWirecast.isBroadcasting != IsBroadcasting())
            {
                if (inputWirecast.isBroadcasting) StartBroadcast();
                else StopBroadcast();
            }
            
            return Get();
        }

        public Wirecast Parse(ExpandoObject input)
        {
            IDictionary<string, object> json = input as IDictionary<string, object>;
            //JsonConvert.DeserializeObject<Wirecast>(input.ToString());
            Wirecast wirecast = new Wirecast();
            object value;
            if (json.TryGetValue("isBroadcasting", out value)) wirecast.isBroadcasting = (bool)value;            
            if (json.TryGetValue("isRecording", out value)) wirecast.isRecording = (bool)value;
            if (json.TryGetValue("previewShotId", out value)) wirecast.previewShotId = (int)value;
            if (json.TryGetValue("programShotId", out value)) wirecast.programShotId = (int)value;
            return wirecast;
        }

        private object _Document;
        private object _SelectedLayer;
        private int _SelectedLayerIndex;

        /// <summary>
        /// Late binding helper class.
        /// static bindings to help you get/set via OLE/COM.
        /// </summary>
        class Late
        {
            public static void Set(object obj, string sProperty, object oValue)
            {
                object[] oParam = new object[1];
                oParam[0] = oValue;
                obj.GetType().InvokeMember(sProperty, BindingFlags.SetProperty, null, obj, oParam);
            }
            public static object Get(object obj, string sProperty, object oValue)
            {
                object[] oParam = new object[1];
                oParam[0] = oValue;
                return obj.GetType().InvokeMember(sProperty, BindingFlags.GetProperty, null, obj, oParam);
            }
            public static object Get(object obj, string sProperty, object[] oValue)
            {
                return obj.GetType().InvokeMember(sProperty, BindingFlags.GetProperty, null, obj, oValue);
            }
            public static object Get(object obj, string sProperty)
            {
                return obj.GetType().InvokeMember(sProperty, BindingFlags.GetProperty, null, obj, null);
            }
            public static object Invoke(object obj, string sProperty, object[] oParam)
            {
                return obj.GetType().InvokeMember(sProperty, BindingFlags.InvokeMethod, null, obj, oParam);
            }
            public static object Invoke(object obj, string sProperty, object oValue)
            {
                object[] oParam = new object[1];
                oParam[0] = oValue;
                return obj.GetType().InvokeMember(sProperty, BindingFlags.InvokeMethod, null, obj, oParam);
            }
            public static object Invoke(object obj, string sProperty, object oValue, object oValue2)
            {
                object[] oParam = new object[2];
                oParam[0] = oValue;
                oParam[1] = oValue2;
                return obj.GetType().InvokeMember(sProperty, BindingFlags.InvokeMethod, null, obj, oParam);
            }
            public static object Invoke(object obj, string sProperty)
            {
                return obj.GetType().InvokeMember(sProperty, BindingFlags.InvokeMethod, null, obj, null);
            }
        }

        /// <summary>
        /// Initialize the Binding object with Document Index 1
        /// And the Normal Layer (3) Selected
        /// </summary>
        public void Initialize()
        {
            _Wirecast = null;
            _Document = null;
            _SelectedLayer = null;
            _SelectedLayerIndex = -1;
            try
            {
                _Wirecast = Marshal.GetActiveObject("Wirecast.Application");
            }
            catch
            {
                Type objClassType = Type.GetTypeFromProgID("Wirecast.Application");
                _Wirecast = Activator.CreateInstance(objClassType);
            }

            SwitchDocument(1);
        }
        
        public WirecastBinding()
        {
            Initialize();
        }

        /// <summary>
        /// Returns a string array of the layer names
        /// </summary>
        public string[] ValidLayerNames()
        {
            return new string[] { "text", "overlay", "normal", "underlay", "audio" };
        }

        /// <summary>
        /// Converts a given Layer Name to the associated Index value
        /// If the name is invalid, returns -1
        /// </summary>
        public int LayerNameToIndex(string layer)
        {
            string[] layerNames = ValidLayerNames();
            for (int i = 0; i < 5; i++)
            {
                if (layerNames[i] == layer)
                {
                    return i + 1;
                }
            }

            return -1;
        }
        /// <summary>
        /// Get the Name of the layer associated with the index value.
        /// If the index is out of bounds, returns an empty string
        /// </summary>
        public string LayerIndexToName(int index)
        {
            if (index >= 1 && index <= 5)
            {
                string[] layerNames = ValidLayerNames();
                return layerNames[index - 1];
            }

            return "";
        }

        /// <summary>
        /// Switches the internal document of the binding object to the new one
        /// The seletec layer of the new document will be the same as the previous document
        /// So if the selected layer was "text"/1, then that is the selected layer of the new document
        /// All future call to the Document and Layer API will apply to the new document
        /// </summary>
        public bool SwitchDocument(int index)
        {
            bool validDoc = false;
            object newDocument = Late.Invoke(_Wirecast, "DocumentByIndex", index);

            if (newDocument != null)
            {
                _Document = newDocument;
                validDoc = true;

                //  After Change the Document, we need to update the layer
                SwitchLayer(3);
            }

            return validDoc;
        }

        /// <summary>
        /// Same as SwitchDocument with an index except indead of a document index, use the document name
        /// </summary>
        public bool SwitchDocument(string docName)
        {
            bool validDoc = false;
            object newDocument = Late.Invoke(_Wirecast, "DocumentByName", docName);

            if (newDocument != null)
            {
                _Document = newDocument;
                validDoc = true;

                //  After Change the Document, we need to update the layer
                SwitchLayer(_SelectedLayerIndex); // 3 is the "normal" layer
            }

            return validDoc;
        }

        /// <summary>
        /// Returns the index of the currently selected layer
        /// </summary>
        public int GetSelectedLayerIndex()
        {
            return _SelectedLayerIndex;
        }

        /// <summary>
        /// Returns the name of the currently selected layer
        /// </summary>
        public string GetSelectedLayerName()
        {
            return LayerIndexToName(_SelectedLayerIndex);
        }

        /// <summary>
        /// Returns the number of master layers in the system
        /// </summary>
        public int GetMasterLayerCount()
        {
            return 5;
        }

        /// <summary>
        /// Change the internal selected layer of the binding object
        /// All future calls to layer related APIs will be use this new layer
        /// </summary>
        public bool SwitchLayer(int index)
        {
            bool hasLayer = false;
            string layerName = LayerIndexToName(index);

            if (layerName != "")
            {
                _SelectedLayer = Late.Invoke(_Document, "LayerByName", layerName);
                _SelectedLayerIndex = index;
                hasLayer = true;
            }

            return hasLayer;
        }

        /// <summary>
        /// Change the internal selected layer of the binding object
        /// All future calls to layer related APIs will be use this new layer
        /// </summary>
        public bool SwitchLayer(string name)
        {
            bool validName = false;
            int index = LayerNameToIndex(name.ToLower());

            if (index != -1)
            {
                SwitchLayer(index);
                validName = true;
            }

            return validName;
        }

        /// <summary>
        /// Return whether or not the current document is broadcasting a stream
        /// </summary>
        public bool IsBroadcasting()
        {
            int isBroadcasting = (int)Late.Invoke(_Document, "IsBroadcasting");
            return isBroadcasting == 1;
        }

        /// <summary>
        /// Toggles the Broadcast state
        /// If it is not broadcasting, start a broadcast
        /// If it is, stop it
        /// </summary>
        public void ToggleBroadcast()
        {
            if (IsBroadcasting())
            {
                StopBroadcast();
            }
            else
            {
                StartBroadcast();
            }
        }
        public void StartBroadcast()
        {
            Late.Invoke(_Document, "Broadcast", "start");
        }
        public void StopBroadcast()
        {
            Late.Invoke(_Document, "Broadcast", "stop");
        }

        /// <summary>
        /// Returns whether or not the current document is recording to disk
        /// </summary>
        public bool IsRecording()
        {
            int isRecording = (int)Late.Invoke(_Document, "IsArchivingToDisk");
            return isRecording == 1;
        }

        /// <summary>
        /// Toggles the Record to Disk state
        /// If it is not recording, start the recording
        /// If it is, stop it
        /// </summary>
        public void ToggleRecord()
        {
            if (IsRecording())
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }
        public void StartRecording()
        {
            Late.Invoke(_Document, "ArchiveToDisk", "start");
        }
        public void StopRecording()
        {
            Late.Invoke(_Document, "ArchiveToDisk", "stop");
        }

        /// <summary>
        /// Returns the total umber of shots in the selected layer
        /// To change which layer to count the shots in, use switch layers
        /// To get the total number of shots in the document, use GetTotalShotCount
        /// </summary>
        public int GetShotCount()
        {
            int shotCount = (int)Late.Invoke(_SelectedLayer, "ShotCount");
            return shotCount;
        }

        /// <summary>
        /// Returns the total number of shots in the document
        /// </summary>
        public int GetTotalShotCount()
        {
            int totalLayers = GetMasterLayerCount();
            int totalShotCount = 0;
            for (int i = 0; i < totalLayers; i++)
            {
                totalShotCount += (int)Late.Invoke(i, "ShotCount");
            }

            return totalShotCount;
        }

        /// <summary>
        /// Returns the ShotID for associated with the Shot name
        /// The name is the text under the shot in the Shot bin
        /// </summary>
        public int GetShotIDByName(string name)
        {
            int shotID = (int)Late.Invoke(_SelectedLayer, "ShotIDByName", name, 2);
            return shotID;
        }

        /// <summary>
        /// Returns the ShotID for the index within the selected layer
        /// The index is Zero based and from left to right in the layer
        /// </summary>
        public int GetShotIDByIndex(int index)
        {
            int shotID = (int)Late.Invoke(_SelectedLayer, "ShotIDByIndex", index);
            return shotID;
        }

        /// <summary>
        /// Returns the Shot COM object for the index
        /// Usually would not need to call this method directly
        ///
        /// Returns null if there are no shots assoicated witht he index
        /// </summary>
        public object GetShotWithIndex(int index)
        {
            object shot = null;
            int shotID = GetShotIDByIndex(index);

            if (shotID != 0)
            {
                shot = GetShotWithID(shotID);
            }

            return shot;
        }

        /// <summary>
        /// Returns the Shot COM object for the shotID
        /// Usually would not need to call this method directly
        ///
        /// Returns null if there are no shots assoicated witht he shotID
        /// </summary>
        public object GetShotWithID(int shotID)
        {
            return Late.Invoke(_Document, "ShotByShotID", shotID);
        }

        /// <summary>
        /// Returns the Shot COM object for the name
        /// Usually would not need to call this method directly
        ///
        /// Returns null if there are no shots assoicated witht he name
        /// </summary>
        public object GetShotWithName(string name)
        {
            int shotID = GetShotIDByName(name);
            return GetShotWithID(shotID);
        }

        /// <summary>
        /// Returns the name of the shot associated with the shotID
        /// If there are no shots associated with that ID, returns an empty string
        /// </summary>
        public string GetShotNameWithID(int shotID)
        {
            object shot = GetShotWithID(shotID);
            if (shot != null)
            {
                return (string)Late.Get(shot, "Name");
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Returns the name of the shot at "index"
        /// If there are no shots associated with the index, returns an empty string
        /// </summary>
        public string GetShotNameWithIndex(int index)
        {
            int shotID = GetShotIDByIndex(index);
            return GetShotNameWithID(shotID);
        }

        /// <summary>
        /// Sets the name of the shot associated with the shot ID
        /// Returns true if the name was set successfully
        /// Returns false if there is no shot associated with the shot ID
        /// </summary>
        public bool SetShotNameWithID(int shotID, string newName)
        {
            bool isShotValid = false;
            object shot = GetShotWithID(shotID);

            if (shot != null)
            {
                Late.Set(shot, "Name", newName);
                isShotValid = true;
            }

            return isShotValid;
        }

        /// <summary>
        /// Returns true if the name was set successfully
        /// Returns false if there is no shot called "oldName"
        /// </summary>
        public bool SetShotNameWithName(string oldName, string newName)
        {
            bool isShotValid = false;
            object shot = GetShotWithName(oldName);
            if (shot != null)
            {
                Late.Set(shot, "Name", newName);
                isShotValid = true;
            }
            return isShotValid;
        }

        /// <summary>
        /// Returns true if the speed input string is a valid transition speed name
        /// Returns false if it is not
        /// </summary>
        private bool isSpeedValid(string speed)
        {
            bool isValid = (speed == "slowest" || speed == "slow" || speed == "normal" || speed == "faster" || speed == "fastest");
            return isValid;
        }

        /// <summary>
        /// Returns a string array of all the valid Transition Speeds
        /// </summary>
        public string[] GetValidTransitionSpeeds()
        {
            return new string[] { "slowest", "slow", "normal", "faster", "fastest" };
        }

        /// <summary>
        /// Returns the currently select Transition speed name
        ///
        /// For a list of valid transition speeds, see GetValidTransitionSpeeds()
        /// </summary>
        public string GetTransitionSpeed()
        {
            string transSpeed = (string)Late.Get(_Document, "TransitionSpeed");
            return transSpeed;
        }

        /// <summary>
        /// Sets the Transition speed of the durrent document
        /// Returns true if the speed is valie
        /// Returns false if the speed is invalid
        ///
        /// For a list of valid transition speeds, see GetValidTransitionSpeeds()
        /// </summary>
        public bool SetTransitionSpeed(string speed)
        {
            string lowerSpeed = speed.ToLower();
            bool isValidSpeed = isSpeedValid(lowerSpeed);
            if (isValidSpeed)
            {
                Late.Set(_Document, "TransitionSpeed", lowerSpeed);
            }

            return isValidSpeed;
        }

        /// <summary>
        /// Returns the current sate of AutoLive in the selected document
        /// </summary>
        public bool IsAutoLiveOn()
        {
            int autoLiveOn = (int)Late.Get(_Document, "AutoLive");
            return autoLiveOn == 1;
        }

        /// <summary>
        /// Toggles the AutoLive state
        /// If it is off, turn it on
        /// If it is on, turn it off
        /// </summary>
        public void ToggleAutoLive()
        {
            SetAutoLive(!IsAutoLiveOn());
        }
        public void SetAutoLive(bool on)
        {
            Late.Set(_Document, "AutoLive", on);
        }

        /// <summary>
        /// Returns true if the index maps to a Transition popup in Wirecast, false otherwise
        /// </summary>
        public bool isTransitionIndexValid(int index)
        {
            return (index == 1 || index == 2);
        }

        /// <summary>
        /// Returns the index of the currently active Transition popup
        /// A value of 0 represents the left most popup in the Wirecast UI
        /// </summary>
        public int GetActiveTransitionIndex()
        {
            int activeTransIndex = (int)Late.Get(_Document, "ActiveTransitionIndex");
            return activeTransIndex;
        }

        /// <summary>
        /// Set the Active Transition popup in Wirecast
        /// A value of 0 represents the left most popup in the Wirecast UI
        /// </summary>
        public bool SetActiveTransitionIndex(int index)
        {
            bool isIndexValid = isTransitionIndexValid(index);
            if (isIndexValid)
            {
                Late.Set(_Document, "ActiveTransitionIndex", index);
            }
            return isIndexValid;
        }

        /// <summary>
        /// Returns true if the Audio is muted tot he speakers
        /// </summary>
        public bool IsAudioMutedToSpeakers()
        {
            int audioMuted = (int)Late.Get(_Document, "AudioMutedToSpeaker");
            return audioMuted == 1;
        }
        public void ToggleAudioMutedToSpeakers()
        {
            bool audioMuted = IsAudioMutedToSpeakers();
            SetAudioMutedToSpeakers(!audioMuted);
        }
        public void SetAudioMutedToSpeakers(bool muted)
        {
            Late.Set(_Document, "AudioMutedToSpeaker", muted);
        }

        /// <summary>
        /// Takes a snapshot still image of the current output and saves it as a JPEG to the given path
        /// </summary>
        public void SaveSnapshot(string path)
        {
            Late.Invoke(_Document, "SaveSnapshot", path);
        }

        /// <summary>
        /// Remove the media asset at the given path from Wirecast
        /// The path is not the shot name, but the actual media location on disk
        /// </summary>
        public void RemoveMedia(string path)
        {
            Late.Invoke(_Document, "RemoveMedia", path);
        }

        /// <summary>
        /// Creates a new shot with the asset located in the given path and adds it to the currently selected layer
        /// </summary>
        public int AddShotWithMedia(string path)
        {
            int shotID = (int)Late.Invoke(_SelectedLayer, "AddShotWithMedia", path);
            return shotID;
        }

        /// <summary>
        /// Removes the shot with the given ID from the currently selected layer
        /// Does nothing if the shot ID is invalid or not associated with any shots in the currently selected layer
        /// </summary>
        public void RemoveShotWithID(int shotID)
        {
            Late.Invoke(_SelectedLayer, "RemoveShotByID", shotID);
        }

        /// <summary>
        /// Removes the shot with the given name from the currently selected layer
        /// Does nothing if the name is not associated with any shots in the currently selected layer
        /// </summary>
        public void RemoveShotWithName(string name)
        {
            int shotID = GetShotIDByName(name);
            RemoveShotWithID(shotID);
        }

        /// <summary>
        /// Makes the active shot of the selected layer go live
        /// </summary>
        public void Go()
        {
            Late.Invoke(_SelectedLayer, "Go");
        }

        /// <summary>
        /// Returns true if the currently selected layer is visible, false otherwise
        /// </summary>
        public bool IsLayerVisible()
        {
            int visible = (int)Late.Get(_SelectedLayer, "Visible");
            return visible == 1;
        }

        /// <summary>
        /// Toggles the selected layer's visibility
        /// </summary>
        public void ToggleLayerVisibility()
        {
            bool visible = IsLayerVisible();
            Late.Set(_SelectedLayer, "Visible", !visible);
        }

        /// <summary>
        /// Returns true if the shot ID is the ID of current active shot of the currently selected layer
        /// Returns false if the shot ID is invalid or not in the currently selected layer
        /// </summary>
        public bool IsActiveShot(int shotID)
        {
            int activeShotID = GetActiveShotID();
            return activeShotID == shotID;
        }

        /// <summary>
        /// Returns true if the name is the name of the current active shot of the currently selected layer
        /// Returns false if the name is not
        /// </summary>
        public bool IsActiveShot(string name)
        {
            string activeShotName = GetActiveShotName();
            return activeShotName == name;
        }

        /// <summary>
        /// Returns the shot ID of the active shot, of the currently selected layer
        /// The Active shot is equivilent to the shot the user has clicked
        /// It doesn't mean the shot that is currently live or in preview, though it is possible the active shot is live or in preview
        /// </summary>
        public int GetActiveShotID()
        {
            int shotID = (int)Late.Get(_SelectedLayer, "ActiveShotID");
            return shotID;
        }

        /// <summary>
        /// Sets the active shot of the currently selected layer
        /// The Active shot is equivilent to the shot the user has clicked, so this method is the same as when a user clicked the shot
        /// It doesn't mean the shot that is currently live or in preview, though it is possible the active shot is live or in preview
        /// </summary>
        public bool SetActiveShot(int shotID)
        {
            bool isShotValid = false;
            object shot = GetShotWithID(shotID);
            if (shot != null)
            {
                Late.Set(_SelectedLayer, "ActiveShotID", shotID);
            }
            return isShotValid;
        }
        public bool SetActiveShot(string name)
        {
            bool isShotValid = false;
            object shot = GetShotWithName(name);
            if (shot != null)
            {
                int shotID = GetShotIDByName(name);
                Late.Set(_SelectedLayer, "ActiveShotID", shotID);
            }
            return isShotValid;
        }

        /// <summary>
        /// Returns the shot info of the shot currently in preview, of the currently selected layer
        /// The shot in preview is equivilent to the active shot
        /// </summary>
        public int GetPreviewShotID()
        {
            int shotID = (int)Late.Invoke(_SelectedLayer, "PreviewShotID");
            return shotID;
        }
        public string GetPreviewShotName()
        {
            int shotID = GetPreviewShotID();
            return GetShotNameWithID(shotID);
        }
        public object GetPreviewShot()
        {
            int shotID = GetPreviewShotID();
            return GetShotWithID(shotID);
        }

        /// <summary>
        /// Returns the shot info of the shot currently live, in the currently selected layer
        /// </summary>
        public int GetLiveShotID()
        {
            int shotID = (int)Late.Invoke(_SelectedLayer, "LiveShotID");
            return shotID;
        }
        public string GetLiveShotName()
        {
            int shotID = GetLiveShotID();
            return GetShotNameWithID(shotID);
        }
        public object GetLiveShot()
        {
            int shotID = GetLiveShotID();
            return GetShotWithID(shotID);
        }

        /// <summary>
        /// Returns the name of the active shot in the currently selected layer
        /// </summary>
        public string GetActiveShotName()
        {
            int shotID = GetActiveShotID();
            return GetShotNameWithID(shotID);
        }

        /// <summary>
        /// Returns true if the shot is currently in preview
        /// false otherwise
        /// </summary>
        public bool IsShotIDInPreview(int shotID)
        {
            object shot = GetShotWithID(shotID);
            int result = (int)Late.Invoke(shot, "Preview");

            return result == 1;
        }
        public bool IsShotNameInPreview(string name)
        {
            object shot = GetShotWithName(name);
            int result = (int)Late.Invoke(shot, "Preview");

            return result == 1;
        }

        /// <summary>
        /// Returns true if the shot is currently live
        /// false otherwise
        /// </summary>
        public bool IsShotIDLive(int shotID)
        {
            object shot = GetShotWithID(shotID);
            int result = (int)Late.Invoke(shot, "Live");

            return result == 1;
        }
        public bool IsShotNameLive(string name)
        {
            object shot = GetShotWithName(name);
            int result = (int)Late.Invoke(shot, "Live");

            return result == 1;
        }

        /// <summary>
        /// Returns true if the shot is a Playlist Shot
        /// false otherwise
        /// </summary>
        public bool IsPlaylistByShotID(int shotID)
        {
            object shot = GetShotWithID(shotID);
            int result = (int)Late.Invoke(shot, "Playlist");

            return result == 1;
        }
        public bool IsPlaylistByShotName(string name)
        {
            object shot = GetShotWithName(name);
            int result = (int)Late.Invoke(shot, "Playlist");

            return result == 1;
        }

        /// <summary>
        /// Tells the input Playlist Shot to transition to the next shot
        /// </summary>
        public void NextShotByShotID(int shotID)
        {
            object shot = GetShotWithID(shotID);
            Late.Invoke(shot, "NextShot");
        }
        public void NextShotByShotName(string name)
        {
            object shot = GetShotWithName(name);
            Late.Invoke(shot, "NextShot");
        }

        /// <summary>
        /// Tells the input Playlist Shot to transition to the previous shot
        /// </summary>
        public void PreviousShotByShotID(int shotID)
        {
            object shot = GetShotWithID(shotID);
            Late.Invoke(shot, "PreviousShot");
        }
        public void PreviousShotByShotName(string name)
        {
            object shot = GetShotWithName(name);
            Late.Invoke(shot, "PreviousShot");
        }
    }
}