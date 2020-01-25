﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BGC.Mathematics;
using BGC.Users;
using BGC.UI.Dialogs;
using BGC.UI.Panels;

namespace BGC.Settings
{
    public abstract class BaseSettingsMenu : ModePanel
    {
        [Header("Settings Menu UI")]
        [SerializeField]
        private Text valueLabel = null;
        [SerializeField]
        private InputField valueField = null;
        [SerializeField]
        private Dropdown valueDropdown = null;
        [SerializeField]
        private Button actionCancel = null;
        [SerializeField]
        private Button actionApply = null;
        [SerializeField]
        private Button applyChangesButton = null;
        [SerializeField]
        private Button cancelChangesButton = null;
        [SerializeField]
        private GameObject settingsPanel = null;
        [SerializeField]
        private GameObject settingsWidgetArea = null;

        [Header("Prefabs")]
        [SerializeField]
        private GameObject settingContainerTemplate = null;
        [SerializeField]
        private GameObject settingWidgetTemplate = null;
        [SerializeField]
        private GameObject colorWidgetTemplate = null;

        protected enum UIState
        {
            SettingsMenu = 0,
            EnterValue,
            SelectValue
        };

        protected enum SettingType
        {
            Integer = 0,
            Float,
            String,
            Boolean,
            Color
        };

        protected enum SettingProtection
        {
            Open = 0,
            Admin,
            AlwaysLocked
        };

        protected enum SettingScope
        {
            User = 0,
            Global
        };

        private static readonly List<SettingBase> settings = new List<SettingBase>();
        private static readonly Dictionary<string, SettingBase> nameSettingsMap = new Dictionary<string, SettingBase>();

        private static readonly Dictionary<string, string> maskedMaskerNameMap = new Dictionary<string, string>();
        private static readonly Dictionary<string, Func<SettingBase, bool>> maskedEvaluatorMap = new Dictionary<string, Func<SettingBase, bool>>();

        private static readonly List<SettingsSet> containers = new List<SettingsSet>();
        private static readonly Dictionary<string, SettingsSet> containerWidgetMap = new Dictionary<string, SettingsSet>();

        SettingBase currentlyEditingValue = null;

        private const string enterValuePrompt = "Enter Value for ";

        private string currentlyEditingValueTitle = "";

        private UIState currentState = UIState.SettingsMenu;

        private bool settingDirty;
        private bool allSettingsDirty = false;

        void Awake()
        {
            applyChangesButton.onClick.AddListener(ApplyChanges);
            cancelChangesButton.onClick.AddListener(TryCancelChanges);
            actionCancel.onClick.AddListener(CancelAction);
            actionApply.onClick.AddListener(SubmitValue);

            ClearFlags();

            OnAwake();

            ConstructButtons();
        }


        #region ModePanel

        public sealed override void FocusAcquired()
        {
            allSettingsDirty = true;
            ShowUIState(UIState.SettingsMenu);

            OnFocusAcquired();
        }


        public sealed override void FocusLost()
        {
            OnFocusLost();
        }


        #endregion ModePanel
        #region Child Customization Methods

        protected virtual void OnAwake() { }
        protected virtual void OnFocusAcquired() { }
        protected virtual void OnFocusLost() { }

        #endregion Child Customization Methods

        protected static bool ShowOnMaskerTrue(SettingBase maskerSetting)
        {
            BoolSetting boolSetting = maskerSetting as BoolSetting;

            return boolSetting.GetCurrentValue();
        }

        public static bool GetSettingBool(string key)
        {
            if (!nameSettingsMap.ContainsKey(key))
            {
                Debug.LogError($"Requested a setting that doesn't exist: {key}");
                return false;
            }

            SettingBase setting = nameSettingsMap[key];

            if (setting.SettingType != SettingType.Boolean)
            {
                Debug.LogError($"Used the wrong SettingType for setting.\tExpected: {SettingType.Boolean}\tReceived: {setting.SettingType}");
                return false;
            }

            return ((BoolSetting)setting).GetInnerValue();
        }

        public static int GetSettingInt(string key)
        {
            if (!nameSettingsMap.ContainsKey(key))
            {
                Debug.LogError($"Requested a setting that doesn't exist: {key}");
                return 0;
            }

            SettingBase setting = nameSettingsMap[key];

            if (setting.SettingType != SettingType.Integer)
            {
                Debug.LogError($"Used the wrong SettingType for setting.\tExpected: {SettingType.Integer}\tReceived: {setting.SettingType}");
                return 0;
            }

            return ((IntSetting)setting).GetInnerValue();
        }

