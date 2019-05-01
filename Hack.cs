using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoR2;
using System;
using System.Text;
using UnityEngine.UI;

namespace ROR2_Pusky
{   
    public class Hack : MonoBehaviour
    {
        // [DllImport("Advapi32.dll")]
        // public static extern bool OpenThreadToken(IntPtr ThreadHandle, int DesiredAccess, bool OpenAsSelf, IntPtr TokenHandle);

        /* Members */

        private static LocalUser m_user;
        private static List<PurchaseInteraction> m_purchaseInteractionList;
        private static TeleporterInteraction m_portalInterraction;
        private static List<CharacterBody> m_enemyEntityList = new List<CharacterBody>();

        private bool m_isActive = true;
        private const string GUI_HEADER = "Made by Pusky";
        private static Rect GUI_POSITION = new Rect(new Vector2(20, 40), new Vector2(200, 20));//Rect(0, Screen.width/4, 200, 800);
        private const int GUI_WINDOW_ID = 0;

        private enum HackMenuOptions
        {
            UNKNOWN = -1,

            // DISABLE_ALL,                   // Disable all of the enabled hacks

            MENU_HEALTH,
                GOD_MODE,                   // Might not work sometime, maybe when having only shield.
                FULLY_HEAL,                 // This is a toggle.
                WEAKEN_ENEMIES,             // 1 HP toggle.

            MENU_ESP,
                ENEMY_ESP,                  // Enemy ESP overlay toggle.
                CHEST_ESP,                  // Chest ESP overlay toggle.
                PORTAL_ESP,                 // Portal ESP overlay toggle.

            MENU_FAST,
                FAST_ATTACK_4X,             // 4x Attack Speed toggle.
                FAST_ATTACK_2X,             // 2x Attack Speed toggle.
                INCREMENT_MOVEMENT_SPEED,   // 4X Base Movement Speed toggle.

            MENU_MISC,
                BUY_ANYTHING,           //
                CHARGE_PORTAL_EXPERIMENTAL,

            LAST
        }

        private static HashSet<HackMenuOptions> m_enabledFunctions = new HashSet<HackMenuOptions>();
        private static HackMenuOptions m_currentMenu = HackMenuOptions.UNKNOWN;
        private static HackMenuOptions m_selectedIndex = HackMenuOptions.UNKNOWN;


        /* Init */
        public void Start()
        {   
            if (!enabled)
                enabled = true;

            m_user = RoR2.MPEventSystemManager.primaryEventSystem.localUser;
        }


        #region Update
        public void Update()
        {
            HandleInput();
        }
        private void HandleInput()
        {
            if (Input.GetKeyUp(KeyCode.Z))
                m_isActive = !m_isActive;

            /* Down */
            if (Input.GetKeyUp(KeyCode.DownArrow))
                OnDown();

            /* Up */
            if (Input.GetKeyUp(KeyCode.UpArrow))
                OnUp();

            /* Confirm, Toggle */
            if (Input.GetKeyUp(KeyCode.RightArrow))
                OnForward();

            /* GO back */
            if (Input.GetKeyUp(KeyCode.LeftArrow))
                OnBack();


        }

        private void FixedUpdate()
        {
            m_purchaseInteractionList = InstanceTracker.GetInstancesList<PurchaseInteraction>();
            foreach (PurchaseInteraction p in m_purchaseInteractionList)
                if (!p.available)
                    m_purchaseInteractionList.Remove(p);

            m_portalInterraction = InstanceTracker.GetInstancesList<TeleporterInteraction>().First();

            if (m_enabledFunctions.Contains(HackMenuOptions.ENEMY_ESP) || m_enabledFunctions.Contains(HackMenuOptions.WEAKEN_ENEMIES)) UpdateEnemyList();
            if (m_enabledFunctions.Contains(HackMenuOptions.WEAKEN_ENEMIES)) WeakenEnemies();
            if (m_enabledFunctions.Contains(HackMenuOptions.BUY_ANYTHING)) UpdatePurchaseInteractions(false);
            if (m_enabledFunctions.Contains(HackMenuOptions.FULLY_HEAL)) HealPlayerPercentage(100);
            if (m_enabledFunctions.Contains(HackMenuOptions.GOD_MODE)) CheckGodMode();


            // if (m_enabledFunctions.Contains(HackMenuOptions.DISABLE_ALL)) m_enabledFunctions.Clear();
        }

