using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity;
using UnityEngine;
using BepInEx;
using RoR2.UI;
using UnityEngine.UI;
using RoR2;
using Random = System.Random;
using BepInEx.Configuration;
using UnityEngine.Networking;
using RoR2.Hologram;
namespace MiniMap
{
    // CUSTOM CLASSES
    [Serializable]
    public class Player {
        public bool EXISTS = false;
        public bool IS_HOST = false;
        RenderTexture texture = null;
        public Camera camera = null;
        public RawImage rawImage = null;
        public RectTransform rawImageTransform = null;
        public GameObject gameObject = null;
        bool destroyed = false;
        MiniMapConfig config;
        public PlayerCharacterMasterController controller;
        public Player(PlayerCharacterMasterController char_controller, MiniMapConfig config) {
            controller = char_controller;
            gameObject = controller.master.GetBody().gameObject;
            this.config = config;
            if (controller.isServer) {
                IS_HOST = true;
            }
            Debug("MiniMap: Added player: " + controller.GetDisplayName() + " Host: " + IS_HOST);
        }
        private void Debug(string s) {
            UnityEngine.Debug.LogWarning("MiniMap: " + s);
        }
        public bool __init__() {
            Debug("MiniMap Initializing Player" + controller.GetDisplayName());
            if (this.EXISTS) {
                Debug("Attempted to initialize an already initialized character.");
                return false;
            }
            camera = SpawnCamera();
            texture = AssignTexture();
            rawImage = SpawnRawImage();
            this.EXISTS = true;
            return true;
        }
        public Camera SpawnCamera(Transform parent = null) {
            Debug("Spawning camera");
            if (parent == null) {
                parent = gameObject.transform;
            }
            // LIGHT
            GameObject new_light_object = new GameObject();
            Light new_light = new_light_object.AddComponent<Light>();
            new_light.type = LightType.Directional;
            new_light.cullingMask = 1 << 16;
            new_light.intensity = 3;
            // CREATE
            // VERIFY THAT THE CAMERA DOESN'T ALREADY EXISTS BECUASE OTHER MODS SOMETIMES DON'T GRACEFULLY TRANSITION SCENES

            GameObject new_camera = new GameObject();
            Camera nc = new_camera.AddComponent<Camera>();
            new_camera.name = "MINI_MAP_CAMERA";
            // MODIFY CAMERA
            new_camera.transform.SetParent(parent);
            new_camera.transform.position = parent.position + new Vector3(0, 100, 0);
            new_camera.transform.Rotate(new Vector3(90, -90, 0));
            // CAMERA SETTINGS
            nc.orthographic = true;
            nc.orthographicSize = config.zoom_level.Value;
            nc.cullingMask = 1 << 16;
            nc.nearClipPlane = 0;
            nc.farClipPlane = 10000;
            nc.allowHDR = false;
            nc.renderingPath = RenderingPath.Forward;
            nc.useOcclusionCulling = false;
            nc.allowMSAA = false;
            Color new_color = Color.black;
            new_color.a = 0.4f;
            nc.backgroundColor = new_color;
            nc.clearFlags = CameraClearFlags.SolidColor;
            Debug("Spawned Camera: " + new_camera.name + " Pos: " + new_camera.transform.position);
            // RETURN
            return nc;
        }
        public RenderTexture AssignTexture(RenderTexture textureToBeAssigned = null) {
            Debug("Assigning Texture to Camera");
            if (textureToBeAssigned == null) {
                textureToBeAssigned = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            }
            if (camera == null) {
                SpawnCamera();
            }
            camera.targetTexture = textureToBeAssigned;
            return textureToBeAssigned;
        }
        public RawImage SpawnRawImage() {
            Debug("Spawning Image");
            RawImage new_image = null;
            if (GameObject.Find("MINI_MAP_IMAGE") != null)
            {
                Debug("Found Existing RawImage");
                new_image = GameObject.Find("MINI_MAP_IMAGE").GetComponent<RawImage>();
            }
            else
            {
                GameObject new_canvas = new GameObject();
                Canvas canvas = new_canvas.AddComponent<Canvas>();
                new_canvas.name = "MINI_MAP_CANVAS";
                canvas.sortingOrder = 1;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                //new_canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                GameObject new_obj = new GameObject();
                new_obj.name = "MINI_MAP_IMAGE";
                new_image = new_obj.AddComponent<RawImage>();
                new_obj.transform.SetParent(canvas.transform);
                RectTransform rect = new_obj.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(config.x.Value, config.y.Value);
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(config.delta_x.Value, config.delta_y.Value);
            }
            new_image.texture = texture;
            rawImageTransform = new_image.GetComponent<RectTransform>();
            return new_image;
        }
        public int ZoomCamera(int amount = 1) {
            if (destroyed) { return -1; }
            float size = camera.orthographicSize;
            if (size >= 150f && amount > 0) {
                size = 150f;
                return (int)size;
            }
            if (size <= 10f && amount < 0) {
                size = 10f;
                return (int)size;
            }
            camera.orthographicSize += amount;
            return (int)camera.orthographicSize;
        }