        public static float GetSettingFloat(string key)
        {
            if (!nameSettingsMap.ContainsKey(key))
            {
                Debug.LogError($"Requested a setting that doesn't exist: {key}");
                return 0.0f;
            }

            SettingBase setting = nameSettingsMap[key];

            if (setting.SettingType != SettingType.Float)
            {
                Debug.LogError($"Used the wrong SettingType for setting.\tExpected: {SettingType.Float}\tReceived: {setting.SettingType}");
                return 0.0f;
            }

            return ((FloatSetting)setting).GetInnerValue();
        }

        public static string GetSettingString(string key)
        {
            if (!nameSettingsMap.ContainsKey(key))
            {
                Debug.LogError($"Requested a setting that doesn't exist: {key}");
                return "";
            }

            SettingBase setting = nameSettingsMap[key];

            if (setting.SettingType != SettingType.String)
            {
                Debug.LogError($"Used the wrong SettingType for setting.\tExpected: {SettingType.String}\tReceived: {setting.SettingType}");
                return "";
            }

            return ((StrSetting)setting).GetInnerValue();
        }

        private void ConstructButtons()
        {
            foreach (SettingBase setting in settings)
            {
                CreateAndLinkButton(setting);
            }
        }

        //On End Edit funcion for New setting values
        public void SubmitValue()
        {
            bool success = false;

            switch (currentState)
            {
                case UIState.EnterValue:
                    {
                        string newValue = valueField.text;

                        //Abort if the name is bad
                        if (string.IsNullOrEmpty(newValue))
                        {
                            Debug.Log("Tried to submit an empty value");
                            CancelAction();
                            return;
                        }

                        //Use newValue
                        if (currentlyEditingValue.TryValue(ref newValue))
                        {
                            success = true;
                        }
                        else
                        {
                            //Update codefield text
                            valueField.text = newValue;

                            //Highlight codefield
                            EventSystem.current.SetSelectedGameObject(valueField.gameObject, null);
                            valueField.OnPointerClick(new PointerEventData(EventSystem.current));
                        }
                    }
                    break;

                case UIState.SelectValue:
                    currentlyEditingValue.SetValueFromDropdown(valueDropdown.value);
                    success = true;
                    break;

                case UIState.SettingsMenu:
                default:
                    Debug.LogError($"Unexpected UIState: {currentState}");
                    return;
            }

            if (success)
            {
                currentlyEditingValueTitle = "";
                currentlyEditingValue = null;
                ShowUIState(UIState.SettingsMenu);
            }
        }

        public void CancelAction()
        {
            ShowUIState(UIState.SettingsMenu);
            currentlyEditingValue = null;
            currentlyEditingValueTitle = "";
        }

        private void EditValue(SettingBase editSetting)
        {
            UIState requestedState = editSetting.EditButtonPressed();

            switch (requestedState)
            {
                case UIState.SettingsMenu:
                    //Do nothing special
                    break;

                case UIState.EnterValue:
                    currentlyEditingValue = editSetting;
                    currentlyEditingValueTitle = editSetting.label;

                    valueField.text = editSetting.GetValue();

                    //Set Focus
                    EventSystem.current.SetSelectedGameObject(valueField.gameObject, null);
                    valueField.OnPointerClick(new PointerEventData(EventSystem.current));
                    break;

                case UIState.SelectValue:
                    currentlyEditingValue = editSetting;

                    valueDropdown.ClearOptions();
                    valueDropdown.AddOptions(editSetting.GetValueList());
                    valueDropdown.value = ((IntSetting)editSetting).GetCurrentValue();
                    valueDropdown.RefreshShownValue();
                    break;

                default:
                    break;
            }

            ShowUIState(requestedState);

            if (requestedState == UIState.EnterValue)
            {
                //Set Focus
                EventSystem.current.SetSelectedGameObject(valueField.gameObject, null);
                valueField.OnPointerClick(new PointerEventData(EventSystem.current));
            }
        }

        protected void RefreshView() => ShowUIState(currentState);

