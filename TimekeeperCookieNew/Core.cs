using HarmonyLib;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework;
using Il2CppVampireSurvivors.Framework.DLC;
using Il2CppVampireSurvivors.Objects;
using Il2CppVampireSurvivors.Objects.Characters;
using MelonLoader;
using System.IO.Pipes;
using UnityEngine;
using static Il2CppMono.Security.X509.X509Stores;
using static Il2CppSystem.ComponentModel.MaskedTextProvider;
using static Il2CppTMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static MelonLoader.MelonLogger;
//using static UnityEngine.TextEditor;

[assembly: MelonInfo(typeof(TimekeeperCookieNew.Core), "Cookie Survivors", "1.0.0", "sup3p", null)]
[assembly: MelonGame("poncle", "Vampire Survivors")]

namespace TimekeeperCookieNew
{
    public class Core : MelonMod
    {
        public static MelonLogger.Instance melonLog;
        public static Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<Sprite>> animationSprites = new();
        private static Vector2 defaultPivot = new(0.5f, 0);
        private static readonly CharacterType startingCharType = (CharacterType)8701;
        private static CharacterType charType = startingCharType;
        private static Il2CppVampireSurvivors.Objects.Characters.CharacterController characterController;
        //public static Dictionary<string, Texture2D> characterTextures = new();
        private readonly static Dictionary<string, CharacterType> characterTypes = new();

        public override void OnInitializeMelon()
        {
            melonLog = LoggerInstance;
            LoggerInstance.Msg("Initialized.");
        }

        private static JArray CreateBaseCharacterData(string textureFileName, string textureName, int width, int height, int frames, string charName, string desc, string weapon)
        {
            string spriteName = charName.ToLower();

            Texture2D charTexture = ResourceHelper.CreateCharacterTexture(textureFileName, textureName);
            ResourceHelper.CreateCharacterSpritesFromStrip(charTexture, 0, 0, width, height, defaultPivot, spriteName, frames);

            JObject charData = new();
            charData["level"] = 1;
            charData["startingWeapon"] = weapon;
            charData["charName"] = charName;
            charData["textureName"] = textureName;
            charData["spriteName"] = $"{spriteName}_01";
            charData["walkingFrames"] = frames;
            charData["description"] = desc;
            charData["maxHp"] = 100;
            charData["cooldown"] = 1;
            charData["armor"] = 0;
            charData["regen"] = 0;
            charData["moveSpeed"] = 1;
            charData["power"] = 1;
            charData["area"] = 1;
            charData["speed"] = 1;
            charData["duration"] = 1;
            charData["amount"] = 0;
            charData["luck"] = 1;
            charData["growth"] = 1;
            charData["greed"] = 1;
            charData["curse"] = 1;
            charData["magnet"] = 0;
            charData["revivals"] = 0;
            charData["rerolls"] = 0;
            charData["skips"] = 0;
            charData["banish"] = 0;

            JObject defaultSkin = new();
            defaultSkin["skinType"] = "DEFAULT";
            defaultSkin["name"] = "Default";
            defaultSkin["textureName"] = textureName;
            defaultSkin["spriteName"] = $"{spriteName}_01";
            defaultSkin["walkingFrames"] = frames;
            defaultSkin["unlocked"] = true;
            JArray skinsArray = new();
            skinsArray.Add(defaultSkin);
            charData["skins"] = skinsArray;

            JArray fullCharArray = new();
            fullCharArray.Add(charData);
            fullCharArray.Add(new JObject());
            fullCharArray[1]["growth"] = 1;
            fullCharArray[1]["level"] = 20;
            fullCharArray.Add(new JObject());
            fullCharArray[2]["growth"] = 1;
            fullCharArray[2]["level"] = 40;
            fullCharArray.Add(new JObject());
            fullCharArray[3]["growth"] = -1;
            fullCharArray[3]["level"] = 21;
            fullCharArray.Add(new JObject());
            fullCharArray[4]["growth"] = -1;
            fullCharArray[4]["level"] = 41;

            return fullCharArray;
        }

