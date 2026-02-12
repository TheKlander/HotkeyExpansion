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
    [BepInPlugin("com.theklander.HotkeyExpansion", "Hotkey Expansion", "1.1.0")]
    [BepInDependency("com.aidanamite.ConfigTweaks", BepInDependency.DependencyFlags.HardDependency)]
    public class Main : BaseUnityPlugin
    {
        // Configuration fields
        [ConfigField("Viking Menu Key", "Hotkeys", Description = "Keybind to open and close the Viking menu")]
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

        // Cached reflection info for various hotkeys
        private static FieldInfo mUiActionsMenuField;
        private static FieldInfo mBtnCloseField;
        private static MethodInfo openStoreForBuyTaskMethod;
        private static MethodInfo onActivateMethod;
        private static FieldInfo mIsFeedCSMOpenedField;
        private static FieldInfo mBtnDragField;
        private static FieldInfo mGlideEndTimeField;
        private static FieldInfo mIsRemoveTutorialDoneField;

        // Cache for frequently accessed objects (cleared when they become null)
        private UiToolbar cachedUiToolbar;
        private UiActions cachedUiActions;
        private UiAvatarCSM cachedUiAvatarCSM;
        private UiFishing cachedUiFishing;
        private AvAvatarController cachedAvAvatarController;

        public void Awake()
        {
            instance = this;
            new Harmony("com.theklander.HotkeyExpansion").PatchAll();

            // Cache ALL reflection info once at startup
            CacheReflectionInfo();


        }

        private void CacheReflectionInfo()
        {

                // Chat detection
                uiChatHistoryType = AccessTools.TypeByName("UiChatHistory");
                if (uiChatHistoryType != null)
                {
                    isVisibleField = AccessTools.Field(uiChatHistoryType, "_IsVisible");
                    mInstanceField = AccessTools.Field(uiChatHistoryType, "mInstance");
                    mEditChatField = AccessTools.Field(uiChatHistoryType, "mEditChat");
                }

                // Emotes
                mUiActionsMenuField = AccessTools.Field(typeof(UiActions), "mUiActionsMenu");

                // Store
                mBtnCloseField = AccessTools.Field(typeof(KAUIStore), "mBtnClose");
                openStoreForBuyTaskMethod = AccessTools.Method(typeof(UiToolbar), "OpenStoreForBuyTask");

                // Dragon menu
                onActivateMethod = AccessTools.Method(typeof(ObContextSensitive), "OnActivate");
                mIsFeedCSMOpenedField = AccessTools.Field(typeof(PetCSM), "mIsFeedCSMOpened");

                // Fishing
                mBtnDragField = AccessTools.Field(typeof(UiFishing), "mBtnDrag");

                // Flightsuit
                mGlideEndTimeField = AccessTools.Field(typeof(AvAvatarController), "mGlideEndTime");
                mIsRemoveTutorialDoneField = AccessTools.Field(typeof(AvAvatarController), "mIsRemoveTutorialDone");


        }

        // Helper method to get or cache UiToolbar
        private UiToolbar GetUiToolbar()
        {
            if (cachedUiToolbar == null || !cachedUiToolbar)
            {
                cachedUiToolbar = UnityEngine.Object.FindObjectOfType<UiToolbar>();
            }
            return cachedUiToolbar;
        }

        // Helper method to get or cache UiActions
        private UiActions GetUiActions()
        {
            if (cachedUiActions == null || !cachedUiActions)
            {
                cachedUiActions = UnityEngine.Object.FindObjectOfType<UiActions>();
            }
            return cachedUiActions;
        }

        // Helper method to get or cache UiAvatarCSM
        private UiAvatarCSM GetUiAvatarCSM()
        {
            if (cachedUiAvatarCSM == null || !cachedUiAvatarCSM)
            {
                cachedUiAvatarCSM = UnityEngine.Object.FindObjectOfType<UiAvatarCSM>();
            }
            return cachedUiAvatarCSM;
        }

        // Helper method to get or cache UiFishing
        private UiFishing GetUiFishing()
        {
            if (cachedUiFishing == null || !cachedUiFishing)
            {
                cachedUiFishing = UnityEngine.Object.FindObjectOfType<UiFishing>();
            }
            return cachedUiFishing;
        }

        // Helper method to get or cache AvAvatarController
        private AvAvatarController GetAvAvatarController()
        {
            if (cachedAvAvatarController == null || !cachedAvAvatarController)
            {
                cachedAvAvatarController = UnityEngine.Object.FindObjectOfType<AvAvatarController>();
            }
            return cachedAvAvatarController;
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

            // Dragon Menu Toggle Hotkey (only when not mounted)
            if (EnableDragonMenuHotkey && Input.GetKeyDown(OpenDragonMenuKey))
            {
                ToggleDragonMenu();
            }

            // Customization Hotkey
            if (EnableCustomizationHotkey && Input.GetKeyDown(OpenCustomizationKey))
            {
                ToggleCustomization();
            }

            // Remove Flightsuit Hotkey
            if (EnableRemoveFlightsuitHotkey && Input.GetKeyDown(OpenRemoveFlightsuitKey))
            {
                ToggleRemoveFlightsuit();
            }

            // Fishing Drag - ONLY check when key state changes
            if (EnableDragFishHotkey)
            {
                if (Input.GetKeyDown(OpenDragFishKey))
                {
                    SetDragFishState(true);
                }
                else if (Input.GetKeyUp(OpenDragFishKey))
                {
                    SetDragFishState(false);
                }
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

            return true;
        }

        private void ToggleCSM()
        {
            UiAvatarCSM uiAvatarCSM = GetUiAvatarCSM();

            if (uiAvatarCSM != null)
            {
                if (uiAvatarCSM.GetVisibility())
                {
                    uiAvatarCSM.Close(true);
                }
                else
                {
                    uiAvatarCSM.OpenCSM();
                }
            }
        }

        private void ToggleCustomization()
        {
            UiToolbar uiToolbar = GetUiToolbar();

            if (uiToolbar != null && uiToolbar.AllowHotKeys() && !FUEManager.pIsFUERunning && AllowedStatesCopy())
            {
                JournalLoader.Load("EquipBtn", "", false, null, true, UILoadOptions.AUTO, "");
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
            UiActions uiActions = GetUiActions();

            if (uiActions == null || mUiActionsMenuField == null)
            {
                return;
            }

            UiActionsMenu uiActionsMenu = (UiActionsMenu)mUiActionsMenuField.GetValue(uiActions);

            if (uiActions.GetVisibility() && uiActionsMenu != null && uiActionsMenu.GetVisibility())
            {
                uiActionsMenu.SetVisibility(false);
                uiActions.SetVisibility(false);
                KAUI.RemoveExclusive(uiActions);
            }
            else
            {
                uiActions.ShowEmoticons();
            }
        }

        private void ToggleStore()
        {
            UiToolbar uiToolbar = GetUiToolbar();
            KAUIStore storeInstance = KAUIStore.pInstance;

            if (storeInstance != null && storeInstance.GetVisibility() && mBtnCloseField != null)
            {
                KAWidget mBtnClose = (KAWidget)mBtnCloseField.GetValue(storeInstance);
                if (mBtnClose != null)
                {
                    storeInstance.OnClick(mBtnClose);
                }
            }

            if (uiToolbar != null && uiToolbar.AllowHotKeys() && !FUEManager.pIsFUERunning && AllowedStatesCopy())
            {
                if (!(storeInstance != null && storeInstance.GetVisibility()))
                {
                    if (openStoreForBuyTaskMethod != null)
                    {
                        openStoreForBuyTaskMethod.Invoke(uiToolbar, null);
                    }
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
                if (pet == null)
                {
                    return;
                }

                ObClickable clickable = pet.GetComponent<ObClickable>();
                if (clickable == null || clickable._ActivateObject == null)
                {
                    return;
                }

                ObContextSensitive contextSensitive = clickable._ActivateObject.GetComponent<ObContextSensitive>();
                if (contextSensitive == null)
                {
                    return;
                }

                PetCSM petCSM = contextSensitive as PetCSM;
                if (petCSM == null)
                {
                    petCSM = clickable._ActivateObject.GetComponent<PetCSM>();
                }

                if (petCSM == null || onActivateMethod == null || mIsFeedCSMOpenedField == null)
                {
                    return;
                }

                bool mIsFeedCSMOpened = (bool)mIsFeedCSMOpenedField.GetValue(petCSM);
                bool isMenuOpen = contextSensitive.pUI != null && contextSensitive.pUI.gameObject.activeInHierarchy;

                if (!isMenuOpen)
                {
                    // STATE 1: Menu closed -> Open the dragon menu
                    onActivateMethod.Invoke(contextSensitive, null);
                }
                else if (!mIsFeedCSMOpened)
                {
                    // STATE 2: Menu open, feed submenu closed -> Open feed submenu
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
                    contextSensitive.CloseMenu(false);
                }

        }

        // Optimized fishing drag - only called when key state changes
        private void SetDragFishState(bool pressed)
        {

                UiFishing uiFishing = GetUiFishing();

                if (uiFishing == null || mBtnDragField == null)
                {
                    return;
                }

                KAWidget mBtnDrag = (KAWidget)mBtnDragField.GetValue(uiFishing);
                if (mBtnDrag != null)
                {
                    uiFishing.OnPressRepeated(mBtnDrag, pressed);
                }

        }

        private void ToggleRemoveFlightsuit()
        {

                AvAvatarController avAvatarController = GetAvAvatarController();

                if (avAvatarController == null || mGlideEndTimeField == null || mIsRemoveTutorialDoneField == null)
                {
                    return;
                }

                bool mIsRemoveTutorialDone = (bool)mIsRemoveTutorialDoneField.GetValue(avAvatarController);

                avAvatarController.ProcessRemoveButton();
                mGlideEndTimeField.SetValue(avAvatarController, 0f);
                avAvatarController.EnableRemoveButton(false);

                if (!mIsRemoveTutorialDone)
                {
                    ProductData.AddTutorial(avAvatarController._RemoveTutorialName);
                    mIsRemoveTutorialDoneField.SetValue(avAvatarController, true);
                }

                avAvatarController.pAvatarCustomization.RestoreAvatar(true, false);
                avAvatarController.pAvatarCustomization.SaveCustomAvatar();
            
        }
    }
}