        private void HealPlayerPercentage(int percent)
        {
            m_user.cachedBody.healthComponent.health = percent / 100 * m_user.cachedBody.healthComponent.fullHealth;
        }


        private static List<int> m_previousPurchasePrices = new List<int>();
        private void UpdatePurchaseInteractions(bool revertChanges)
        {
            /* Revert the changes */
            if(revertChanges)
            {
                if (m_previousPurchasePrices == null || m_previousPurchasePrices.Count <= 0)
                    return;

                /* Revert the prices */
                for (int i = 0; i < m_purchaseInteractionList.Count; ++i)
                {
                    PurchaseInteraction item = m_purchaseInteractionList.ElementAt(i);
                    item.cost = m_previousPurchasePrices.ElementAt(i);
                }

                m_previousPurchasePrices.Clear();
                return;
            }

            /* Update all of the prices to 0 */
            for (int i = 0; i < m_purchaseInteractionList.Count; ++i)
            {
                PurchaseInteraction item = m_purchaseInteractionList.ElementAt(i);
                m_previousPurchasePrices.Add(item.cost);
                item.cost = 0;
            }


        }
        public void UpdateEnemyList()
        {
            if (m_user == null || m_user.cachedBody == null)
                return;

            var characterBody = m_user.cachedBody;

            TeamIndex teamIndex = TeamIndex.Neutral;
            TeamComponent component = characterBody.GetComponent<TeamComponent>();

            if (!component)
                return;

            teamIndex = component.teamIndex;

            List<TeamComponent> enemyList = new List<TeamComponent>();
            for (TeamIndex teamIndex2 = TeamIndex.Neutral; teamIndex2 < TeamIndex.Count; teamIndex2 += 1)
            {
                if (teamIndex2 != teamIndex)
                {
                    enemyList.AddRange(TeamComponent.GetTeamMembers(teamIndex2));
                }
            }

            m_enemyEntityList.Clear();

            foreach (TeamComponent t in enemyList)
            {
                CharacterBody component3 = t.transform.GetComponent<CharacterBody>();

                if (component3 && component3 != characterBody)
                    m_enemyEntityList.Add(component3);
            }
        }


        #endregion

        #region Draw

        /* Draw Functions */
        private void DrawGUI()
        {
            /* Draw the buttons */
            if (m_currentMenu != HackMenuOptions.UNKNOWN) // Add
                CreateButtons(false);
            else
                CreateButtons(true);

        }

        private void CreateButtons(bool skipSubmenus)
        {
            HackMenuOptions i = m_currentMenu + 1;
            float offset = 0;

            while (i < HackMenuOptions.LAST)
            {
                StringBuilder name = new StringBuilder(i.ToString().ToLower());
                name[0] = (char)(name[0] - 32);

                string displayText = "<color=#" + GetMenuLabelColor(i) + ">" + name + "</color>";


                GUI.Label(new Rect(new Vector2(GUI_POSITION.x, GUI_POSITION.y + offset), new Vector2(GUI_POSITION.width, GUI_POSITION.height)), displayText);


                if (skipSubmenus)
                {
                    /* Skip over all of the items of this submenu */
                    if (isMenu(i))
                        i = GoForward(i);
                    else
                        i++;
                }
                else 
                {
                    i++;
                    if (isMenu(i))
                        break;
                }

                offset = offset + GUI_POSITION.height;
            }
        }

        private HackMenuOptions GoForward(HackMenuOptions i)
        {
            bool foundNext = false;

            while (!foundNext && i < HackMenuOptions.LAST)
            {
                i++;
                if (i.ToString().Substring(0, 4) == "MENU")
                    foundNext = true;
            }

            return i;
        }

        private void OnGUI()
        {
            /* Draw Main Hack */
            DrawHack();

            /* First, check if they are enabled! */
            if (m_enabledFunctions.Contains(HackMenuOptions.ENEMY_ESP)) DrawEnemyESP();
            if (m_enabledFunctions.Contains(HackMenuOptions.CHEST_ESP)) DrawChestESP();
            if (m_enabledFunctions.Contains(HackMenuOptions.PORTAL_ESP)) DrawPortalESP();
        }
      