        public void Destroy() {
            if (destroyed) {
                return;
            }
            Debug("MiniMap: Destroying Camera and Canvas for player.");
            GameObject.Destroy(GameObject.Find("MINI_MAP_CAMERA"));
            GameObject.Destroy(GameObject.Find("MINI_MAP_CANVAS"));
            destroyed = true;
        }
        public GameObject FindChild(Transform parent, string child_name, int iteration = 0) {
            iteration++;
            if (iteration >= 9999)
            {
                UnityEngine.Debug.LogError("Failed to find child after n 9999");
                return null;
            }
            foreach (Transform child in parent) {
                if (child.name.ToUpper() == child_name.ToUpper()) {
                    UnityEngine.Debug.LogWarning("Found Child: " + child.name);
                    return child.gameObject;
                } else if (child.childCount > 0) {
                    return FindChild(child, child_name, iteration++);
                }
            }
            return null;
        }
        public List<String> DumpChild(Transform parent, List<String> list = default(List<string>)) {
            if (list == default(List<string>)) {
                list = new List<string>();
            }
            foreach (Transform child in parent) {
                list.Add(parent.name + "/" + child.name);
                UnityEngine.Debug.LogWarning(parent.name + "/" + child.name);
                if (child.childCount > 0) {
                    list = DumpChild(child, list);
                }
            }
            return list;
        }
        public List<MonoBehaviour> DumpMonoBehaviours(GameObject gameObject, bool printResults = true) {
            List<MonoBehaviour> ms = new List<MonoBehaviour>();
            foreach (MonoBehaviour m in gameObject.GetComponents<MonoBehaviour>()) {
                if (printResults) {
                    Debug("Mono(" + gameObject.name + "): " + m.name + " Type: " + m.GetType().ToString());
                }
                ms.Add(m);
            }
            return ms;
        }
    }
    [System.Serializable]
    public class TrackedObject {
        public GameObject gameObject {get;set;}
        public GameObject miniMapMarker {get;set;}
        public bool ignore = false;
        public TrackedObject(GameObject gameObject, GameObject miniMapMarker=null,bool ignore = false) {
            this.miniMapMarker = miniMapMarker;
            this.gameObject = gameObject;
            this.ignore = ignore;
        }
    }
    public class Timer {
        public float currentValue = 0f;
        public float maxValue = 1f;
        public float minValue = 0f;
        public Action callback = null;
        public Timer(float minValue = 0.0f, float maxValue = 1f, Action callback=null) {
            //default is 1 second timer that sends debug message when triggered
            this.callback = callback;
            this.maxValue = maxValue;
            this.minValue = minValue;
        }
        public float Update(float timeIncrement=1f) {
            //time increment is usually Time.DeltaTime
            //default is 1s
            currentValue += timeIncrement;
            if (currentValue >= maxValue)
            {
                if (callback != null)
                {
                    callback();
                }
                else {
                    UnityEngine.Debug.Log("MiniMap Timer: (" + minValue + "," + maxValue + ") completed.");
                }
                currentValue = 0f;
                return maxValue;
            }
            if (currentValue <= minValue) {
                currentValue = minValue;
            }
            return currentValue;
        }
        public bool Get(float timeIncrement = 0f) {
            if (timeIncrement > 0f) {
                if (Update(timeIncrement) >= maxValue)
                {
                    return true;
                }
                else {
                    return false;
                }
            }
            if (currentValue >= maxValue) {
                return true;
            }
            return false;
        }
    }
    public class MiniMapConfig {
        public bool _LOADED = false;
        public ConfigFile file = null;
        public ConfigEntry<bool> EnableMod;
        public ConfigEntry<KeyCode> enableEditing;
        public ConfigEntry<KeyCode> moveUp;
        public ConfigEntry<KeyCode> moveDown;
        public ConfigEntry<KeyCode> moveLeft;
        public ConfigEntry<KeyCode> moveRight;
        public ConfigEntry<KeyCode> increaseSize;
        public ConfigEntry<KeyCode> decreaseSize;
        public ConfigEntry<KeyCode> zoomIn;
        public ConfigEntry<KeyCode> zoomOut;
        public ConfigEntry<Single> x;
        public ConfigEntry<Single> y;
        public ConfigEntry<Single> delta_x;
        public ConfigEntry<Single> delta_y;
        public ConfigEntry<Single> zoom_level;
        public MiniMapConfig() {

        }
        ConfigFile RetrieveConfigFile(bool setGlobal=true) {
            try
            {
                ConfigFile new_file = null;
                if (file == null)
                {
                    Debug.Log("MiniMapMod: Attempting to locate/create Configuration file for MiniMapMod");
                    ConfigFile conf = new ConfigFile("BepInEx//config//MiniMapMod.cfg", true);
                    new_file = conf;
                    if (new_file == null)
                    {
                        Debug.LogWarning("MiniMapMod failed to load config!");
                        return null;
                    }
                }
                else
                {
                    Debug.Log("Found configuration file for MiniMapMod.");
                    new_file = file;
                }
                if (setGlobal)
                {
                    file = new_file;
                }
                return new_file;
            }
            catch (Exception e) {
                throw new Exception("MiniMapMod: Error (0x0) FATAL EXCEPTION IN CREATING/REFERENCING CONFIG FILE, verify MiniMapMod.conf and parent folder's permissions and existance. " + e.Message);
            }
        }
        void BindValues() {
            try
            {
                this.EnableMod = file.Bind<bool>(new ConfigDefinition("Enable Mod", "EnableMod"), true, new ConfigDescription("Enables/Disables MiniMapMod"));
                this.enableEditing = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Enable_Moving_Minimap_Key"), KeyCode.M, new ConfigDescription("Key, when pressed, toggles locking the minimaps position and size in place until pressed again."));
                this.moveUp = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Move_Up"), KeyCode.UpArrow, new ConfigDescription("Move MiniMap: Up"));
                this.moveDown = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Move_Down"), KeyCode.DownArrow, new ConfigDescription("Move MiniMap: Down"));
                this.moveLeft = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Move_Left"), KeyCode.LeftArrow, new ConfigDescription("Move MiniMap: Left"));
                this.moveRight = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Move_Right"), KeyCode.RightArrow, new ConfigDescription("Move MiniMap: Right"));
                this.increaseSize = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Increase_Size"), KeyCode.PageUp, new ConfigDescription("Increases MiniMaps Size"));
                this.decreaseSize = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Decrease_Size"), KeyCode.PageDown, new ConfigDescription("Decreases MiniMaps Size"));
                this.zoomIn = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Zoom_In"), KeyCode.Plus, new ConfigDescription("Zooms minimap In"));
                this.zoomOut = file.Bind<KeyCode>(new ConfigDefinition("Controls", "Zoom_Out"), KeyCode.Minus, new ConfigDescription("Zooms MiniMap Out"));
                this.x = file.Bind<Single>(new ConfigDefinition("Position and Size", "Horizontal_Starting_Position"), 0, new ConfigDescription("Starting Position of MiniMap"));
                this.y = file.Bind<Single>(new ConfigDefinition("Position and Size", "Vertical_Starting_Position"), 0, new ConfigDescription("Starting Position of MiniMap"));
                this.delta_x = file.Bind<Single>(new ConfigDefinition("Position and Size", "Minimap_Width"), 100, new ConfigDescription("Width of MiniMap"));
                this.delta_y = file.Bind<Single>(new ConfigDefinition("Position and Size", "Minimap_Height"), 100, new ConfigDescription("Width of MiniMap"));
                this.zoom_level = file.Bind<Single>(new ConfigDefinition("Position and Size", "Minimap_Zoom_Level"), 85, new ConfigDescription("How far the minimap is zoomed in"));
            }
            catch (Exception e) {
                throw new Exception("MiniMapMod: Error(0x1) FATAL EXCEPTION IN BINDING CONFIGURATION FILE, verify permissions and write access to MiniMapMod.conf: " + e.Message);
            }
        }
        public bool Load() {
            this.file = RetrieveConfigFile();
            if (this.file != null)
            {
                BindValues();
                this._LOADED = true;
                return true;
            }
            else {
                Debug.LogError("MiniMapMod: Failed to load config from disk.");
                return false;
            }
        }
        public void Reload() {
            try
            {
                file.Reload();
            }
            catch (Exception e) {
                throw new Exception("MiniMapMod: Error(0x2) FATAL EXCEPTION IN RELOADING CONFIG." ,e);
            }
        }
        public void Save() {
            try
            {
                file.Save();
            }
            catch (Exception e)
            {
                throw new Exception("MiniMapMod: Error(0x3) FATAL EXCEPTION IN SAVING CONFIG.", e);
            }
        }
    }
    // PLUGIN
    [BepInPlugin("MiniMap", "Mini Map Mod", "1.0.0")]
    public class MiniMap : BaseUnityPlugin
    {
        // GLOBAL VARIABLES
        bool _ENABLE_DEBUG = false;
        bool _IS_MINIMAP_LOADED = false;
        Player _PLAYER = null;
        bool _ENABLE_EDITING = true;
        Timer _MINIMAP_POLLRATE;
        MiniMapConfig _CONFIG = new MiniMapConfig();
        bool _ENABLED = true;
        // GLOBAL LISTS
        List<Player> _PLAYERS = new List<Player>();
        List<GameObject> _PERSISTENT_OBJECTS = new List<GameObject>();
        List<TrackedObject> _SCENE_OBJECTS = new List<TrackedObject>();
        private void Debug(string str, string priority = "info")
        {
            if (_ENABLE_DEBUG || priority == "error")
            {
                str = "MiniMap: " + str;
                switch (priority)
                {
                    case "info":
                        Logger.Log(BepInEx.Logging.LogLevel.Info,str);
                        return;
                    case "error":
                        Logger.Log(BepInEx.Logging.LogLevel.Error, str);
                        return;
                    default:
                        //debug
                        Logger.Log(BepInEx.Logging.LogLevel.All, str);
                        return;
                }
            }

        }
        