        private static void AddCharacter(ref DataManager dataManager, JArray characterArray)
        {
            dataManager._allCharactersJson[charType.ToString()] = characterArray;
            string charName = characterArray[0]["charName"].ToString();
            characterTypes.Add(charName, charType);
            melonLog.Msg($"Added character {charName}");
            
            charType++;
        }

        [HarmonyPatch(typeof(DataManager))]
        class DataManager_Patch
        {
            [HarmonyPatch(nameof(DataManager.LoadBaseJObjects))]
            [HarmonyPostfix]
            static void LoadBaseJObjects_Postfix(DataManager __instance)
            {
                JArray workingArray;
                
                workingArray = CreateBaseCharacterData("timekeeperTexture.png", "character_timekeeper", 39, 36, 5, "Timekeeper", "Starts with an extra Arcana XII - Out of Time. Gains +1% Duration every level.", "LANCET");
                workingArray[0]["surname"] = "Cookie";
                workingArray[0]["maxHp"] = 80;
                workingArray[0]["speed"] = 1.1;
                workingArray[0]["duration"] = 1.1;
                workingArray[0]["rerolls"] = 10;
                workingArray[0]["onEveryLevelUp"] = new JObject();
                workingArray[0]["onEveryLevelUp"]["duration"] = 0.01;
                //workingArray[0]["power"] = 1.1;
                //workingArray[0]["banish"] = 5;
                //workingArray[0]["revivals"] = 1;
                AddCharacter(ref __instance, workingArray);
            }
        }

        [HarmonyPatch(typeof(Il2CppVampireSurvivors.Objects.Characters.CharacterController))]
        class CharacterController_Patch
        {
            [HarmonyPatch(nameof(Il2CppVampireSurvivors.Objects.Characters.CharacterController.InitCharacter))]
            [HarmonyPostfix]
            static void InitCharacter_Patch(Il2CppVampireSurvivors.Objects.Characters.CharacterController __instance, CharacterType characterType)
            {
                characterController = __instance;
                if (characterType >= startingCharType && characterType <= charType && __instance.CurrentCharacterData.GetCurrentSkinData().skinType == SkinType.DEFAULT)
                {
                    foreach (Sprite animSprite in animationSprites[__instance.CurrentCharacterData.charName.ToLower()])
                    {
                        __instance.Anims._animations["walk"]._frames.Add(animSprite);
                    }
                    __instance.SetupAnimation();
                }
            }

            [HarmonyPatch(nameof(Il2CppVampireSurvivors.Objects.Characters.CharacterController.AfterFullInitialization))]
            [HarmonyPostfix]
            static void AfterFullInitialization_Patch(Il2CppVampireSurvivors.Objects.Characters.CharacterController __instance)
            {
                if (__instance.CharacterType == characterTypes["Timekeeper"])
                {
                    var _gameManager = __instance._gameManager;
                    _gameManager.ArcanaManager.ActiveArcanas.Add(ArcanaType.T12_OUT_OF_TIME);
                    _gameManager.ArcanaManager.TriggerArcana(ArcanaType.T12_OUT_OF_TIME);
                }
            }
        }

        /*
        [HarmonyPatch(nameof(GameManager.InitializeGameSessionPostLoad))]
        [HarmonyPostfix]
        static void InitializeGameSessionPostLoad_Patch(GameManager __instance)
        {
            //_GameManager = __instance;
            Il2CppVampireSurvivors.Objects.Characters.CharacterController characterController = __instance.PlayerOne;
            CharacterType characterType = characterController.CharacterType;
            melonLog.Msg($"{characterType}");
            melonLog.Msg($"{characterTypes["Timekeeper"]}");

            if (characterType == characterTypes["Timekeeper"])
            {
                __instance!.ArcanaManager.ActiveArcanas.Add(ArcanaType.T12_OUT_OF_TIME);
                __instance.ArcanaManager.TriggerArcana(ArcanaType.T12_OUT_OF_TIME);
            }
        }
        */
    }
}