        private void ShowUIState(UIState state)
        {
            currentState = state;

            valueLabel.text = $"{enterValuePrompt}{currentlyEditingValueTitle}:";
            bool settingsVisible = (state == UIState.SettingsMenu);

            valueField.gameObject.SetActive(state == UIState.EnterValue);
            valueDropdown.gameObject.SetActive(state == UIState.SelectValue);
            valueLabel.gameObject.SetActive(!settingsVisible);
            actionCancel.gameObject.SetActive(!settingsVisible);
            actionApply.gameObject.SetActive(!settingsVisible);

            applyChangesButton.gameObject.SetActive(settingsVisible);
            cancelChangesButton.gameObject.SetActive(settingsVisible);

            settingsPanel.SetActive(settingsVisible);

            if (settingsVisible)
            {
                settingDirty = false;

                foreach (SettingBase setting in settings)
                {
                    setting.SettingWidget.SetActive(GetSettingActive(setting.name));

                    //Set the interactable setting based on current lock state
                    setting.SettingModifyButton.interactable = setting.GetModifiable(PlayerData.IsLocked);
                    if (setting.NameNeedsUpdate() || allSettingsDirty)
                    {
                        setting.ApplyValuesToButton();
                    }

                    settingDirty |= setting.GetModified();
                }

                foreach (SettingsSet container in containers)
                {
                    container.gameObject.SetActive(GetSettingActive(container.settingSetName));
                }

                allSettingsDirty = false;

                applyChangesButton.interactable = settingDirty;

                cancelChangesButton.GetComponentInChildren<Text>().text = settingDirty ? "Cancel" : "Menu";
                cancelChangesButton.interactable = !PlayerData.IsDefault;
            }
        }

        private bool GetSettingActive(string settingName)
        {
            if (maskedMaskerNameMap.ContainsKey(settingName))
            {
                string maskerName = maskedMaskerNameMap[settingName];

                if (!nameSettingsMap.ContainsKey(maskerName))
                {
                    Debug.LogError($"SettingName not found: {maskerName}");
                    return true;
                }

                SettingBase masker = nameSettingsMap[maskerName];
                bool tierActive = maskedEvaluatorMap[settingName](masker);

                return tierActive && GetSettingActive(maskerName);
            }

            return true;
        }

        protected virtual void PostApplyChanges() { }

        protected void ApplyChanges()
        {
            foreach (SettingBase setting in settings)
            {
                if (setting.GetModified())
                {
                    setting.ApplyValue();
                }
            }

            PlayerData.Save();

            PostApplyChanges();

            //Reload the screen
            ShowUIState(UIState.SettingsMenu);
        }

        public void TryCancelChanges()
        {
            if (settingDirty)
            {
                ModalDialog.ShowSimpleModal(ModalDialog.Mode.ConfirmCancel,
                    "Discard Changes?",
                    "Are you sure you want to discard your changes and return to the Menu?",
                    (ModalDialog.Response response) =>
                    {
                        switch (response)
                        {
                            case ModalDialog.Response.Confirm:
                                CancelChanges();
                                break;

                            case ModalDialog.Response.Cancel:
                                //Do Nothing
                                break;

                            default:
                                Debug.LogError($"Unexpected ModalDialog.Response: {response}");
                                break;
                        }
                    });
            }
            else
            {
                CancelChanges();
            }
        }

        protected abstract void ExitMenu();

        public void CancelChanges()
        {
            ClearFlags();
            ExitMenu();
        }

        public static void PullPushableSettings()
        {
            foreach (SettingBase setting in settings)
            {
                if (setting.scope == SettingScope.User && setting.pushOnCopy)
                {
                    setting.PullDefaultProfileValue();
                }
            }
        }

        private static void AddToMaskerMap(
            string maskerName,
            string settingName,
            Func<SettingBase, bool> maskingEvaluator)
        {
            if (string.IsNullOrEmpty(maskerName) || string.IsNullOrEmpty(settingName))
            {
                Debug.LogError($"Tried to add blank name to MaskerMap.  Masker: {maskerName}.  Setting: {settingName}");
                return;
            }

            maskedMaskerNameMap.Add(settingName, maskerName);
            maskedEvaluatorMap.Add(settingName, maskingEvaluator);
        }

        protected static void PushBoolSetting(
            SettingScope scope,
            SettingProtection protectionLevel,
            string label,
            string name,
            bool defaultVal,
            string maskerName = "",
            Func<SettingBase, bool> maskingEvaluator = null,
            bool pushOnCopy = true)
        {
            SettingBase newSetting = new BoolSetting(
                scope,
                protectionLevel,
                label,
                name,
                defaultVal,
                pushOnCopy);

            settings.Add(newSetting);
            nameSettingsMap.Add(name, newSetting);

            if (!string.IsNullOrEmpty(maskerName))
            {
                AddToMaskerMap(maskerName, name, maskingEvaluator);
            }
        }

        protected static void PushIntSetting(
            SettingScope scope,
            SettingProtection protectionLevel,
            string label,
            string name,
            int defaultVal,
            int minVal = int.MinValue,
            int maxVal = int.MaxValue,
            Func<int, string> translator = null,
            string postFix = "",
            string maskerName = "",
            Func<SettingBase, bool> maskingEvaluator = null,
            bool pushOnCopy = true,
            bool dropdown = false)
        {
            SettingBase newSetting = new IntSetting(
                scope,
                protectionLevel,
                label,
                name,
                defaultVal,
                minVal: minVal,
                maxVal: maxVal,
                translator: translator,
                dropdown: dropdown,
                postFix: postFix,
                pushOnCopy: pushOnCopy);

            settings.Add(newSetting);
            nameSettingsMap.Add(name, newSetting);

            if (!string.IsNullOrEmpty(maskerName))
            {
                AddToMaskerMap(maskerName, name, maskingEvaluator);
            }
        }