        // CUSTOM FUNCTIONS
        void LoadPlayers() {
            // Fills the List<Players> list with current game's players.
            foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController.instances)
            {
                _PLAYERS.Add(new Player(controller,_CONFIG));
            }
        }
        void SpawnMiniMapMarkers(List<TrackedObject> tracked_objects = null,int layer=16) {
            if (tracked_objects == null) {
                return;
            }
            // Searches scene for primary, secondary and tiertiary game objects and spawns a marker above each.
            foreach (TrackedObject tracked_object in tracked_objects)
            {
                //verify we aren't drawing objects that shouldn't be drawn to minimap
                if (tracked_object.ignore) continue;
                if (tracked_object.gameObject == null) continue;

                //check to see if chest/interactable has already been opened
                if (tracked_object.gameObject.GetComponent<PurchaseInteraction>())
                {
                    if (tracked_object.gameObject.GetComponent<PurchaseInteraction>().available == false)
                    {
                        Debug("Chest purchased, destroying marker." + tracked_object.gameObject.name);
                        DestroyImmediate(tracked_object.miniMapMarker);
                        tracked_object.ignore = true;
                        continue;
                    }
                }
                //determine if we have already spawned an object, since we don't destroy them on refresh any more.
                if (tracked_object.miniMapMarker != null) continue;

                //spawn new marker
                GameObject new_marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                new_marker.name = tracked_object.gameObject.GetInstanceID().ToString();

                //set the marker on the object
                tracked_object.miniMapMarker = new_marker;

                //change color
                Renderer m = new_marker.GetComponent<Renderer>();
                m.material.SetColor("_Color", Color.red);
                new_marker.transform.position = tracked_object.gameObject.transform.position + new Vector3(0, 10, 0);

                //set to camera layer
                new_marker.layer = layer;

                //set size to be more visible on map
                new_marker.transform.localScale = new Vector3(5, 5, 5);
            }
            foreach (Player player in _PLAYERS)
            {
                string p_name;
                try
                {
                    p_name = player.controller.GetDisplayName();
                }
                catch (Exception e) {
                    Debug("Failed to reference player array.");
                    ResetMod();
                    return;
                }
                bool exists = false;
                foreach (GameObject g in _PERSISTENT_OBJECTS) {
                    if (g.name == "MiniMapPlayerMarker_" + p_name) {
                        exists = true;
                        continue;
                    }
                }
                if (exists) continue;
                Debug("Marking Player: " + p_name);
                GameObject new_primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                new_primitive.name = "MiniMapPlayerMarker_"+p_name;
                _PERSISTENT_OBJECTS.Add(new_primitive);

                Renderer primitive_renderer = new_primitive.GetComponent<Renderer>();
                primitive_renderer.material.SetColor("_Color", Color.white);

                new_primitive.transform.position = player.gameObject.transform.position + new Vector3(0, 10, 0);
                new_primitive.transform.SetParent(player.gameObject.transform);
                new_primitive.transform.localScale = new Vector3(5, 5, 5);

                new_primitive.layer = layer;
            }
            Debug("Refreshed MiniMap");
        }
        void RefreshMiniMapMarkers() {
            SpawnMiniMapMarkers(_SCENE_OBJECTS);
        }
        List<TrackedObject> ScanSceneObjects() {
            //the goal of this is to alleviate lag from scanning the scene of all game objects
            List<TrackedObject> new_tracked_object = new List<TrackedObject>();
            //scan entire scene for objects
            string PrimaryObjects = "ShrineCombat(Clone) ShrineBlood(Clone) ShrineChance(Clone) ShrineBoss(Clone) Teleporter1(Clone) TripleShop(Clone) GoldshoresBeacon(Clone) NullSafeZone (1)";
            string SecondaryObjects = "LunarChest(Clone) Chest1(Clone) Duplicator(Clone) EquipmentBarrel(Clone) Chest2(Clone) Chest3(Clone) GoldChest(Clone) CategoryChestDamage(Clone) CategoryChestHealing(Clone) CategoryChestUtility(Clone) Chest1 - 1 Chest1 - 2 Chest1 - 3 Chest1 - 4 Chest1 - 5";
            string TertiaryObjects = "NewtStatue NewtStatue (1) NewtStatue (2) NewtStatue (3) Barrel Barrel1(Clone) Drone1Broken(Clone) MissleDroneBroken(Clone) EquipmentDroneBroken(Clone)";
            string MasterObjects = PrimaryObjects + SecondaryObjects + TertiaryObjects;
            //MasterObjects = MasterObjects;
            foreach (GameObject g in GameObject.FindObjectsOfType<GameObject>())
            {
                if (MasterObjects.Contains(g.name))
                {
                    new_tracked_object.Add(new TrackedObject(g));
                }
            }
            return new_tracked_object;
        }
        Player GetPlayer() {
            // Finds the player in the current lobby and sets it for futher reference.
            if (_PLAYER != null) {
                return _PLAYER;
            }
            if (_PLAYERS.Count == 0 || _PLAYERS == null) {
                return null;
            }
            foreach (Player player in _PLAYERS) {
                if (player.controller.hasAuthority && player.controller.master.alive) {
                    //Debug("Found player: " + player.controller.GetDisplayName());
                    return player;
                }
            }
            return null;
        }
        void FollowRotation(GameObject source_object, GameObject target_object, Vector3 limits=default(Vector3)) {
            //Copies the rotation of source_object to target_object, if limits is present, it sets the values to given limits, 0=no limit 0.0001f = lock rotation to 0
            if (limits == default(Vector3)) {
                limits = new Vector3(0,0,0);
            }
            //get source 
            Vector3 source_uler = source_object.transform.rotation.eulerAngles;
            Vector3 new_rotation = new Vector3(source_uler.x, source_uler.y, source_uler.z);
            if (limits.x != 0) {
                new_rotation.x = limits.x;
            }
            if (limits.y != 0)
            {
                new_rotation.y = limits.y;
            }
            if (limits.z != 0)
            {
                new_rotation.z = limits.z;
            }
            target_object.transform.rotation = Quaternion.Euler(new_rotation);
        }
        bool IsGameLoaded() {
            // Checks to determine if the game is still running and all players aren't dead.
            int dead = 0;
            foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController.instances) {
                if (controller.master.alive)
                {
                    if (_PLAYERS.Count == 0)
                    {
                        LoadPlayers();
                    }
                    return true;
                }
                else {
                    dead++;
                }
            }
            int count = PlayerCharacterMasterController.instances.Count;
            if (dead >= count && count > 0) {
                if (GetPlayer() != null) {
                    GetPlayer().Destroy();
                }
                _PLAYERS = new List<Player>();
                _PLAYER = null;
                //Debug("All players dead, resetting MiniMap players.");
                _IS_MINIMAP_LOADED = false;
            }
            return false;
        }
        void ResetMod() {
            Debug("Resetting Mod.");
            try {
                _PLAYER.Destroy();
            }
            catch (NullReferenceException) {
                //some mods don't gracefully transition scenes and causes a nullreferenceexception, ignore it
            }
            _PLAYER = null;
            _PLAYERS.Clear();
            _PERSISTENT_OBJECTS.Clear();
            _SCENE_OBJECTS.Clear();
            _IS_MINIMAP_LOADED = false;
            _IS_MINIMAP_LOADED = false;
        }
        // UNITY FUNCTIONS
        private void Start()
        {
            Debug("Sucessfully Loaded MiniMap Mod", "info");
            _MINIMAP_POLLRATE = new Timer(0, 3, RefreshMiniMapMarkers);
            Debug("Attempting to load configuration.");
            _CONFIG.Load();
            _ENABLED = _CONFIG.EnableMod.Value;
        }
        private void Update()
        {
            //this is for testing purposes only remove before prod
            if (!_ENABLED)
            {
                return;
            }
            bool is_loaded = false;
            try
            {
                is_loaded = IsGameLoaded();
                if (!is_loaded && _PLAYER != null) {
                    ResetMod();
                }
            }
            catch (NullReferenceException)
            {
                Debug("NullReferenceException, MiniMapMod IsGameLoaded, did you switch characters without dying? Your membership to the Salty Spitoon has been revoked, please see the clerk at the Weenie Hut Junior's membership office for your complementary membership. Resetting Mod.");
                ResetMod();
            }
            if (is_loaded && !_IS_MINIMAP_LOADED) {
                bool sucessfully_initialized = GetPlayer().__init__();
                if (!sucessfully_initialized) {
                    ResetMod();
                    return;
                }
                _SCENE_OBJECTS = ScanSceneObjects();
                SpawnMiniMapMarkers(_SCENE_OBJECTS);
                _IS_MINIMAP_LOADED = true;
            }
            if (is_loaded && _IS_MINIMAP_LOADED) {
                FollowRotation(Camera.allCameras[0].gameObject,GetPlayer().camera.gameObject,new Vector3(90,0,0.0001f));
                _MINIMAP_POLLRATE.Update(Time.deltaTime);
            }
            if (!is_loaded) return;
            // ZOOMING 
            if (Input.GetKey(_CONFIG.zoomIn.Value))
            {
                GetPlayer().ZoomCamera(-1);
                _CONFIG.zoom_level.Value += -1;
            }
            if (Input.GetKey(_CONFIG.zoomOut.Value))
            {
                GetPlayer().ZoomCamera(1);
                _CONFIG.zoom_level.Value += 1;
            }
            if (Input.GetKeyDown(_CONFIG.enableEditing.Value)) {
                _ENABLE_EDITING = !_ENABLE_EDITING;
            }
            if (_ENABLE_EDITING) {
                // SIZE
                if (Input.GetKey(_CONFIG.increaseSize.Value)) {
                    GetPlayer().rawImageTransform.sizeDelta += new Vector2(1,1);
                    _CONFIG.delta_x.Value += 1;
                    _CONFIG.delta_y.Value += 1;
                }
                if (Input.GetKey(_CONFIG.decreaseSize.Value))
                {
                    GetPlayer().rawImageTransform.sizeDelta += new Vector2(-1,-1);
                    _CONFIG.delta_x.Value += -1;
                    _CONFIG.delta_y.Value += -1;
                }
                // MOVEMENT
                if (Input.GetKey(_CONFIG.moveUp.Value))
                {
                    GetPlayer().rawImageTransform.anchoredPosition += new Vector2(0,1);
                    _CONFIG.y.Value += 1;
                }
                if (Input.GetKey(_CONFIG.moveDown.Value))
                {
                    GetPlayer().rawImageTransform.anchoredPosition += new Vector2(0,-1);
                    _CONFIG.y.Value += -1;
                }
                if (Input.GetKey(_CONFIG.moveLeft.Value))
                {
                    GetPlayer().rawImageTransform.anchoredPosition += new Vector2(-1,0);
                    _CONFIG.x.Value += -1;
                }
                if (Input.GetKey(_CONFIG.moveRight.Value))
                {
                    GetPlayer().rawImageTransform.anchoredPosition += new Vector2(1,0);
                    _CONFIG.x.Value += 1;
                }
            }
        }
    }
}