        private void DrawHack()
        {
            if (!m_isActive)
                return;

            DrawGUI();

            //GUI_POSITION = GUILayout.Window(GUI_WINDOW_ID, GUI_POSITION, OnDrawGUI, GUI_HEADER);
        }
        private void DrawEnemyESP()
        {
            if (m_enemyEntityList == null || m_enemyEntityList.Count <= 0)
                return;

            foreach (CharacterBody enemy in m_enemyEntityList)
            {
                var transformedPosition = Camera.main.WorldToScreenPoint(enemy.transform.position);

                if (transformedPosition.z < 0.001f)
                    continue;

                GUI.Label(new Rect(transformedPosition.x, Screen.height - transformedPosition.y, 100, 100), enemy.GetDisplayName());
            }
        }
        private void DrawChestESP()
        {
            if (m_purchaseInteractionList == null || m_purchaseInteractionList.Count <= 0)
                return;

            foreach (PurchaseInteraction interaction in m_purchaseInteractionList)
            {
                var transformedPosition = Camera.main.WorldToScreenPoint(interaction.transform.position);
                if (transformedPosition.z < 0.001f)
                    continue;

                GUI.Label(new Rect(transformedPosition.x, Screen.height - transformedPosition.y, 100, 100), GetPurchaseInteractionStringData(interaction));

            }
        }
        private void DrawPortalESP()
        {
            if (m_portalInterraction == null)
                return;
           
            var transformedPosition = Camera.main.WorldToScreenPoint(m_portalInterraction.transform.position);

            if (transformedPosition.z > 0.001f)
                GUI.Label(new Rect(transformedPosition.x, Screen.height - transformedPosition.y, 100, 100), GetTeleporterStringData(m_portalInterraction));
           
        }

        #endregion

        // DO NOT REMOVE THE COMMENTS

        /*
        private void GetEveryObject()
        {
            LayerMask mask = LayerIndex.defaultLayer.mask | LayerIndex.world.mask | LayerIndex.pickups.mask;
            //array = Physics.OverlapSphere(Camera.main.transform.position, 9999, mask, QueryTriggerInteraction.Collide);

            // foreach (Collider item in array)
            

        }
        */
        /*
        private void GetLocalPlayerData()
        {

            user.cachedBody.healthComponent.godMode = true;
            user.cachedBody.baseMaxHealth += 1000;s
            var user.cachedBody.GetComponentsInChildren<PurchaseInteraction>();

           
            Debug.Log("INFO: Entered GetLocalPlayerData!");

            var master = (CharacterMaster)RoR2.GlobalEventManager.instance.GetType().GetProperty("master", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(RoR2.GlobalEventManager.instance);

            RoR2.CharacterBody characterBody = master.GetBodyObject().GetComponent<RoR2.CharacterBody>();

            if (!characterBody)
            {
                Debug.Log("ERROR: CharacterBody not found!");
                return;
            }

            if(!characterBody.isLocalPlayer)
            {
                Debug.Log("ERROR: characterBody.isLocalPlayer set to false!");
                return;
            }

            characterBody.master.GetType().GetMethod("ToggleGod", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(characterBody.master, null);
    
        NetworkUser flag = playerController.networkUserObject.GetComponent<NetworkUser>();
        var body = (CharacterBody)playerController.GetType().GetField("body", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(playerController);
            if (!body)
            {
                Debug.Log("ERROR: CharacterBody not found!");
                return;
            }

    body.master.GetType().GetMethod("ToggleGod", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(body.master, null);

    (CharacterBody) playerController.GetType().GetMethod("UpdateBodyGodMode", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(body.master)
             var masterHealthComponent = .GetComponent<RoR2.HealthComponent>();
    masterHealthComponent.godMode = true;
             ToggleGod


}
    */

        //


