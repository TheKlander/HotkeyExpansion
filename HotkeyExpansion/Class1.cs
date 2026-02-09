using BepInEx;
using ConfigTweaks;
using HarmonyLib;
using JSGames.UI;
using KA.Framework.ThirdParty;
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace HotkeyExpansion
{
    [BepInPlugin("com.theklander.HotkeyExpansion", "Hotkey Expansion", "1.0.0")]
    [BepInDependency("com.aidanamite.ConfigTweaks", BepInDependency.DependencyFlags.HardDependency)]
    public class Main : BaseUnityPlugin
    {
        // Configuration fields
        [ConfigField("Viking Menu Key", "Hotkeys", Description = "Keybind to open adn close the Viking menu")]
        public static KeyCode OpenVikingMenuKey = KeyCode.X;

        [ConfigField("Enable Viking Menu Hotkey", "Hotkeys", Description = "Enable/disable the Viking menu hotkey")]
        public static bool EnableVikingMenuHotkey = true;


        [ConfigField("Emote Key", "Hotkeys", Description = "Keybind to open and close the emote menu")]
        public static KeyCode OpenEmoteKey = KeyCode.RightShift;

        [ConfigField("Enable Emote Hotkey", "Hotkeys", Description = "Enable/disable the emote hotkey")]
        public static bool EnableEmoteHotkey = true;


        [ConfigField("Store Key", "Hotkeys", Description = "Keybind to open the store")]
        public static KeyCode OpenStoreKey = KeyCode.RightBracket;

        [ConfigField("Enable Store Hotkey", "Hotkeys", Description = "Enable/disable the store hotkey")]
        public static bool EnableStoreHotkey = true;


        [ConfigField("Dragon Menu Key", "Hotkeys", Description = "Keybind to open and close the Dragon menu")]
        public static KeyCode OpenDragonMenuKey = KeyCode.F;

        [ConfigField("Enable Dragon Menu Hotkey", "Hotkeys", Description = "Enable/disable the Dragon menu hotkey")]
        public static bool EnableDragonMenuHotkey = true;


        [ConfigField("Drag Key", "Hotkeys", Description = "Keybind to drag reel during fishing")]
        public static KeyCode OpenDragFishKey = KeyCode.D;

        [ConfigField("Enable Drag Key", "Hotkeys", Description = "Enable/disable the fishing drag hotkey")]
        public static bool EnableDragFishHotkey = true;


        [ConfigField("Customization Key", "Hotkeys", Description = "Keybind to open the viking customization menu")]
        public static KeyCode OpenCustomizationKey = KeyCode.K;

        [ConfigField("Enable Customization Hotkey", "Hotkeys", Description = "Enable/disable the viking customization hotkey")]
        public static bool EnableCustomizationHotkey = true;

        [ConfigField("Remove Flightsuit Key", "Hotkeys", Description = "Keybind to Remove Flightsuit")]
        public static KeyCode OpenRemoveFlightsuitKey = KeyCode.E;

        [ConfigField("Enable Remove Flightsuit Hotkey", "Hotkeys", Description = "Enable/disable the Remove Flightsuit hotkey")]
        public static bool EnableRemoveFlightsuitHotkey = true;

        private static Main instance;

        // Cached reflection info for chat detection
        private static Type uiChatHistoryType;
        private static FieldInfo isVisibleField;
        private static FieldInfo mInstanceField;
        private static FieldInfo mEditChatField;
        private static MethodInfo hasFocusMethod;

        public void Awake()
        {
            instance = this;
            new Harmony("com.theklander.HotkeyExpansion").PatchAll();

            // Cache reflection info once at startup
            CacheChatReflectionInfo();

            // Apply Harmony patches
            //Harmony.CreateAndPatchAll(typeof(FishingReelStatePatch));
        }

        private void CacheChatReflectionInfo()
        {
            try
            {
                uiChatHistoryType = AccessTools.TypeByName("UiChatHistory");
                if (uiChatHistoryType != null)
                {
                    isVisibleField = AccessTools.Field(uiChatHistoryType, "_IsVisible");
                    mInstanceField = AccessTools.Field(uiChatHistoryType, "mInstance");
                    mEditChatField = AccessTools.Field(uiChatHistoryType, "mEditChat");

                    // We'll get the HasFocus method type later when we have an instance
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error caching chat reflection info: {ex.Message}");
            }
        }

        public void Update()
        {
            // Don't process hotkeys if chat is active
            if (IsChatActive())
            {
                return;
            }

            // CSM Menu Toggle Hotkey
            if (EnableVikingMenuHotkey && Input.GetKeyDown(OpenVikingMenuKey))
            {
                ToggleCSM();
            }

            // Emote Hotkey
            if (EnableEmoteHotkey && Input.GetKeyDown(OpenEmoteKey))
            {
                ToggleEmotes();
            }

            // Store Toggle Hotkey
            if (EnableStoreHotkey && Input.GetKeyDown(OpenStoreKey))
            {
                ToggleStore();
            }

            // Feed Menu Toggle Hotkey (only when not mounted)
            if (EnableDragonMenuHotkey && Input.GetKeyDown(OpenDragonMenuKey))
            {
                ToggleDragonMenu();
            }

            if (EnableCustomizationHotkey && Input.GetKeyDown(OpenCustomizationKey))
            {
                ToggleCustomization();
            }

            if (EnableRemoveFlightsuitHotkey && Input.GetKeyDown(OpenRemoveFlightsuitKey))
            {
                ToggleRemoveFlightsuit();
            }

            if (EnableDragFishHotkey)
            {
                ToggleDragFish();
            }
        }

        private bool IsChatActive()
        {
            
            // Quick exit if reflection info wasn't cached
            if (uiChatHistoryType == null || isVisibleField == null)
            {
                return false;
            }

            // Check if chat is visible (fast static field check)
            bool isVisible = (bool)isVisibleField.GetValue(null);
            if (!isVisible)
            {
                return false;
            }

            // Chat is visible, check if input has focus
            if (mInstanceField == null || mEditChatField == null)
            {
                // Can't check focus, assume chat is active to be safe
                return true;
            }

            var instance = mInstanceField.GetValue(null);
            if (instance == null)
            {
                return false;
            }

            var editChat = mEditChatField.GetValue(instance);
            if (editChat == null)
            {
                return false;
            }

            // Cache the HasFocus method if we haven't yet
            if (hasFocusMethod == null)
            {
                hasFocusMethod = AccessTools.Method(editChat.GetType(), "HasFocus");
            }

            if (hasFocusMethod != null)
            {
                bool hasFocus = (bool)hasFocusMethod.Invoke(editChat, null);
                return hasFocus;
            }

            // Couldn't get focus status, assume active to be safe
            return true;
            
        }

        private void ToggleCSM()
        {

            UiAvatarCSM uiAvatarCSM = UnityEngine.Object.FindObjectOfType<UiAvatarCSM>();

            if (uiAvatarCSM != null)
            {
                // Check if CSM is currently active/visible
                if (uiAvatarCSM.GetVisibility())
                {
                    // CSM is open, so close it
                    uiAvatarCSM.Close(true);

                }
                else
                {
                    // CSM is closed, so open it
                    uiAvatarCSM.OpenCSM();

                }
            }
        }

        private void ToggleCustomization()
        {
            UiAvatarCSM uiAvatarCSM = UnityEngine.Object.FindObjectOfType<UiAvatarCSM>();
            UiToolbar uiToolbar = UnityEngine.Object.FindObjectOfType<UiToolbar>();
            if (uiToolbar.AllowHotKeys() && !FUEManager.pIsFUERunning && AllowedStatesCopy())
            {

                JournalLoader.Load("EquipBtn", "", false, null, true, UILoadOptions.AUTO, "");
                return;

            }
        }

        private static bool AllowedStatesCopy()
        {
            return (!(AvAvatar.pObject != null) || !AvAvatar.pObject.GetComponent<AvAvatarController>().pPlayerCarrying)
                && (AvAvatar.pLevelState != AvAvatarLevelState.RACING
                    && AvAvatar.pLevelState != AvAvatarLevelState.TARGETPRACTICE
                    && AvAvatar.pSubState != AvAvatarSubState.GLIDING
                    && AvAvatar.pSubState != AvAvatarSubState.FLYING
                    && AvAvatar.pLevelState != AvAvatarLevelState.FLIGHTSCHOOL)
                && AvAvatar.pSubState != AvAvatarSubState.WALLCLIMB;
        }

        private void ToggleEmotes()
        {
            UiActions uiActions = UnityEngine.Object.FindObjectOfType<UiActions>();

            // Get the private mUiActionsMenu field
            var mUiActionsMenuField = AccessTools.Field(typeof(UiActions), "mUiActionsMenu");
            UiActionsMenu uiActionsMenu = (UiActionsMenu)mUiActionsMenuField.GetValue(uiActions);

            // Check if the emote menu is currently visible
            if (uiActions.GetVisibility() && uiActionsMenu.GetVisibility())
            {
                // Emotes are open, so close them
                uiActionsMenu.SetVisibility(false);
                uiActions.SetVisibility(false);
                KAUI.RemoveExclusive(uiActions);
            }
            else
            {
                // Emotes are closed, so open them
                uiActions.ShowEmoticons();

            }
        }

        private void ToggleStore()
        {

            // Check if store is accessible by looking for the store button
            UiToolbar uiToolbar = UnityEngine.Object.FindObjectOfType<UiToolbar>();
            KAUIStore storeInstance = KAUIStore.pInstance;

            if (storeInstance != null && storeInstance.GetVisibility())
            {
                var mBtnCloseField = AccessTools.Field(typeof(KAUIStore), "mBtnClose");
                KAWidget mBtnClose = (KAWidget)mBtnCloseField.GetValue(storeInstance);  
                storeInstance.OnClick(mBtnClose);
            }

            if (uiToolbar.AllowHotKeys() && !FUEManager.pIsFUERunning && AllowedStatesCopy())
            {
                if (!(storeInstance != null && storeInstance.GetVisibility()))
                {
                    var OpenStoreForBuyTaskField = AccessTools.Method(typeof(UiToolbar), "OpenStoreForBuyTask");
                    OpenStoreForBuyTaskField.Invoke(uiToolbar, null);
                }
            }

            
        }

        private void ToggleDragonMenu()
        {
            if (SanctuaryManager.pCurPetInstance != null && SanctuaryManager.pCurPetInstance.pIsMounted)
            {
                return;
            }
            SanctuaryPet pet = SanctuaryManager.pCurPetInstance;
            ObClickable clickable = pet.GetComponent<ObClickable>();
            ObContextSensitive contextSensitive = clickable._ActivateObject.GetComponent<ObContextSensitive>();
            PetCSM petCSM = UnityEngine.Object.FindObjectOfType<PetCSM>();

            //var onwidgetClickedMethod = AccessTools.Method(typeof(PetCSM), "OnWidgetClicked");

            var onActivateMethod = AccessTools.Method(typeof(ObContextSensitive), "OnActivate");
            //var onWidgetClicked = AccessTools.Method(typeof(PetCSM), "OnWidgetClicked");
            var mIsFeedCSMOpenedField = AccessTools.Field(typeof(PetCSM), "mIsFeedCSMOpened");
            bool mIsFeedCSMOpened = (bool)mIsFeedCSMOpenedField.GetValue(petCSM);

            bool isMenuOpen = contextSensitive.pUI != null && contextSensitive.pUI.gameObject.activeInHierarchy;

            if (!isMenuOpen)
            {
                // STATE 1: Menu closed -> Open the dragon menu
                Logger.LogInfo("Opening dragon menu");
                onActivateMethod.Invoke(contextSensitive, null);
            }
            else if (!mIsFeedCSMOpened)
            {
                // STATE 2: Menu open, feed submenu closed -> Open feed submenu
                Logger.LogInfo("Opening feed submenu");                

                // Find and click the Feed widget to open the submenu
                foreach (UiContextSensetiveMenu uiContextSensetiveMenu in contextSensitive.pUI.pUIContextSensitiveMenuList)
                {
                    KAWidget kawidget = uiContextSensetiveMenu.FindItem(petCSM._FeedInventoryCSItemName, true);
                    if (kawidget != null)
                    {
                        contextSensitive.pUI.OnClick(kawidget);
                        break;
                    }
                }
            }
            else
            {
                // STATE 3: Menu open, feed submenu open -> Close everything
                Logger.LogInfo("Closing dragon menu entirely");
                contextSensitive.CloseMenu(false);
            }
        }

        private void ToggleDragFish()
        {
            UiFishing uiFishing = UnityEngine.Object.FindObjectOfType<UiFishing>();
            var mBtnDragField = AccessTools.Field(typeof(UiFishing), "mBtnDrag");
            KAWidget mBtnDrag = (KAWidget)mBtnDragField.GetValue(uiFishing);

            if (Input.GetKey(OpenDragFishKey))
            {
                uiFishing.OnPressRepeated(mBtnDrag, true);
            }
            if (Input.GetKeyUp(OpenDragFishKey))
            {          
                uiFishing.OnPressRepeated(mBtnDrag, false);
            }


        }


        private void ToggleRemoveFlightsuit()
        {

            AvAvatarController avAvatarController = UnityEngine.Object.FindObjectOfType<AvAvatarController>();

            var mGlideEndTimeField = AccessTools.Field(typeof(AvAvatarController), "mGlideEndTime");
            float mGlideEndTime = (float)mGlideEndTimeField.GetValue(avAvatarController);

            var mIsRemoveTutorialDoneField = AccessTools.Field(typeof(AvAvatarController), "mIsRemoveTutorialDone");
            bool mIsRemoveTutorialDone = (bool)mIsRemoveTutorialDoneField.GetValue(avAvatarController);

            avAvatarController.ProcessRemoveButton();
            mGlideEndTime = 0f;
            avAvatarController.EnableRemoveButton(false);
            
            if (!mIsRemoveTutorialDone)
            {
                ProductData.AddTutorial(avAvatarController._RemoveTutorialName);
                mIsRemoveTutorialDone = true;
            }
            avAvatarController.pAvatarCustomization.RestoreAvatar(true, false);
            avAvatarController.pAvatarCustomization.SaveCustomAvatar();

        }

    }
}