        protected static void PushSettingContainer(
            string name,
            string maskerName = "",
            Func<SettingBase, bool> maskingEvaluator = null)
        {
            //Containers handled lazily
            if (!string.IsNullOrEmpty(maskerName))
            {
                AddToMaskerMap(maskerName, name, maskingEvaluator);
            }
        }

        protected static void PushStringSetting(
            SettingScope scope,
            SettingProtection protectionLevel,
            string label,
            string name,
            string defaultVal,
            string maskerName = "",
            Func<SettingBase, bool> maskingEvaluator = null,
            bool pushOnCopy = true)
        {
            SettingBase newSetting = new StrSetting(
                scope,
                protectionLevel,
                label,
                name,
                defaultVal,
                pushOnCopy);

            settings.Add(newSetting);
            nameSettingsMap.Add(name, newSetting);

            if (!string.IsNullOrEmpty(maskerName))
            {
                AddToMaskerMap(maskerName, name, maskingEvaluator);
            }
        }

        protected static void PushFloatSetting(
            SettingScope scope,
            SettingProtection protectionLevel,
            string label,
            string name,
            float defaultVal,
            float minVal = float.MinValue,
            float maxVal = float.MaxValue,
            Func<float, string> translator = null,
            string postFix = "",
            string maskerName = "",
            Func<SettingBase, bool> maskingEvaluator = null,
            bool pushOnCopy = true)
        {
            SettingBase newSetting = new FloatSetting(
                scope,
                protectionLevel,
                label,
                name,
                defaultVal,
                minVal: minVal,
                maxVal: maxVal,
                translator: translator,
                postFix: postFix,
                pushOnCopy: pushOnCopy);

            settings.Add(newSetting);
            nameSettingsMap.Add(name, newSetting);

            if (!string.IsNullOrEmpty(maskerName))
            {
                AddToMaskerMap(maskerName, name, maskingEvaluator);
            }
        }

        private void CreateAndLinkButton(SettingBase setting)
        {
            Transform parent;

            parent = settingsWidgetArea.transform;

            setting.CreateSettingWidget(parent, this);

            setting.SettingModifyButton.onClick.AddListener(() => { EditValue(setting); });

            setting.ApplyValuesToButton();
        }

        private Transform GetSettingContainer(string containerName)
        {
            if (!containerWidgetMap.ContainsKey(containerName))
            {
                GameObject newContainer = Instantiate(settingContainerTemplate, settingsWidgetArea.transform, false);

                SettingsSet tempSet = newContainer.GetComponent<SettingsSet>();

                containerWidgetMap.Add(containerName, tempSet);
                tempSet.settingSetName = containerName;
                containers.Add(tempSet);
            }

            return containerWidgetMap[containerName].setGroup;
        }

        /// <summary>
        /// Clear all modified flags
        /// </summary>
        private void ClearFlags()
        {
            foreach (SettingBase setting in settings)
            {
                setting.ClearFlags();
            }
        }

        protected abstract class SettingBase
        {
            public readonly SettingScope scope;
            public readonly SettingProtection protectionLevel;
            public readonly string label;
            public readonly string name;
            public readonly bool pushOnCopy;

            /// <summary>
            /// Stores submitted value until it is applied or discarded
            /// </summary>
            protected string tmpNewValue;

            /// <summary>
            /// Flag that marks the button name in need of update
            /// </summary>
            protected bool nameDirty;

            /// <summary>
            /// Container to hold the button associated with this setting
            /// </summary>
            public GameObject SettingWidget { get; protected set; }
            public Text SettingLabelText { get; protected set; }
            public Button SettingModifyButton { get; protected set; }
            public Text SettingButtonLabel { get; protected set; }

            public abstract SettingType SettingType { get; }

            protected SettingBase(
                SettingScope scope,
                SettingProtection protectionLevel,
                string label,
                string name,
                bool pushOnCopy)
            {
                this.scope = scope;
                this.protectionLevel = protectionLevel;
                this.label = label;
                this.name = name;
                this.pushOnCopy = pushOnCopy;

                tmpNewValue = "";
                nameDirty = false;
            }

            /// <summary>
            /// Return the full string label for the button
            /// </summary>
            public string GetFullLabel() => $"{label}: {GetValueLabel()}";