        #region Events
        private void OnForward()
        {
            switch (m_selectedIndex)
            {
                case HackMenuOptions.MENU_ESP:
                case HackMenuOptions.MENU_FAST:
                case HackMenuOptions.MENU_MISC:
                case HackMenuOptions.MENU_HEALTH:

                    LoadSubMenu(m_selectedIndex);
                    break;


                case HackMenuOptions.PORTAL_ESP:
                case HackMenuOptions.WEAKEN_ENEMIES:
                case HackMenuOptions.CHEST_ESP:
                case HackMenuOptions.ENEMY_ESP:
                case HackMenuOptions.FULLY_HEAL:
                // case HackMenuOptions.DISABLE_ALL:

                    ToggleEnabledFunction(m_selectedIndex);
                    break;

                case HackMenuOptions.FAST_ATTACK_4X: 
                case HackMenuOptions.FAST_ATTACK_2X:

                    ToggleAttackSpeed(m_selectedIndex);
                    break;

                case HackMenuOptions.INCREMENT_MOVEMENT_SPEED:

                    IncrementMovementSpeed();
                    break;


                case HackMenuOptions.BUY_ANYTHING: ToggleBuyAnything(); break;
                case HackMenuOptions.GOD_MODE: ToggleGodMode(); break;
                case HackMenuOptions.CHARGE_PORTAL_EXPERIMENTAL: FullyChargeTeleporter(); break;

                default: break;
            }
           
        }
        private void OnBack()
        {
            if (m_currentMenu == HackMenuOptions.UNKNOWN)
                return;

            m_currentMenu = GoBack(m_currentMenu);
            m_selectedIndex = GoBack(m_selectedIndex, true, false);
        }
        private void OnUp()
        {
            if (isMenu(m_selectedIndex) && m_currentMenu == HackMenuOptions.UNKNOWN)
            {
                HackMenuOptions newIndex = GoBack(m_selectedIndex, true);

                if (newIndex == HackMenuOptions.UNKNOWN)
                    newIndex = (m_selectedIndex - 1 <= HackMenuOptions.UNKNOWN ? m_selectedIndex : m_selectedIndex - 1);

                m_selectedIndex = newIndex;
            }
            else
                m_selectedIndex = (m_selectedIndex - 1 <= m_currentMenu ? m_selectedIndex : m_selectedIndex - 1);
        }
        private void OnDown()
        {
            if (isMenu(m_selectedIndex) && m_currentMenu == HackMenuOptions.UNKNOWN)
                m_selectedIndex = GoForward(m_selectedIndex);
            else if (m_currentMenu != HackMenuOptions.UNKNOWN)
                m_selectedIndex = (m_selectedIndex + 1 >= GoForward(m_currentMenu) ? m_selectedIndex : m_selectedIndex + 1);
            else
                m_selectedIndex = (m_selectedIndex + 1 >= HackMenuOptions.LAST ? m_selectedIndex : m_selectedIndex + 1);

        }

        #endregion

        /*
        public void OnDrawGUI(int id)
        {
            switch (id)
            {
                case GUI_WINDOW_ID: DrawGUI(); break;
                default: break;

            }
        }
        */

        #region Toggle Functions

        /* Buy Anything */
        private void ToggleBuyAnything()
        {
            /* UpdatePurchaseInteractions one last time if it got disabled */
            if (ToggleEnabledFunction(HackMenuOptions.BUY_ANYTHING) == false)
                UpdatePurchaseInteractions(true);
        }

        /* God Mode */
        private void ToggleGodMode()
        {
            CheckGodMode();
           

            bool godMode = ToggleEnabledFunction(HackMenuOptions.GOD_MODE);
            m_user.cachedBody.healthComponent.godMode = godMode;
        }

        private void CheckGodMode()
        {
            if (m_user == null || m_user.cachedBody == null || m_user.cachedBody.healthComponent == null)
            {
                m_enabledFunctions.Remove(HackMenuOptions.GOD_MODE);
                return;
            }
        }

        /* Attack Speed */
        private static float m_previousAttackSpeed = -1;
        private void ToggleAttackSpeed(HackMenuOptions i)
        {
            if (ToggleEnabledFunction(i))
            {
                m_previousAttackSpeed = m_user.cachedBody.baseAttackSpeed;

                if (i == HackMenuOptions.FAST_ATTACK_4X && !m_enabledFunctions.Contains(HackMenuOptions.FAST_ATTACK_2X))
                    m_user.cachedBody.baseAttackSpeed *= 4;
                else if (i == HackMenuOptions.FAST_ATTACK_2X && !m_enabledFunctions.Contains(HackMenuOptions.FAST_ATTACK_4X))
                    m_user.cachedBody.baseAttackSpeed *= 2;
                else
                    ToggleEnabledFunction(i);

                return;
            }

            m_user.cachedBody.baseAttackSpeed = m_previousAttackSpeed;
        }

        /* Movement Speed */
        private void IncrementMovementSpeed()
        {
            m_user.cachedBody.baseMoveSpeed += .5f;
        }

        /* Generic function */
        private bool ToggleEnabledFunction(HackMenuOptions option)
        {
            if (m_enabledFunctions.Contains(option))
            {
                m_enabledFunctions.Remove(option);
                return false;
            }
            else
            {
                m_enabledFunctions.Add(option);
                return true;
            }
        }

        #endregion

        #region Utility Functions
        private void WeakenEnemies()
        {
            if (m_enemyEntityList != null && m_enemyEntityList.Count > 0)
                foreach (CharacterBody enemy in m_enemyEntityList)
                    if (m_enabledFunctions.Contains(HackMenuOptions.WEAKEN_ENEMIES) && enemy.healthComponent.health > 1)
                        enemy.healthComponent.health = 1;
        }
        private void FullyChargeTeleporter()
        {
            RoR2.TeleporterInteraction.instance.remainingChargeTimer = 0.001f;
        }
        private void LoadSubMenu(HackMenuOptions menuIndex)
        {
            m_currentMenu = menuIndex;
        }
        private HackMenuOptions GoBack(HackMenuOptions oldMenu, bool findIndex = false, bool ignoreSame = true)
        {
            if (findIndex && !ignoreSame)
                oldMenu++;

            while (oldMenu != HackMenuOptions.UNKNOWN)
            {
                oldMenu--;
                if (isMenu(oldMenu) && findIndex)
                    return oldMenu;
            }

            return oldMenu;
        }

        #endregion

        #region Helper Functions

        private bool isMenu(HackMenuOptions i)
        {
            return i.ToString().Substring(0, 4) == "MENU";
        }
        private string GetMenuLabelColor(HackMenuOptions index)
        {
            if (m_enabledFunctions.Contains(index) && index != m_selectedIndex)
                return "00FF00";

            return (index == m_selectedIndex ? "FF0000" : "FFFFFF");
        }
        private string GetTeleporterStringData(TeleporterInteraction portalInteraction)
        {
            string text = "Teleporter";
            Color32 color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarItemDark);

            string returnString = "<color=#" + color + ">" + text + "</color>";

            if (m_user.cachedBody != null)
                returnString += "\n" + "[ " + Vector3.Distance(m_user.cachedBody.transform.position, portalInteraction.transform.position) + " ]";


            return returnString;
        }
        private string GetPurchaseInteractionStringData(PurchaseInteraction interaction)
        {            
            string text;
            string color;

            switch (interaction.costType)
            {
                case CostType.Money:
                    text = string.Format("${0}", interaction.cost);
                    color = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Money);
                    break;
                case CostType.PercentHealth:
                    text = string.Format("{0}% HP", interaction.cost);
                    color = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Teleporter);
                    break;
                case CostType.Lunar:
                    text = string.Format("{0} Lunar", interaction.cost);
                    color = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.LunarCoin);
                    break;
                case CostType.WhiteItem:
                    text = string.Format("{0} Items", interaction.cost);
                    color = ColorCatalog.GetColorHexString(PurchaseInteraction.CostTypeToColorIndex(interaction.costType));
                    break;
                case CostType.GreenItem:
                    text = string.Format("{0} Items", interaction.cost);
                    color = ColorCatalog.GetColorHexString(PurchaseInteraction.CostTypeToColorIndex(interaction.costType));
                    break;
                case CostType.RedItem:
                    text = string.Format("{0} Items", interaction.cost);
                    color = ColorCatalog.GetColorHexString(PurchaseInteraction.CostTypeToColorIndex(interaction.costType));
                    break;
                default:
                   text = string.Format("${0}", interaction.cost);
                   color = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Error);
                    break;

            }

            string returnString = "<color=#" + color + ">" + interaction.GetDisplayName() + "\n" + text + "</color>";

            if (m_user.cachedBody != null)
                returnString += "\n" + "[ " + Vector3.Distance(m_user.cachedBody.transform.position, interaction.transform.position) + " ]";


            return returnString;
        }  
       
     
    }

    #endregion
}