            /// <summary>
            /// Return the full string label for the button
            /// </summary>
            public abstract string GetValueLabel();

            /// <summary>
            /// Execute button press and inform the editor what state should be engaged
            /// </summary>
            public abstract UIState EditButtonPressed();

            /// <summary>
            /// Return the current value as a string
            /// </summary>
            public abstract string GetValue();

            /// <summary>
            /// Test parsability of <paramref name="newValue"/>, assign if successful.  Return success.
            /// </summary>
            public abstract bool TryValue(ref string newValue);

            /// <summary>
            /// Apply any value stored in tmpNewValue to the actual setting
            /// </summary>
            public abstract void ApplyValue();

            /// <summary>
            /// Copy the relevant value from the Default profile
            /// </summary>
            public abstract void PullDefaultProfileValue();

            public virtual List<string> GetValueList() => null;
            public virtual void SetValueFromDropdown(int index) { }

            //Clears flag as a side-effect
            public bool NameNeedsUpdate()
            {
                if (nameDirty)
                {
                    nameDirty = false;
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Clear all flags that might mark setting as dirty
            /// </summary>
            public void ClearFlags()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    nameDirty = true;
                    tmpNewValue = "";
                }
            }

            /// <summary>
            /// Get whether this setting is holding an unsaved modification
            /// </summary>
            public bool GetModified() => !string.IsNullOrEmpty(tmpNewValue);

            public void ApplyValuesToButton()
            {
                SettingLabelText.text = label;
                SettingButtonLabel.text = GetValueLabel();

                SupplementaryValueUpdate();
            }

            protected virtual void SupplementaryValueUpdate() { }

            public virtual void CreateSettingWidget(Transform parent, BaseSettingsMenu menu)
            {
                SettingWidget = Instantiate(menu.settingWidgetTemplate, parent, false);

                SettingLabelText = SettingWidget.GetComponentInChildren<Text>();
                SettingModifyButton = SettingWidget.GetComponentInChildren<Button>();
                SettingButtonLabel = SettingModifyButton.GetComponentInChildren<Text>();
            }

            public virtual bool GetModifiable(bool locked)
            {
                switch (protectionLevel)
                {
                    case SettingProtection.Open: return true;
                    case SettingProtection.Admin: return !locked;
                    case SettingProtection.AlwaysLocked: return false;

                    default:
                        Debug.LogError($"Unexpected protectionLevel: {protectionLevel}");
                        return false;
                }
            }
        };


        protected class IntSetting : SettingBase
        {
            public int defaultVal;

            public int minVal;
            public int maxVal;

            private readonly string postFix;
            private readonly Func<int, string> translator;

            private readonly bool dropdown;

            public override SettingType SettingType => SettingType.Integer;

            public IntSetting(
                SettingScope scope,
                SettingProtection protectionLevel,
                string label,
                string name,
                int defaultVal = 0,
                int minVal = int.MinValue,
                int maxVal = int.MaxValue,
                Func<int, string> translator = null,
                string postFix = "",
                bool dropdown = false,
                bool pushOnCopy = true)
                : base(
                    scope: scope,
                    protectionLevel: protectionLevel,
                    label: label,
                    name: name,
                    pushOnCopy: pushOnCopy)
            {
                this.defaultVal = defaultVal;

                this.minVal = minVal;
                this.maxVal = maxVal;

                this.translator = translator;
                this.postFix = postFix;

                this.dropdown = dropdown;
            }

            public override string GetValueLabel()
            {
                int val = GetInnerValue();

                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    val = int.Parse(tmpNewValue);
                }

                return GetValueLabel(val);
            }

            private string GetValueLabel(int val)
            {
                if (translator != null)
                {
                    return $"({val}) {translator(val)}{postFix}";
                }

                return $"{val.ToString()}{postFix}";
            }

            public override List<string> GetValueList()
            {
                if (maxVal - minVal > 100)
                {
                    throw new Exception("Are you sure you want to display more than 100?  Temporarily blocked.");
                }

                List<string> valueList = new List<string>();

                for (int i = minVal; i <= maxVal; i++)
                {
                    valueList.Add(GetValueLabel(i));
                }

                return valueList;
            }

            public override void SetValueFromDropdown(int index)
            {
                int result = minVal + index;

                if (result == GetInnerValue())
                {
                    tmpNewValue = "";
                }
                else
                {
                    tmpNewValue = result.ToString();
                }

                nameDirty = true;
            }

            public override UIState EditButtonPressed() => dropdown ? UIState.SelectValue : UIState.EnterValue;

            public override string GetValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    return tmpNewValue;
                }

                return GetInnerValue().ToString();
            }

            public int GetCurrentValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    return int.Parse(tmpNewValue);
                }

                return GetInnerValue();
            }

            public int GetInnerValue()
            {
                int returnVal = defaultVal;
                switch (scope)
                {
                    case SettingScope.Global:
                        returnVal = PlayerPrefs.GetInt(name, defaultVal);
                        break;

                    case SettingScope.User:
                        returnVal = PlayerData.GetInt(name, defaultVal);
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }

                return GeneralMath.Clamp(returnVal, minVal, maxVal);
            }

            private void SetInnerValue(int newValue)
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        PlayerPrefs.SetInt(name, newValue);
                        break;

                    case SettingScope.User:
                        PlayerData.SetInt(name, newValue);
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override void PullDefaultProfileValue()
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        Debug.LogError("Tried to pull a global setting");
                        return;

                    case SettingScope.User:
                        PlayerData.SetInt(name, PlayerData.DefaultData.GetInt(name, defaultVal));
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override bool TryValue(ref string newValue)
            {
                //Test parsability
                if (int.TryParse(newValue, out int result))
                {
                    //Test bounds
                    if (result >= minVal && result <= maxVal)
                    {
                        tmpNewValue = newValue;
                        if (result == GetInnerValue())
                        {
                            tmpNewValue = "";
                        }

                        nameDirty = true;
                        return true;
                    }
                    else
                    {
                        //Failed bounds check
                        result = GeneralMath.Clamp(result, minVal, maxVal);
                        //Update value string
                        newValue = result.ToString();
                        return false;
                    }
                }

                //Failed to parse
                return false;
            }

            public override void ApplyValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    if (int.TryParse(tmpNewValue, out int result))
                    {
                        SetInnerValue(result);
                        tmpNewValue = "";
                    }
                    else
                    {
                        Debug.LogError("Failed to parse tmpNewValue");
                    }
                }
            }
        };

        protected class FloatSetting : SettingBase
        {
            public float defaultVal;

            public float minVal;
            public float maxVal;

            private readonly string postFix;
            private readonly Func<float, string> translator;

            public override SettingType SettingType => SettingType.Float;

            public FloatSetting(
                SettingScope scope,
                SettingProtection protectionLevel,
                string label,
                string name,
                float defaultVal = 0,
                float minVal = float.MinValue,
                float maxVal = float.MaxValue,
                Func<float, string> translator = null,
                string postFix = "",
                bool pushOnCopy = true)
                : base(
                    scope: scope,
                    protectionLevel: protectionLevel,
                    label: label,
                    name: name,
                    pushOnCopy: pushOnCopy)
            {
                this.defaultVal = defaultVal;

                this.minVal = minVal;
                this.maxVal = maxVal;

                this.postFix = postFix;
                this.translator = translator;
            }

            public override string GetValueLabel()
            {
                float tmpValue = GetInnerValue();

                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    tmpValue = float.Parse(tmpNewValue);
                }

                if (translator == null)
                {
                    return $"{tmpValue.ToString()}{postFix}";
                }

                return $"{translator(tmpValue)}{postFix}";
            }

            public override UIState EditButtonPressed() => UIState.EnterValue;

            public override string GetValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    return tmpNewValue;
                }

                return GetInnerValue().ToString();
            }

            public float GetInnerValue()
            {
                float returnValue = defaultVal;
                switch (scope)
                {
                    case SettingScope.Global:
                        returnValue = PlayerPrefs.GetFloat(name, defaultVal);
                        break;

                    case SettingScope.User:
                        returnValue = PlayerData.GetFloat(name, defaultVal);
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }

                return GeneralMath.Clamp(returnValue, minVal, maxVal);
            }

            private void SetInnerValue(float newValue)
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        PlayerPrefs.SetFloat(name, newValue);
                        break;

                    case SettingScope.User:
                        PlayerData.SetFloat(name, newValue);
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override void PullDefaultProfileValue()
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        Debug.LogError("Tried to pull a global setting");
                        return;

                    case SettingScope.User:
                        PlayerData.SetFloat(name, PlayerData.DefaultData.GetFloat(name, defaultVal));
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override bool TryValue(ref string newValue)
            {
                //Check for parsability
                if (float.TryParse(newValue, out float result))
                {
                    //Check Bounds
                    if (result >= minVal && result <= maxVal)
                    {
                        tmpNewValue = newValue;
                        if (result == GetInnerValue())
                        {
                            tmpNewValue = "";
                        }

                        nameDirty = true;
                        return true;
                    }
                    else
                    {
                        //Failed bounds check
                        result = GeneralMath.Clamp(result, minVal, maxVal);
                        //Update value string
                        newValue = result.ToString();
                        return false;
                    }
                }

                //failed to parse
                return false;
            }

            public override void ApplyValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    if (float.TryParse(tmpNewValue, out float result))
                    {
                        SetInnerValue(result);
                        tmpNewValue = "";
                    }
                    else
                    {
                        Debug.LogError("Failed to parse tmpNewValue");
                    }
                }
            }
        };

        protected class StrSetting : SettingBase
        {
            public override SettingType SettingType => SettingType.String;

            public StrSetting(
                SettingScope scope,
                SettingProtection protectionLevel,
                string label,
                string name,
                string defaultVal = "",
                bool pushOnCopy = true)
                : base(
                    scope: scope,
                    protectionLevel: protectionLevel,
                    label: label,
                    name: name,
                    pushOnCopy: pushOnCopy)
            {
                this.defaultVal = defaultVal;
            }

            public string defaultVal;

            public override string GetValueLabel()
            {
                string val = GetInnerValue();

                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    val = tmpNewValue;
                }

                return val;
            }

            public override UIState EditButtonPressed()
            {
                return UIState.EnterValue;
            }

            public override string GetValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    return tmpNewValue;
                }
                else
                {
                    return GetInnerValue();
                }
            }

            public string GetInnerValue()
            {
                switch (scope)
                {
                    case SettingScope.Global: return PlayerPrefs.GetString(name, defaultVal);
                    case SettingScope.User: return PlayerData.GetString(name, defaultVal);

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        return defaultVal;
                }
            }

            private void SetInnerValue(string newValue)
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        PlayerPrefs.SetString(name, newValue);
                        break;

                    case SettingScope.User:
                        PlayerData.SetString(name, newValue);
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override void PullDefaultProfileValue()
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        Debug.LogError("Tried to pull a global setting");
                        return;

                    case SettingScope.User:
                        PlayerData.SetString(name, PlayerData.DefaultData.GetString(name, defaultVal));
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override bool TryValue(ref string newValue)
            {
                //Any sanitization checks here
                tmpNewValue = newValue;
                if (newValue == GetInnerValue())
                {
                    tmpNewValue = "";
                }

                nameDirty = true;
                return true;
            }

            public override void ApplyValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    SetInnerValue(tmpNewValue);
                    tmpNewValue = "";
                }
            }
        };

        protected abstract class ColorSetting : SettingBase
        {
            public override SettingType SettingType => SettingType.Color;
            public abstract ColorSource Source { get; }

            protected ColorWidget colorWidget;

            public enum ColorSource
            {
                Theme = 0,
                Settings
            }

            protected ColorSetting(
                SettingScope scope,
                SettingProtection protectionLevel,
                string label,
                string name,
                bool pushOnCopy = true)
                : base(
                    scope: scope,
                    protectionLevel: protectionLevel,
                    label: label,
                    name: name,
                    pushOnCopy: pushOnCopy)
            {
            }

            public override string GetValueLabel()
            {
                return GetValue();
            }

            public override UIState EditButtonPressed() => UIState.EnterValue;

            public override string GetValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    return tmpNewValue;
                }
                else
                {
                    return $"#{ColorUtility.ToHtmlStringRGBA(GetInnerValue())}";
                }
            }

            public abstract Color GetInnerValue();

            protected abstract void SetInnerValue(string newValue);

            public override bool TryValue(ref string newValue)
            {
                if (!newValue.StartsWith("#"))
                {
                    newValue.Insert(0, "#");
                }

                if (!ColorUtility.TryParseHtmlString(newValue, out Color tmpColor))
                {
                    newValue = $"#{ColorUtility.ToHtmlStringRGBA(GetInnerValue())}";

                    return false;
                }

                tmpNewValue = newValue;

                if (tmpColor == GetInnerValue())
                {
                    tmpNewValue = "";
                }

                SupplementaryValueUpdate();
                nameDirty = true;
                return true;
            }

            public override void ApplyValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    SetInnerValue(tmpNewValue);
                    tmpNewValue = "";
                }
            }

            public override void CreateSettingWidget(Transform parent, BaseSettingsMenu menu)
            {
                SettingWidget = Instantiate(menu.colorWidgetTemplate, parent, false);

                colorWidget = SettingWidget.GetComponent<ColorWidget>();

                SettingLabelText = colorWidget.label;
                SettingModifyButton = colorWidget.settingButton;
                SettingButtonLabel = SettingModifyButton.GetComponentInChildren<Text>();
            }

            protected Color GetCurrentColor()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    if (ColorUtility.TryParseHtmlString(tmpNewValue, out Color tmpColor))
                    {
                        return tmpColor;
                    }
                }

                return GetInnerValue();
            }

            protected override void SupplementaryValueUpdate()
            {
                colorWidget.SetColor(GetCurrentColor());
            }
        };

        protected class SettingColorSetting : ColorSetting
        {
            public override ColorSource Source => ColorSource.Settings;

            private Color defaultVal;

            public SettingColorSetting(
                SettingScope scope,
                SettingProtection protectionLevel,
                string label,
                string name,
                Color defaultVal,
                bool pushOnCopy = true)
                : base(
                    scope: scope,
                    protectionLevel: protectionLevel,
                    label: label,
                    name: name,
                    pushOnCopy: pushOnCopy)
            {
                this.defaultVal = defaultVal;
            }

            public override Color GetInnerValue()
            {
                string colorString;

                switch (scope)
                {
                    case SettingScope.Global:
                        colorString = PlayerPrefs.GetString(name, "");
                        break;

                    case SettingScope.User:
                        colorString = PlayerData.GetString(name, "");
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        return defaultVal;
                }

                if (string.IsNullOrEmpty(colorString))
                {
                    return defaultVal;
                }

                if (ColorUtility.TryParseHtmlString(colorString, out Color parsedColor))
                {
                    return parsedColor;
                }

                return defaultVal;
            }

            protected override void SetInnerValue(string newValue)
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        PlayerPrefs.SetString(name, newValue);
                        break;

                    case SettingScope.User:
                        PlayerData.SetString(name, newValue);
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override void PullDefaultProfileValue()
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        Debug.LogError("Tried to pull a global setting");
                        return;

                    case SettingScope.User:
                        PlayerData.SetString(name,
                            PlayerData.DefaultData.GetString(
                                name, $"#{ColorUtility.ToHtmlStringRGBA(defaultVal)}"));
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }
        }

        private class BoolSetting : SettingBase
        {
            public override SettingType SettingType => SettingType.Boolean;

            public BoolSetting(
                SettingScope scope,
                SettingProtection protectionLevel,
                string label,
                string name,
                bool defaultVal = false,
                bool pushOnCopy = true)
                : base(
                    scope: scope,
                    protectionLevel: protectionLevel,
                    label: label,
                    name: name,
                    pushOnCopy: pushOnCopy)
            {
                this.defaultVal = defaultVal;
            }

            public bool defaultVal;

            public override string GetValueLabel()
            {
                bool val = GetInnerValue();

                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    val = bool.Parse(tmpNewValue);
                }

                return (val ? "True" : "False");
            }

            public override UIState EditButtonPressed()
            {
                //We do not need to open the editor for booleans
                if (string.IsNullOrEmpty(tmpNewValue))
                {
                    tmpNewValue = (!GetInnerValue()).ToString();
                }
                else
                {
                    //If tmpNewValue isn't empty, then it was just changed back to old value
                    tmpNewValue = "";
                }

                nameDirty = true;

                return UIState.SettingsMenu;
            }

            public override string GetValue() => GetValueLabel();

            public bool GetCurrentValue()
            {
                if (string.IsNullOrEmpty(tmpNewValue))
                {
                    return GetInnerValue();
                }

                return bool.Parse(tmpNewValue);
            }

            public bool GetInnerValue()
            {
                switch (scope)
                {
                    case SettingScope.Global: return ((PlayerPrefs.GetInt(name, defaultVal ? 1 : 0)) != 0);
                    case SettingScope.User: return PlayerData.GetBool(name, defaultVal);

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        return defaultVal;
                }
            }

            private void SetInnerValue(bool newValue)
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        PlayerPrefs.SetInt(name, (newValue ? 1 : 0));
                        break;

                    case SettingScope.User:
                        PlayerData.SetBool(name, newValue);
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override void PullDefaultProfileValue()
            {
                switch (scope)
                {
                    case SettingScope.Global:
                        Debug.LogError("Tried to pull a global setting");
                        return;

                    case SettingScope.User:
                        PlayerData.SetBool(name, PlayerData.DefaultData.GetBool(name, defaultVal));
                        break;

                    default:
                        Debug.LogError($"Unexpected SettingScope: {scope}");
                        break;
                }
            }

            public override bool TryValue(ref string newValue)
            {
                if (bool.TryParse(newValue, out bool result))
                {
                    tmpNewValue = newValue;
                    if (result == GetInnerValue())
                    {
                        tmpNewValue = "";
                    }

                    nameDirty = true;
                    return true;
                }

                return false;
            }

            public override void ApplyValue()
            {
                if (!string.IsNullOrEmpty(tmpNewValue))
                {
                    if (bool.TryParse(tmpNewValue, out bool result))
                    {
                        SetInnerValue(result);
                        tmpNewValue = "";
                    }
                    else
                    {
                        Debug.LogError("Failed to parse tmpNewValue");
                    }
                }
            }
        };
    }
}